import { Component, OnInit, signal, output } from '@angular/core';
import { PennantService } from '../pennant.service';
import { InsightsService } from '../insights.service';
import { LoggingService } from '../logging.service';

@Component({
  selector: 'sa-pennant-leaderboard',
  standalone: false,
  templateUrl: './leaderboard.component.html',
  styleUrl: './leaderboard.component.scss',
})
export class LeaderboardComponent implements OnInit {
  isLoading = signal(false);
  loadError = signal(false);
  leaderboard = signal<any[]>([]);
  private loadSeq = 0;

  years: number[] = [];
  divisions: string[] = [];
  pools: string[] = [];

  selectedYear: number | undefined = undefined;
  selectedDivision = '';
  selectedPool = '';
  minGames = 5;
  pageSize = 50;
  currentPage = 0;
  sortColumn = 'winRate';
  sortDirection: 'asc' | 'desc' = 'desc';
  playerSelected = output<string>();
  divisionPools: Record<string, string[]> = {};
  filteredPools: string[] = [];

  constructor(
    private pennant: PennantService,
    private insights: InsightsService,
    private logging: LoggingService
  ) {}

  ngOnInit(): void {
    this.pennant.getFilters().subscribe(filters => {
      this.years = filters.years;
      this.divisions = filters.divisions;
      this.pools = filters.pools;
      this.divisionPools = filters.divisionPools;
      this.filteredPools = filters.pools;
    });
    this.loadLeaderboard();
  }

  onDivisionChange(): void {
    this.selectedPool = '';
    this.filteredPools = this.selectedDivision
      ? (this.divisionPools[this.selectedDivision] ?? this.pools)
      : this.pools;
    this.loadLeaderboard();
  }

  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'desc' ? 'asc' : 'desc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'desc';
    }
    this.currentPage = 0;
  }

  ariaSort(column: string): string | null {
    return this.sortColumn === column ? (this.sortDirection === 'asc' ? 'ascending' : 'descending') : null;
  }

  get sortedLeaderboard(): any[] {
    return [...this.leaderboard()].sort((a, b) => {
      const aVal = a[this.sortColumn];
      const bVal = b[this.sortColumn];
      const dir = this.sortDirection === 'desc' ? -1 : 1;
      if (typeof aVal === 'string') return aVal.localeCompare(bVal) * dir;
      return (aVal - bVal) * dir;
    });
  }

  get pagedLeaderboard(): any[] {
    const start = this.currentPage * this.pageSize;
    return this.sortedLeaderboard.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.leaderboard().length / this.pageSize);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages - 1) this.currentPage++;
  }

  prevPage(): void {
    if (this.currentPage > 0) this.currentPage--;
  }

  getPosition(pageIndex: number): number {
    return this.currentPage * this.pageSize + pageIndex + 1;
  }

  selectPlayer(name: string): void {
    this.logging.usage('player', `${name} (via leaderboard)`);
    this.pennant.pendingSearch.set(name);
    this.playerSelected.emit(name);
  }

  loadLeaderboard(): void {
    this.insights.trackEvent('LeaderboardView', {
      year: this.selectedYear?.toString() ?? 'all',
      division: this.selectedDivision || 'all',
      pool: this.selectedPool || 'all'
    });
    const seq = ++this.loadSeq;
    this.isLoading.set(true);
    this.loadError.set(false);
    this.currentPage = 0;
    this.pennant.getLeaderboard({
      year: this.selectedYear,
      division: this.selectedDivision || undefined,
      pool: this.selectedPool || undefined,
      minGames: this.minGames,
    }).subscribe({
      next: (data) => {
        if (seq !== this.loadSeq) return;
        this.leaderboard.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        if (seq !== this.loadSeq) return;
        this.logging.error(`Leaderboard load failed: ${err?.message ?? err}`, 'LeaderboardComponent');
        this.loadError.set(true);
        this.isLoading.set(false);
      }
    });
  }
}