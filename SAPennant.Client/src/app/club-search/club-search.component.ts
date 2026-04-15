import { Component, signal, Output, EventEmitter } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { PennantService } from '../pennant.service';
import { ClubPlayer } from '../models/pennant.models';
import { CLUB_LOGOS } from '../data/club-logos';
import { InsightsService } from '../insights.service';

@Component({
  selector: 'sa-pennant-club',
  standalone: false,
  templateUrl: './club-search.component.html',
  styleUrl: './club-search.component.scss',
})
export class ClubSearchComponent {
  @Output() playerSelected = new EventEmitter<string>();

  query = '';
  allPlayers: ClubPlayer[] = [];
  isLoading = signal(false);
  hasSearched = false;
  selectedClub: string | null = null;
  suggestions = signal<string[]>([]);
  showSuggestions = signal(false);
  isSlowResponse = signal(false);
  slowTimeout: any = null;

  availableYears: number[] = [];
  selectedYears = new Set<number>();
  availablePools: string[] = [];
  selectedPools = new Set<string>();

  sortCol: 'playerName' | 'played' | 'wins' | 'losses' | 'halved' | 'winRate' = 'winRate';
  sortDir: 'asc' | 'desc' = 'desc';

  private readonly clubLogos = CLUB_LOGOS;

  private suggestSubject = new Subject<string>();

  constructor(private pennant: PennantService, private insights: InsightsService) {
    this.suggestSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => {
        if (!q || q.trim().length < 2) {
          this.suggestions.set([]);
          this.showSuggestions.set(false);
          return [];
        }
        return this.pennant.getClubSuggestions(q.trim());
      })
    ).subscribe(clubs => {
      this.suggestions.set(clubs);
      this.showSuggestions.set(clubs.length > 0);
    });
  }

  onQueryChange(): void {
    if (this.selectedClub) {
      this.selectedClub = null;
      this.allPlayers = [];
      this.hasSearched = false;
    }
    this.suggestSubject.next(this.query);
  }

  selectSuggestion(club: string): void {
    this.query = club;
    this.showSuggestions.set(false);
    this.suggestions.set([]);
    this.doSearch();
  }

  doSearch(): void {
    if (!this.query || this.query.trim().length < 2) return;
    this.showSuggestions.set(false);
    this.selectedClub = this.query.trim();
    this.insights.trackEvent('ClubSearch', { club: this.selectedClub });
    this.isLoading.set(true);
    this.isSlowResponse.set(false);
    clearTimeout(this.slowTimeout);
    this.slowTimeout = setTimeout(() => this.isSlowResponse.set(true), 2000);
    this.hasSearched = true;
    this.pennant.getClubPlayers(this.selectedClub).subscribe({
      next: players => {
        clearTimeout(this.slowTimeout);
        this.isSlowResponse.set(false);
        this.allPlayers = players;
        this.availableYears = [...new Set(players.map(p => p.year))].sort((a, b) => b - a);
        this.selectedYears = new Set(this.availableYears);
        this.availablePools = [...new Set(players.map(p => p.pool).filter(Boolean))].sort();
        this.selectedPools = new Set(this.availablePools);
        this.isLoading.set(false);
      },
      error: err => {
        clearTimeout(this.slowTimeout);
        this.isSlowResponse.set(false);
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  toggleYear(year: number): void {
    if (this.selectedYears.has(year)) {
      if (this.selectedYears.size === 1) return;
      this.selectedYears.delete(year);
    } else {
      this.selectedYears.add(year);
    }
    this.selectedYears = new Set(this.selectedYears);
  }

  togglePool(pool: string): void {
    if (this.selectedPools.has(pool)) {
      if (this.selectedPools.size === 1) return;
      this.selectedPools.delete(pool);
    } else {
      this.selectedPools.add(pool);
    }
    this.selectedPools = new Set(this.selectedPools);
  }

  setSort(col: typeof this.sortCol): void {
    if (this.sortCol === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortCol = col;
      this.sortDir = col === 'playerName' ? 'asc' : 'desc';
    }
  }
  
  get players(): ClubPlayer[] {
    const filtered = this.allPlayers.filter(p =>
      this.selectedYears.has(p.year) &&
      this.selectedPools.has(p.pool)
    );

    const map = new Map<string, { played: number; wins: number; losses: number; halved: number }>();
    for (const p of filtered) {
      const existing = map.get(p.playerName);
      if (existing) {
        existing.played  += p.played;
        existing.wins    += p.wins;
        existing.losses  += p.losses;
        existing.halved  += p.halved;
      } else {
        map.set(p.playerName, { played: p.played, wins: p.wins, losses: p.losses, halved: p.halved });
      }
    }

    const aggregated = Array.from(map.entries()).map(([playerName, stats]) => ({
      playerName,
      club: this.selectedClub ?? '',
      year: 0,
      pool: '',
      ...stats,
      winRate: stats.played > 0 ? Math.round(stats.wins / stats.played * 100 * 10) / 10 : 0,
    }));

    return aggregated.sort((a, b) => {
      let cmp = 0;
      if (this.sortCol === 'playerName') {
        cmp = a.playerName.localeCompare(b.playerName);
      } else {
        cmp = a[this.sortCol] - b[this.sortCol];
      }
      return this.sortDir === 'asc' ? cmp : -cmp;
    });
  }

  get totalPlayed(): number { 
    return this.players.reduce((s, p) => s + p.played, 0); 
  }
  
  get totalWins(): number { 
    return this.players.reduce((s, p) => s + p.wins, 0); 
  }

  get clubWinRate(): number {
    return this.totalPlayed > 0 ? Math.round(this.totalWins / this.totalPlayed * 100) : 0;
  }

  hideSuggestions(): void {
    setTimeout(() => this.showSuggestions.set(false), 200);
  }

  goToPlayer(playerName: string): void {
    this.playerSelected.emit(playerName);
  }

  yearCount(year: number): number {
    return this.allPlayers.filter(p =>
      p.year === year &&
      this.selectedPools.has(p.pool)
    ).length;
  }

  poolCount(pool: string): number {
    return this.allPlayers.filter(p =>
      p.pool === pool &&
      this.selectedYears.has(p.year)
    ).length;
  }

  getClubLogo(clubName: string): string | null {
    const logo = this.clubLogos[clubName];
    return logo || null;
  }
}