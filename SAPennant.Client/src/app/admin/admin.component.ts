import { Component, OnInit, signal } from '@angular/core';
import { PennantService } from '../pennant.service';
import { AuthService } from '../auth.service';

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
  seasons = signal<SyncStatus[]>([]);
  isSyncingAll = signal(false);
  newFinalsId: { [year: number]: number | null } = {};
  newSeniorRegularId: { [year: number]: number | null } = {};
  newSeniorFinalsId: { [year: number]: number | null } = {};
  usernameInput = '';
  passwordInput = '';
  authError = '';
  isSyncingUnsettled = signal(false);
  isSyncEnabled = signal(false);
  isMaintenanceMode = signal(false);

  constructor(public pennant: PennantService, public auth: AuthService) {}

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
  }

  login(): void {
    this.authError = '';
    this.auth.login(this.usernameInput, this.passwordInput).subscribe({
      next: () => {
        this.loadSeasons();
        this.pennant.refreshLastUpdated();
      },
      error: () => {
        this.authError = 'Invalid username or password';
      }
    });
  }

  logout(): void {
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
    season.isSyncing = true;
    season.message = '';
    season.messageType = '';
    this.seasons.set([...this.seasons()]);

    this.pennant.refreshYear(season.year).subscribe({
      next: (res: any) => {
        season.isSyncing = false;
        season.message = res.message ?? 'Sync complete';
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
        this.pennant.refreshLastUpdated();
      },
      error: () => {
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

    this.pennant.updateFinalsId(season.year, newId).subscribe({
      next: () => {
        season.finalsId = newId;
        season.message = `Finals ID updated to ${newId}`;
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
      },
      error: () => {
        season.message = 'Failed to update finals ID';
        season.messageType = 'error';
        this.seasons.set([...this.seasons()]);
      }
    });
  }

  updateSeniorRegularId(season: SyncStatus): void {
    const newId = this.newSeniorRegularId[season.year];
    if (!newId) return;
    this.pennant.updateSeniorRegularId(season.year, newId).subscribe({
      next: () => {
        season.seniorRegularId = newId;
        season.message = `Senior Regular ID updated to ${newId}`;
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
      },
      error: () => {
        season.message = 'Failed to update Senior Regular ID';
        season.messageType = 'error';
        this.seasons.set([...this.seasons()]);
      }
    });
  }

  updateSeniorFinalsId(season: SyncStatus): void {
    const newId = this.newSeniorFinalsId[season.year];
    if (!newId) return;
    this.pennant.updateSeniorFinalsId(season.year, newId).subscribe({
      next: () => {
        season.seniorFinalsId = newId;
        season.message = `Senior Finals ID updated to ${newId}`;
        season.messageType = 'success';
        this.seasons.set([...this.seasons()]);
      },
      error: () => {
        season.message = 'Failed to update Senior Finals ID';
        season.messageType = 'error';
        this.seasons.set([...this.seasons()]);
      }
    });
  }

  syncAll(): void {
    this.isSyncingAll.set(true);
    this.pennant.syncAll().subscribe({
      next: () => {
        this.isSyncingAll.set(false);
        this.pennant.refreshLastUpdated();
      },
      error: () => {
        this.isSyncingAll.set(false);
      }
    });
  }

  syncUnsettled(): void {
    this.isSyncingUnsettled.set(true);
    this.pennant.syncUnsettled().subscribe({
      next: () => {
        this.isSyncingUnsettled.set(false);
        this.pennant.refreshLastUpdated();
      },
      error: () => {
        this.isSyncingUnsettled.set(false);
      }
    });
  }

  toggleSync(): void {
    const newValue = !this.isSyncEnabled();
    this.pennant.toggleSync(newValue).subscribe(s => this.isSyncEnabled.set(s.enabled));
  }

  toggleMaintenance(): void {
    const newValue = !this.isMaintenanceMode();
    this.pennant.setMaintenance(newValue).subscribe({
      next: (data) => this.isMaintenanceMode.set(data.enabled),
      error: () => {}
    });
  }
}