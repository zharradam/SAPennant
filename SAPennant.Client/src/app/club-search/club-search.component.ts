import { Component, signal, Output, EventEmitter } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { PennantService } from '../pennant.service';
import { ClubPlayer } from '../models/pennant.models';

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

  availableYears: number[] = [];
  selectedYears = new Set<number>();
  availablePools: string[] = [];
  selectedPools = new Set<string>();

  sortCol: 'playerName' | 'played' | 'wins' | 'losses' | 'halved' | 'winRate' = 'winRate';
  sortDir: 'asc' | 'desc' = 'desc';

  private readonly clubLogos: Record<string, string> = {
    'Royal Adelaide Golf Club': 'assets/clubs/royal-adelaide.png',
    'Aston Hills Golf Club at Mount Barker': '',
    'Balaklava Golf Club': '',
    'Barmera Golf Club': '',
    'Barossa Valley Golf Club': '',
    'Berri Golf Club': '',
    'Blackwood Golf Club': '',
    'Blue Lake Golf Club': '',
    'Blyth Golf Club': '',
    'Booleroo Centre Golf Club': '',
    'Bordertown Golf Club': '',
    'Burra Golf Club': '',
    'Clare Golf Club': '',
    'Copperclub, The Dunes Port Hughes': '',
    'Echunga Golf Club': '',
    'Flagstaff Hill Golf Club': '',
    'Future Golf': '',
    'Glenelg Golf Club': '',
    'Highercombe Golf + Country Club': '',
    'Horsham Golf Club': '',
    'Kadina Golf Club': '',
    'Kapunda Golf Club': '',
    'Kingston SE Golf Club': '',
    'Kiwi Golf Club': '',
    'Kooyonga Golf Club': '',
    'Lameroo Golf Club': '',
    'Links Lady Bay Golf Club': '',
    'Loxton Golf Club': '',
    'Maitland Golf Club (SA)': '',
    'McCracken Country Club': '',
    'Mitsubishi Staff Golf Club': '',
    'Mount Compass Golf Club': '',
    'Mount Osmond Golf Club': '',
    'Mt Gambier Golf Club': '',
    'Murray Bridge Golf Club': '',
    'Naracoorte Golf Club': '',
    'North Adelaide Golf Club': '',
    'North Haven Golf Club': '',
    'Oakbank Golf Club': '',
    'Penfield Golf Club': '',
    'Peterborough Golf Club': '',
    'Pinnaroo Golf Club': '',
    'Playford Lakes Golf Club': '',
    'Port Augusta Golf Club': '',
    'Port Broughton Golf Club': '',
    'Regency Park Golf Club': '',
    'Robe Golf Club': '',
    'SA Police Golf Club': '',
    'Sandy Creek Golf Club': '',
    'South Lakes Golf Club': '',
    'Streaky Bay Golf Club': '',
    'Tanunda Pines Golf Club': '',
    'Tea Tree Gully Golf Club': '',
    'Thaxted Park Golf Club': '',
    'The Grange Golf Club (SA)': '',
    'The Gums Golf Club Salisbury': '',
    'The Stirling Golf Club (ex Mt Lofty)': '',
    'The Vines Golf Club of Reynella': '',
    'Victor Harbor Golf Club': '',
    'Waikerie Golf Club': '',
    'Waratah Golf Club': '',
    'West Lakes Golf Club': '',
    'Westward HO Golf Club': '',
    'Whyalla Golf Club': '',
    'Willunga Golf Club': '',
    'Yacka Golf Club': '',
  };

  private suggestSubject = new Subject<string>();

  constructor(private pennant: PennantService) {
    this.suggestSubject.pipe(
      debounceTime(50),
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
    this.isLoading.set(true);
    this.hasSearched = true;
    this.pennant.getClubPlayers(this.selectedClub).subscribe({
      next: players => {
        this.allPlayers = players;

        this.availableYears = [...new Set(players.map(p => p.year))].sort((a, b) => b - a);
        this.selectedYears = new Set(this.availableYears);

        this.availablePools = [...new Set(players.map(p => p.pool).filter(Boolean))].sort();
        this.selectedPools = new Set(this.availablePools);

        this.isLoading.set(false);
      },
      error: err => { console.error(err); this.isLoading.set(false); }
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