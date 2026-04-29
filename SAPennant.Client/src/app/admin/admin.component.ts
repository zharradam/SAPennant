import { Component, OnInit, signal } from '@angular/core';
import { PennantService } from '../pennant.service';
import { AuthService } from '../auth.service';
import { LoggingService } from '../logging.service';

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
export class AdminComponent implements OnInit {
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
  isMaintenanceMode = signal(false);
  intervalError = signal<string | null>(null);

  constructor(
    public pennant: PennantService,
    public auth: AuthService,
    private logging: LoggingService
  ) {}

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.loadSeasons();
      this.pennant.refreshLastUpdated();
      this.pennant.getSyncStatus().subscribe(s => this.isSyncEnabled.set(s.enabled));
    }

    this.pennant.getMaintenance().subscribe({
      next: (data) => this.isMaintenanceMode.set(data.enabled),
      error: () => {}
    });

    this.pennant.getPollingInterval().subscribe(r => this.pollingInterval.set(r.minutes));
  }

  login(): void {
    this.authError.set(null);
    this.auth.login(this.usernameInput, this.passwordInput).subscribe({
      next: () => {
        this.logging.info(`Admin login successful: "${this.usernameInput}"`, 'AdminComponent');
        this.loadSeasons();
        this.pennant.refreshLastUpdated();
      },
      error: (err) => {
        this.logging.warn(`Admin login failed for "${this.usernameInput}": status ${err.status}`, 'AdminComponent');
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