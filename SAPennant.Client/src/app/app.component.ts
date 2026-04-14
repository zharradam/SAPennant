import { Component, signal, OnInit } from '@angular/core';
import { PennantService } from './pennant.service';
import { InsightsService } from './insights.service';
import { retry, switchMap } from 'rxjs/operators';
import { interval } from 'rxjs';
import { buildInfo } from '../environments/build-info';
import { Router } from '@angular/router';

@Component({
  selector: 'sa-pennant-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.scss'
})
export class App implements OnInit {
  activeTab = signal<'team' | 'search' | 'club' | 'leaderboard' | 'handicap' | 'honour-roll' | 'admin'>('team');
  selectedPlayer = signal('');
  isLoadingApi = signal(true);
  menuOpen = signal(false);
  aboutOpen = signal(false);
  buildInfo = buildInfo;
  maintenanceMode = signal(false);
  showIosBanner = signal(false);

  constructor(private pennant: PennantService, private router: Router, private insights: InsightsService) {}

  ngOnInit(): void {
    this.checkMaintenance();
    setInterval(() => this.checkMaintenance(), 60000);
      
    this.checkIosBanner();

    this.pennant.refreshLastUpdated().pipe(
      retry({ count: 10, delay: 3000 })
    ).subscribe({
      next: () => this.isLoadingApi.set(false),
      error: () => this.isLoadingApi.set(false)
    });

    // keepalive every 15 minutes
    interval(15 * 60 * 1000).pipe(
      switchMap(() => this.pennant.getLastUpdated())
    ).subscribe();
  }

  navigateToPlayer(name: string): void {
    this.pennant.pendingSearch.set(name);
    this.activeTab.set('search');
  }

  get lastUpdated() {
     return this.pennant.lastUpdated; 
  }

  toggleMenu(): void {
    this.menuOpen.set(!this.menuOpen());
  }

  selectTab(tab: 'team' | 'club' | 'leaderboard' | 'handicap' | 'search' | 'honour-roll' | 'admin'): void {
    this.activeTab.set(tab);
    this.menuOpen.set(false);
    this.insights.trackTabView(tab);
    this.checkMaintenance();
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
      error: () => {} // silently fail
    });
  }

  isAdminRoute(): boolean {
    return this.router.url.includes('/admin');
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
    localStorage.setItem('sapennant_ios_banner_dismissed', 'true')
    this.showIosBanner.set(false);
  }
}