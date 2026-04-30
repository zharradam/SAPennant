import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, signal, ViewChild } from '@angular/core';
import { PennantService } from '../pennant.service';
import { PlayerMatch } from '../models/pennant.models';
import { LoggingService } from '../logging.service';
import { getClubLogo } from '../data/club-logos';
import { PlayerStatsComponent } from '../player-stats/player-stats.component';

@Component({
  selector: 'sa-pennant-player-modal',
  standalone: false,
  templateUrl: './player-modal.component.html',
  styleUrl: './player-modal.component.scss',
})
export class PlayerModalComponent implements OnChanges {
  @Input() playerName: string | null = null;
  @Output() closed = new EventEmitter<void>();
  @ViewChild('statsRef') statsRef!: PlayerStatsComponent;

  isLoading = signal(false);
  matches = signal<PlayerMatch[]>([]);
  resolvedName = '';
  resolvedClub = '';

  get shareStateValue(): 'idle' | 'sharing' | 'copied' {
    return this.statsRef?.shareState() ?? 'idle';
  }

  constructor(private pennant: PennantService, private logging: LoggingService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['playerName'] && this.playerName) {
      this.load(this.playerName);
    }
  }

  load(name: string): void {
    this.isLoading.set(true);
    this.matches.set([]);
    this.logging.info(`Player modal opened: "${name}"`, 'PlayerModalComponent');
    this.pennant.search(name, 'team-results-modal').subscribe({
      next: (data) => {
        this.matches.set(data);
        this.resolvedName = this.resolveName(data, name);
        this.resolvedClub = data.length > 0
          ? ([...data].sort((a, b) => b.year - a.year)[0].playerClub ?? '')
          : '';
        this.isLoading.set(false);
      },
      error: (err) => {
        this.logging.error(`Player modal load failed for "${name}": ${err?.message ?? err}`, 'PlayerModalComponent');
        this.isLoading.set(false);
      }
    });
  }

  close(): void {
    this.closed.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-overlay')) {
      this.close();
    }
  }

  private resolveName(data: PlayerMatch[], fallback: string): string {
    if (!data.length) return fallback;
    const freq = new Map<string, number>();
    data.map(m => m.playerName).filter(Boolean)
      .forEach(n => freq.set(n, (freq.get(n) ?? 0) + 1));
    let max = 0, name = fallback;
    freq.forEach((count, n) => { if (count > max) { max = count; name = n; } });
    return name;
  }

  getClubLogoUrl(): string | null {
    return getClubLogo(this.resolvedClub);
  }

  sharePlayer(): void {
    this.statsRef?.sharePlayer();
  }
}