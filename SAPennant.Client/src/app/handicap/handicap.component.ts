import { Component, OnInit, signal } from '@angular/core';
import { PennantService } from '../pennant.service';
import { HandicapPlayer, HandicapDataPoint } from '../models/pennant.models';
import { getClubLogo } from '../data/club-logos';

@Component({
  selector: 'sa-pennant-handicap',
  standalone: false,
  templateUrl: './handicap.component.html',
  styleUrl: './handicap.component.scss',
})
export class HandicapComponent implements OnInit {
  players: HandicapPlayer[] = [];
  searchQuery = '';
  isLoading = signal(true);
  selectedPlayer: HandicapPlayer | null = null;
  history: HandicapDataPoint[] = [];
  historyLoading = signal(false);
  tooltip: { x: number; y: number; text: string } | null = null;

  sortCol: 'playerName' | 'lowestHandicap' | 'currentHandicap' | 'club' = 'lowestHandicap';
  sortDir: 'asc' | 'desc' = 'asc';

  constructor(private pennant: PennantService) {}

  ngOnInit(): void {
    this.pennant.getHandicapLeaderboard().subscribe({
      next: players => {
        this.players = players;
        this.isLoading.set(false);
      },
      error: err => {
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  setSort(col: typeof this.sortCol): void {
    if (this.sortCol === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortCol = col;
      this.sortDir = col === 'playerName' || col === 'club' ? 'asc' : 'asc';
    }
  }

  get sortedPlayers(): HandicapPlayer[] {
    return [...this.players].sort((a, b) => {
      let cmp = 0;
      if (this.sortCol === 'playerName') cmp = a.playerName.localeCompare(b.playerName);
      else if (this.sortCol === 'club') cmp = a.club.localeCompare(b.club);
      else cmp = a[this.sortCol] - b[this.sortCol];
      return this.sortDir === 'asc' ? cmp : -cmp;
    });
  }

  openModal(player: HandicapPlayer): void {
    this.selectedPlayer = player;
    this.historyLoading.set(true);
    this.pennant.getHandicapHistory(player.playerName).subscribe({
      next: history => {
        this.history = history;
        this.historyLoading.set(false);
      },
      error: err => {
        console.error(err);
        this.historyLoading.set(false);
      }
    });
  }

  closeModal(): void {
    this.selectedPlayer = null;
    this.history = [];
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-overlay')) {
      this.closeModal();
    }
  }

  get chartPoints(): string {
    if (!this.history.length) return '';
    const { padding, width, height, minH, maxH } = this.chartMeta;
    const range = maxH - minH || 1;

    return this.history.map((h, i) => {
      const x = padding + (i / (this.history.length - 1)) * (width - padding * 2);
      const y = padding + ((h.handicap - minH) / range) * (height - padding * 2);
      return `${x},${y}`;
    }).join(' ');
  }

  get scratchY(): number {
    const { minH, maxH, padding, height } = this.chartMeta;
    const range = maxH - minH || 1;
    return padding + ((0 - minH) / range) * (height - padding * 2);
  }

  get chartMeta() {
    if (!this.history.length) return { minH: 0, maxH: 0, width: 600, height: 220, padding: 40 };
    const handicaps = this.history
      .map(h => h.handicap)
      .filter(h => h >= -10 && h <= 54); // extra safety filter
    return {
      minH: Math.round((Math.min(...handicaps) - 1) * 10) / 10,
      maxH: Math.round((Math.max(...handicaps) + 1) * 10) / 10,
      width: 600,
      height: 220,
      padding: 40
    };
  }

  get chartFillPoints(): string {
    if (!this.history.length) return '';
    const { padding, width, height } = this.chartMeta;
    const points = this.chartPoints;
    const firstX = padding;
    const lastX = width - padding;
    return `${firstX},${height - padding} ${points} ${lastX},${height - padding}`;
  }

  displayHandicap(h: number): string {
    const rounded = Math.round(h * 10) / 10;
    if (rounded < 0) return `+${Math.abs(rounded)}`;
    if (rounded === 0) return 'Scratch';
    return rounded.toString();
  }

  showTooltip(event: MouseEvent, h: HandicapDataPoint, i: number): void {
    const svg = (event.target as SVGElement).closest('svg')!.getBoundingClientRect();
    const dot = (event.target as SVGElement).getBoundingClientRect();
    this.tooltip = {
      x: dot.left - svg.left + 8,
      y: dot.top - svg.top - 36,
      text: `${h.date} · ${this.displayHandicap(h.handicap)}`
    };
  }

  hideTooltip(): void {
    this.tooltip = null;
  }

  getClubLogo(clubName: string): string | null {
    return getClubLogo(clubName);
  }

  get filteredPlayers(): HandicapPlayer[] {
    if (!this.searchQuery.trim()) return this.sortedPlayers;
    const q = this.searchQuery.toLowerCase().trim();
    return this.sortedPlayers.filter(p =>
      p.playerName.toLowerCase().includes(q) ||
      p.club.toLowerCase().includes(q)
    );
  }
}