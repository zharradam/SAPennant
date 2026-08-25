import { Component, signal, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { PennantService } from './pennant.service';
import { InsightsService } from './insights.service';
import { retry, switchMap } from 'rxjs/operators';
import { interval } from 'rxjs';
import { buildInfo } from '../environments/build-info';
import { LoggingService } from './logging.service';

type TabName = 'team' | 'search' | 'club' | 'leaderboard' | 'handicap' | 'honour-roll' | 'admin';

const TAB_NAMES: readonly TabName[] = ['team', 'search', 'club', 'leaderboard', 'handicap', 'honour-roll', 'admin'];

@Component({
  selector: 'sa-pennant-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.scss'
})
export class App implements OnInit, AfterViewInit {
  activeTab = signal<TabName>('team');
  selectedPlayer = signal('');
  isLoadingApi = signal(true);
  menuOpen = signal(false);
  aboutOpen = signal(false);
  buildInfo = buildInfo;
  maintenanceMode = signal(false);
  showIosBanner = signal(false);
  showOverlay = signal(true);
  overlayMessage = signal('Waking up the server…');
  apiFailed = signal(false);

  private overlayMessages = [
    'Waking up the server…',
    'Loading season data…',
    'Almost there…',
    'Just a moment…'
  ];
  private msgIndex = 0;
  private msgInterval: any;
  private particleInterval: any;
  private barInterval: any;
  private barWidth = 0;
  private particles: { x: number; y: number; vx: number; vy: number; r: number; o: number }[] = [];

  @ViewChild('overlayCanvas') overlayCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('overlayBar') overlayBar!: ElementRef<HTMLDivElement>;
  @ViewChild('overlayBall') overlayBall!: ElementRef<HTMLImageElement>;
  @ViewChild('overlaySpinner') overlaySpinner!: ElementRef<HTMLDivElement>;

  constructor(
    private pennant: PennantService,
    private insights: InsightsService,
    private logging: LoggingService
  ) {}

  ngOnInit(): void {
    const originalError = console.error.bind(console);
    const originalWarn = console.warn.bind(console);

    console.error = (...args: any[]) => {
      originalError(...args);
      // Error objects reach GlobalErrorHandler, which ships them with full
      // context — forwarding them here too would double-log every error.
      if (args[0] instanceof Error) return;
      this.logging.error(args.map(a => String(a)).join(' '), 'console');
    };

    console.warn = (...args: any[]) => {
      originalWarn(...args);
      this.logging.warn(args.map(a => String(a)).join(' '), 'console');
    };

    this.logging.usage('visit',
      `${window.location.pathname}${window.location.search} (${this.logging.visitType})`);

    // Restore the tab from the URL path (shareable links), then let a
    // ?player= param force the search tab as before.
    const initialTab = this.tabFromPath(window.location.pathname);
    if (initialTab) this.activeTab.set(initialTab);

    const params = new URLSearchParams(window.location.search);
    if (params.get('player')) {
      this.activeTab.set('search');
    }

    window.addEventListener('popstate', () => {
      this.activeTab.set(this.tabFromPath(window.location.pathname) ?? 'team');
      this.menuOpen.set(false);
    });

    this.checkMaintenance();
    setInterval(() => this.checkMaintenance(), 60000);
    this.checkIosBanner();
    this.startOverlayAnimations();

    this.connectToApi();

    interval(15 * 60 * 1000).pipe(
      switchMap(() => this.pennant.getLastUpdated())
    ).subscribe(() => this.checkForNewVersion());

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') this.checkForNewVersion();
    });
  }

  // Replaces the removed SwUpdate flow: compare the deployed bundle hash
  // against the one this page loaded with, and reload once when they differ.
  private currentBundleHash(): string | null {
    const src = document.querySelector('script[src*="main-"]')?.getAttribute('src') ?? '';
    return /main-([A-Z0-9]+)\.js/.exec(src)?.[1] ?? null;
  }

  private checkForNewVersion(): void {
    const current = this.currentBundleHash();
    if (!current) return; // dev server — bundles aren't hashed

    fetch(new URL('index.html', document.baseURI).toString(), { cache: 'no-store' })
      .then(r => (r.ok ? r.text() : null))
      .then(html => {
        const latest = html ? /main-([A-Z0-9]+)\.js/.exec(html)?.[1] : null;
        if (!latest || latest === current) return;

        // Only attempt one reload per new version, so a briefly stale HTTP
        // cache can't cause a reload loop.
        const guardKey = 'sapennant_reload_for';
        if (sessionStorage.getItem(guardKey) === latest) return;
        sessionStorage.setItem(guardKey, latest);

        this.logging.info(`New version deployed (${latest}) — reloading`, 'VersionCheck');
        setTimeout(() => window.location.reload(), 1000);
      })
      .catch(() => {});
  }

  ngAfterViewInit(): void {
    this.startParticles();
  }

  connectToApi(): void {
    this.pennant.refreshLastUpdated().pipe(
      retry({ count: 10, delay: 3000 })
    ).subscribe({
      next: () => {
        this.completeOverlay();
        this.isLoadingApi.set(false);
      },
      error: () => this.failOverlay()
    });
  }

  failOverlay(): void {
    clearInterval(this.msgInterval);
    clearInterval(this.barInterval);
    this.apiFailed.set(true);
    this.overlayMessage.set("Couldn't reach the server — it may be down. Please try again shortly.");
  }

  retryConnection(): void {
    this.apiFailed.set(false);
    this.overlayMessage.set('Waking up the server…');
    this.startOverlayAnimations();
    this.connectToApi();
  }

  navigateToPlayer(name: string): void {
    this.pennant.pendingSearch.set(name);
    this.selectTab('search');
  }

  private tabFromPath(pathname: string): TabName | null {
    const segment = pathname.replace(/\/+$/, '').split('/').pop() ?? '';
    return (TAB_NAMES as readonly string[]).includes(segment) ? (segment as TabName) : null;
  }

  get lastUpdated() {
    return this.pennant.lastUpdated;
  }

  toggleMenu(): void {
    this.menuOpen.set(!this.menuOpen());
  }

  selectTab(tab: TabName): void {
    this.activeTab.set(tab);
    this.menuOpen.set(false);
    this.logging.usage('tab', tab);
    this.insights.trackTabView(tab);
    this.checkMaintenance();

    const path = tab === 'team' ? '/' : `/${tab}`;
    if (window.location.pathname !== path) {
      history.pushState(null, '', path);
    }
  }

  openAbout(e: Event): void {
    e.preventDefault();
    this.aboutOpen.set(true);
  }

  closeAbout(): void {
    this.aboutOpen.set(false);
  }

  checkMaintenance(): void {
    this.pennant.getMaintenance().subscribe({
      next: (data) => this.maintenanceMode.set(data.enabled),
      error: () => {}
    });
  }

  checkIosBanner(): void {
    const isIos = /iPhone|iPad|iPod/.test(navigator.userAgent);
    const isInStandaloneMode = (window.navigator as any).standalone === true;
    const isDismissed = localStorage.getItem('sapennant_ios_banner_dismissed') === 'true';

    if (isIos && !isInStandaloneMode && !isDismissed) {
      this.showIosBanner.set(true);
    }
  }

  dismissIosBanner(): void {
    localStorage.setItem('sapennant_ios_banner_dismissed', 'true');
    this.showIosBanner.set(false);
  }

  startOverlayAnimations(): void {
    this.msgInterval = setInterval(() => {
      this.msgIndex = (this.msgIndex + 1) % this.overlayMessages.length;
      this.overlayMessage.set(this.overlayMessages[this.msgIndex]);
    }, 2000);

    this.barWidth = 0;
    this.barInterval = setInterval(() => {
      if (this.barWidth < 85) {
        this.barWidth += 0.5;
        if (this.overlayBar?.nativeElement) {
          this.overlayBar.nativeElement.style.width = `${this.barWidth}%`;
        }
      }
    }, 50);
  }

  startParticles(): void {
    const canvas = this.overlayCanvas?.nativeElement;
    if (!canvas) return;

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    for (let i = 0; i < 30; i++) {
      this.particles.push({
        x: Math.random() * window.innerWidth,
        y: Math.random() * window.innerHeight,
        vx: (Math.random() - 0.5) * 0.4,
        vy: (Math.random() - 0.5) * 0.4,
        r: Math.random() * 2 + 1,
        o: Math.random() * 0.4 + 0.1
      });
    }

    const ctx = canvas.getContext('2d')!;
    const draw = () => {
      if (!this.showOverlay()) return;
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      for (const p of this.particles) {
        p.x += p.vx;
        p.y += p.vy;
        if (p.x < 0) p.x = canvas.width;
        if (p.x > canvas.width) p.x = 0;
        if (p.y < 0) p.y = canvas.height;
        if (p.y > canvas.height) p.y = 0;
        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(96, 165, 250, ${p.o})`;
        ctx.fill();
      }
      requestAnimationFrame(draw);
    };
    draw();
  }

  completeOverlay(): void {
    clearInterval(this.msgInterval);
    clearInterval(this.barInterval);

    if (this.overlayBar?.nativeElement) {
      this.overlayBar.nativeElement.style.width = '100%';
      this.overlayBar.nativeElement.style.transition = 'width 0.3s ease';
      this.overlayBar.nativeElement.classList.add('ready');
    }

    if (this.overlayBall?.nativeElement) {
      this.overlayBall.nativeElement.classList.add('ready');
    }

    if (this.overlaySpinner?.nativeElement) {
      this.overlaySpinner.nativeElement.classList.add('ready');
    }

    this.overlayMessage.set('Ready!');

    setTimeout(() => {
      this.showOverlay.set(false);
    }, 800);
  }
}