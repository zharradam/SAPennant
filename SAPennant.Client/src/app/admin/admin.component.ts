import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { UsageSummary } from '../models/pennant.models';
import { PennantService } from '../pennant.service';
import { AuthService } from '../auth.service';
import { LoggingService } from '../logging.service';

// Bars for a plain SVG chart; `tick` marks the bars that get an axis label.
// Text in an SVG scales with the viewBox, so charts drawn at half the card's
// width get a half-width viewBox to keep their labels legible.
function barChart(points: { label: string; value: number; title: string; tick: boolean }[], height = 170, width = 600) {
  const left = 30, top = 20, base = height - 20;
  const max = Math.max(0, ...points.map(p => p.value));
  const scale = Math.max(1, max); // avoid dividing by zero on an empty window
  const w = (width - left) / Math.max(1, points.length);
  const bars = points.map((p, i) => {
    const h = (p.value / scale) * (base - top);
    return { ...p, x: left + i * w, y: base - h, w: Math.max(1, w - 1), h };
  });
  return { width, height, left, base, max, bars };
}

function isoDay(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

type AdminTab = 'sync' | 'usage';
const TAB_KEY = 'sapennant_admin_tab';

function storedTab(): AdminTab {
  try { return sessionStorage.getItem(TAB_KEY) === 'usage' ? 'usage' : 'sync'; } catch { return 'sync'; }
}

interface SyncStatus {
  year: number;
  regularId: number;
  finalsId: number | null;
  seniorRegularId: number | null;
  seniorFinalsId: number | null;
  isSyncing: boolean;
  message: string;
  messageType: 'success' | 'error' | 'info' | '';
}

@Component({
  selector: 'sa-pennant-admin',
  standalone: false,
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit, OnDestroy {
  ngOnDestroy(): void {
    this.mq.removeEventListener('change', this.onMq);
  }

  newFinalsId: { [year: number]: number | null } = {};
  newSeniorRegularId: { [year: number]: number | null } = {};
  newSeniorFinalsId: { [year: number]: number | null } = {};
  usernameInput = '';
  passwordInput = '';
  pollingInterval = signal(60);
  isSavingInterval = signal(false);
  intervalSaved = signal(false);

  seasons = signal<SyncStatus[]>([]);
  isSyncingAll = signal(false);
  authError = signal<string | null>(null);
  isSyncingUnsettled = signal(false);
  isSyncEnabled = signal(false);
  // Which admin tab is open; remembered for the session so a reload after a
  // sync lands back on the same view.
  adminTab = signal<AdminTab>(storedTab());
  usage = signal<UsageSummary | null>(null);
  usageDays = signal(30);
  usageError = signal(false);
  usageLoading = signal(false);
  readonly windowOptions = [
    { days: 7, label: '7d', name: 'Last 7 days' },
    { days: 30, label: '30d', name: 'Last 30 days' },
    { days: 90, label: '90d', name: 'Last 90 days' },
    { days: 365, label: '1y', name: 'Last year' },
    { days: 0, label: 'All', name: 'All time' },
  ];

  // "Last 30 days · 15 Sep – 14 Oct"
  rangeLabel = computed(() => {
    const name = this.windowOptions.find(w => w.days === this.usageDays())?.name ?? '';
    const c = this.usageChart();
    if (!c || c.bars.length < 2) return name;
    const fmt = (iso: string) => new Date(iso + 'T00:00').toLocaleDateString('en-AU', { day: 'numeric', month: 'short' });
    return `${name} · ${fmt(c.bars[0].label)} – ${fmt(c.bars[c.bars.length - 1].label)}`;
  });

  // Phones draw every chart at full card width, which is about half a
  // desktop card, so they get the narrow viewBox too.
  private narrow = signal(window.matchMedia('(max-width: 768px)').matches);
  private readonly mq = window.matchMedia('(max-width: 768px)');
  private readonly onMq = (e: MediaQueryListEvent) => this.narrow.set(e.matches);

  // Visits-per-day bars with the empty days filled in, sized for the SVG.
  usageChart = computed(() => {
    const u = this.usage();
    if (!u) return null;
    const byDay = new Map(u.days.map(d => [d.day, d]));
    const today = new Date();
    const days = this.usageDays();
    const first = days > 0 ? new Date(today.getFullYear(), today.getMonth(), today.getDate() - (days - 1))
      : u.days.length ? new Date(u.days[0].day) : today;
    const labels: string[] = [];
    for (const d = new Date(first); d <= today; d.setDate(d.getDate() + 1)) labels.push(isoDay(d));
    return barChart(labels.map((day, i) => {
      const row = byDay.get(day);
      return { label: day, value: row?.visits ?? 0, title: `${day}: ${row?.visits ?? 0} visits, ${row?.visitors ?? 0} visitors`,
               tick: i === 0 || i === labels.length - 1 };
    }), this.narrow() ? 140 : 170, this.narrow() ? 300 : 600);
  });

  hourChart = computed(() => {
    const u = this.usage();
    return u && barChart(u.byHour.map((v, h) => ({ label: `${h}:00`, value: v, title: `${h}:00–${h}:59: ${v} visits`, tick: h % 6 === 0 })), 110, 300);
  });

  weekdayChart = computed(() => {
    const u = this.usage();
    const names = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    return u && barChart(u.byWeekday.map((v, i) => ({ label: names[i], value: v, title: `${names[i]}: ${v} visits`, tick: true })), 110, 300);
  });

  // "+12%" against the previous window, or null when there is nothing to compare to.
  delta(now: number, prev: number | undefined): { text: string; dir: 'up' | 'down' | 'flat' } | null {
    if (prev === undefined || prev === 0 || this.usageDays() === 0) return null;
    const pct = Math.round(((now - prev) / prev) * 100);
    return { text: `${pct > 0 ? '+' : pct < 0 ? '−' : ''}${Math.abs(pct)}%`, dir: pct > 0 ? 'up' : pct < 0 ? 'down' : 'flat' };
  }

  mobileShare(): string | null {
    const u = this.usage();
    return u && u.knownDeviceVisits > 0 ? `${Math.round((u.mobileVisits / u.knownDeviceVisits) * 100)}%` : null;
  }

  usageLists = computed(() => {
    const u = this.usage();
    if (!u) return [];
    const list = (title: string, rows: { name: string; count: number }[], empty: string) =>
      ({ title, rows, empty, max: Math.max(1, ...rows.map(r => r.count)) });
    return [
      list('What people do', u.byEvent, 'No activity yet'),
      list('Top searches', u.topSearches, 'No searches yet'),
      list('Searches with no results', u.topMisses, 'Every search found something'),
      list('Top players', u.topPlayers, 'No player lookups yet'),
      list('Top clubs', u.topClubs, 'No club lookups yet'),
      list('Came from', u.topReferrers, 'Direct visits only'),
    ];
  });
  isMaintenanceMode = signal(false);
  intervalError = signal<string | null>(null);
  isBackingUp = signal(false);
  backupError = signal<string | null>(null);

  constructor(
    public pennant: PennantService,
    public auth: AuthService,
    private logging: LoggingService
  ) {}

  ngOnInit(): void {
    this.mq.addEventListener('change', this.onMq);
    if (this.auth.isAuthenticated()) {
      this.loadAdminData();
    }

    this.pennant.getMaintenance().subscribe({
      next: (data) => this.isMaintenanceMode.set(data.enabled),
      error: () => {}
    });
  }

  async downloadBackup(): Promise<void> {
    const filename = `sapennant-backup-${new Date().toISOString().slice(0, 10)}.zip`;

    // Where supported (Chrome/Edge), open a Save As dialog immediately —
    // it must happen on the click gesture, before the slow download. Pick
    // the SAPennant\Backups folder once and the browser remembers it for
    // next time. Elsewhere (Firefox/Safari) we fall back to a normal
    // download into the browser's Downloads folder.
    let fileHandle: any = null;
    const picker = (window as any).showSaveFilePicker;
    if (picker) {
      try {
        fileHandle = await picker.call(window, {
          suggestedName: filename,
          types: [{ description: 'Zip archive', accept: { 'application/zip': ['.zip'] } }]
        });
      } catch (e: any) {
        if (e?.name === 'AbortError') return; // user cancelled the dialog
        fileHandle = null; // any other picker failure → plain download
      }
    }

    this.isBackingUp.set(true);
    this.backupError.set(null);
    this.pennant.downloadBackup().subscribe({
      next: async (blob) => {
        try {
          if (fileHandle) {
            const writable = await fileHandle.createWritable();
            await writable.write(blob);
            await writable.close();
          } else {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            a.click();
            URL.revokeObjectURL(url);
          }
          this.logging.info('Database backup downloaded', 'AdminComponent');
        } catch {
          this.backupError.set('Could not save the backup file.');
        }
        this.isBackingUp.set(false);
      },
      error: (err) => {
        this.isBackingUp.set(false);
        this.backupError.set('Backup failed. Please try again.');
        this.logging.error(`Backup download failed: status ${err.status}`, 'AdminComponent');
      }
    });
  }

  login(): void {
    this.authError.set(null);
    this.auth.login(this.usernameInput, this.passwordInput, this.logging.visitorId).subscribe({
      next: () => {
        this.loadAdminData();
      },
      error: (err) => {
        if (err.status === 429) {
          this.authError.set('Too many login attempts. Please wait before trying again.');
        } else if (err.status === 401) {
          this.authError.set('Invalid username or password.');
        } else {
          this.authError.set('An error occurred. Please try again.');
        }
      }
    });
  }

  // Everything the admin screen needs once authenticated — called on init
  // with a stored token and again after a fresh login, so the toggles show
  // the server's saved state rather than their defaults.
  private loadAdminData(): void {
    this.loadUsage();
    this.loadSeasons();
    this.pennant.refreshLastUpdated();
    this.pennant.getSyncStatus().subscribe(s => this.isSyncEnabled.set(s.enabled));
    this.pennant.getPollingInterval().subscribe(r => this.pollingInterval.set(r.minutes));
  }

  selectTab(tab: AdminTab): void {
    this.adminTab.set(tab);
    try { sessionStorage.setItem(TAB_KEY, tab); } catch { /* storage unavailable */ }
    // Arrow keys move the selection; keep keyboard focus on the selected tab.
    queueMicrotask(() => document.getElementById('admin-tab-' + tab)?.focus());
  }

  setUsageDays(days: number): void {
    if (days === this.usageDays()) return;
    this.usageDays.set(days);
    this.loadUsage();
  }

  loadUsage(): void {
    const days = this.usageDays();
    this.usageError.set(false);
    this.usageLoading.set(true);
    this.pennant.getUsage(days).subscribe({
      next: u => {
        if (days !== this.usageDays()) return; // a newer window was picked meanwhile
        this.usage.set(u);
        this.usageLoading.set(false);
      },
      error: () => {
        if (days !== this.usageDays()) return;
        this.usageError.set(true);
        this.usageLoading.set(false);
      },
    });
  }

  logout(): void {
    this.logging.info('Admin logged out', 'AdminComponent');
    this.auth.logout();
  }

  loadSeasons(): void {
    this.pennant.getAdminSeasons().subscribe(data => {
      this.seasons.set(data.map((s: any) => ({
        year: s.year,
        regularId: s.regularId,
        finalsId: s.finalsId,
        seniorRegularId: s.seniorRegularId ?? null,
        seniorFinalsId: s.seniorFinalsId ?? null,
        isSyncing: false,
        message: '',
        messageType: ''
      })));
      data.forEach((s: any) => {
        this.newFinalsId[s.year] = s.finalsId;
        this.newSeniorRegularId[s.year] = s.seniorRegularId;
        this.newSeniorFinalsId[s.year] = s.seniorFinalsId;
      });
    });
  }

  refreshYear(season: SyncStatus): void {
    this.logging.info(`Season refresh started: ${season.year}`, 'AdminComponent');
    season.isSyncing = true;
    season.message = '';
    season.messageType = '';
    this.seasons.set([...this.seasons()]);

    this.pennant.refreshYear(season.year).subscribe({
      next: (res: any) => {
        this.logging.info(`Season refresh complete: ${season.year} — ${res.message ?? 'Sync complete'}`, 'AdminComponent');
        season.isSyncing = false;
        season.message = res.message ?? 'Sync complete';
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
        this.pennant.refreshLastUpdated();
      },
      error: (err) => {
        this.logging.error(`Season refresh failed: ${season.year} — ${err?.message ?? err}`, 'AdminComponent');
        season.isSyncing = false;
        season.message = 'Sync failed';
        season.messageType = 'error';
        this.seasons.set([...this.seasons()]);
      }
    });
  }

  updateFinalsId(season: SyncStatus): void {
    const newId = this.newFinalsId[season.year];
    if (!newId) return;
    this.logging.info(`Finals ID updated: ${season.year} → ${newId}`, 'AdminComponent');
    this.pennant.updateFinalsId(season.year, newId).subscribe({
      next: () => {
        season.finalsId = newId;
        season.message = `Finals ID updated to ${newId}`;
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
      },
      error: (err) => {
        this.logging.error(`Finals ID update failed: ${season.year} — ${err?.message ?? err}`, 'AdminComponent');
        season.message = 'Failed to update finals ID';
        season.messageType = 'error';
        this.seasons.set([...this.seasons()]);
      }
    });
  }

  updateSeniorRegularId(season: SyncStatus): void {
    const newId = this.newSeniorRegularId[season.year];
    if (!newId) return;
    this.logging.info(`Senior Regular ID updated: ${season.year} → ${newId}`, 'AdminComponent');
    this.pennant.updateSeniorRegularId(season.year, newId).subscribe({
      next: () => {
        season.seniorRegularId = newId;
        season.message = `Senior Regular ID updated to ${newId}`;
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
      },
      error: (err) => {
        this.logging.error(`Senior Regular ID update failed: ${season.year} — ${err?.message ?? err}`, 'AdminComponent');
        season.message = 'Failed to update Senior Regular ID';
        season.messageType = 'error';
        this.seasons.set([...this.seasons()]);
      }
    });
  }

  updateSeniorFinalsId(season: SyncStatus): void {
    const newId = this.newSeniorFinalsId[season.year];
    if (!newId) return;
    this.logging.info(`Senior Finals ID updated: ${season.year} → ${newId}`, 'AdminComponent');
    this.pennant.updateSeniorFinalsId(season.year, newId).subscribe({
      next: () => {
        season.seniorFinalsId = newId;
        season.message = `Senior Finals ID updated to ${newId}`;
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
      },
      error: (err) => {
        this.logging.error(`Senior Finals ID update failed: ${season.year} — ${err?.message ?? err}`, 'AdminComponent');
        season.message = 'Failed to update Senior Finals ID';
        season.messageType = 'error';
        this.seasons.set([...this.seasons()]);
      }
    });
  }

  syncAll(): void {
    this.logging.info('Sync all seasons triggered', 'AdminComponent');
    this.isSyncingAll.set(true);
    this.pennant.syncAll().subscribe({
      next: () => {
        this.logging.info('Sync all complete', 'AdminComponent');
        this.isSyncingAll.set(false);
        this.pennant.refreshLastUpdated();
      },
      error: (err) => {
        this.logging.error(`Sync all failed: ${err?.message ?? err}`, 'AdminComponent');
        this.isSyncingAll.set(false);
      }
    });
  }

  syncUnsettled(): void {
    this.logging.info('Sync unsettled triggered', 'AdminComponent');
    this.isSyncingUnsettled.set(true);
    this.pennant.syncUnsettled().subscribe({
      next: () => {
        this.logging.info('Sync unsettled complete', 'AdminComponent');
        this.isSyncingUnsettled.set(false);
        this.pennant.refreshLastUpdated();
      },
      error: (err) => {
        this.logging.error(`Sync unsettled failed: ${err?.message ?? err}`, 'AdminComponent');
        this.isSyncingUnsettled.set(false);
      }
    });
  }

  toggleSync(): void {
    const newValue = !this.isSyncEnabled();
    this.logging.info(`Auto sync toggled: ${newValue ? 'enabled' : 'disabled'}`, 'AdminComponent');
    this.pennant.toggleSync(newValue).subscribe(s => this.isSyncEnabled.set(s.enabled));
  }

  toggleMaintenance(): void {
    const newValue = !this.isMaintenanceMode();
    this.logging.info(`Maintenance mode toggled: ${newValue ? 'enabled' : 'disabled'}`, 'AdminComponent');
    this.pennant.setMaintenance(newValue).subscribe({
      next: (data) => this.isMaintenanceMode.set(data.enabled),
      error: () => {}
    });
  }

  savePollingInterval(): void {
    this.logging.info(`Polling interval saved: ${this.pollingInterval()} minutes`, 'AdminComponent');
    this.intervalError.set(null);
    this.isSavingInterval.set(true);
    this.pennant.setPollingInterval(this.pollingInterval()).subscribe({
      next: () => {
        this.isSavingInterval.set(false);
        this.intervalSaved.set(true);
        setTimeout(() => this.intervalSaved.set(false), 3000);
      },
      error: (err) => {
        this.logging.error(`Polling interval save failed: ${err?.error ?? err?.message ?? err}`, 'AdminComponent');
        this.isSavingInterval.set(false);
        this.intervalError.set(err?.error ?? 'Failed to save interval');
        setTimeout(() => this.intervalError.set(null), 5000);
      }
    });
  }
}