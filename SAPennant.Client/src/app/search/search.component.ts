import { Component, signal } from '@angular/core';
import { PennantService } from '../pennant.service';
import { PlayerMatch } from '../models/pennant.models';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

@Component({
  selector: 'sa-pennant-search',
  standalone: false,
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
})
export class SearchComponent {
  query = '';
  allResults: PlayerMatch[] = [];
  isLoading = signal(false);
  hasSearched = false;
  availableYears: number[] = [];
  selectedYears = new Set<number>();
  availablePools: string[] = [];
  selectedPools = new Set<string>();
  suggestions = signal<string[]>([]);
  showSuggestions = signal(false);

  private suggestSubject = new Subject<string>();
  private searchSubject = new Subject<string>();

  constructor(private pennant: PennantService) {
    this.searchSubject.pipe(
      debounceTime(50),
      distinctUntilChanged(),
      switchMap(q => {
        if (!q || q.length < 2) {
          this.allResults = [];
          this.availableYears = [];
          this.selectedYears.clear();
          this.hasSearched = false;
          this.isLoading.set(false)
          return [];
        }
        this.isLoading.set(true)
        this.hasSearched = true;
        return this.pennant.search(q);
      })
    ).subscribe({
        next: (data) => this.handleSearchResults(data),
        error: (err) => {
          console.error(err);
          this.isLoading.set(false)
        }
      });

    this.suggestSubject.pipe(
      debounceTime(50),
      switchMap(q => {
        if (!q || q.trim().length < 2) {
          this.suggestions.set([]);
          this.showSuggestions.set(false);
          return [];
        }
        return this.pennant.getSuggestions(q.trim());
      })
    ).subscribe(names => {
      this.suggestions.set(names);
      this.showSuggestions.set(names.length > 0);
    });
  }

  ngOnInit(): void {
    const pending = this.pennant.pendingSearch();
    if (pending) {
      this.query = pending;
      this.pennant.pendingSearch.set('');
      this.executeSearch(this.query);
    }
  }

  onQueryChange(): void {
    this.suggestSubject.next(this.query);

    if (this.allResults.length > 0) {
      this.allResults = [];
      this.availableYears = [];
      this.selectedYears.clear();
      this.availablePools = [];
      this.selectedPools.clear();
      this.hasSearched = false;
    }
  }

  doSearch(): void {
    this.showSuggestions.set(false);
    this.searchSubject.next(this.query);
  }

  selectSuggestion(name: string): void {
    this.query = name;
    this.showSuggestions.set(false);
    this.suggestions.set([]);
    this.searchSubject.next(name);
  }

  hideSuggestions(): void {
    setTimeout(() => {
      this.showSuggestions.set(false);
    }, 200);
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

  get results(): PlayerMatch[] {
    return this.allResults.filter(m =>
      this.selectedYears.has(m.year) &&
      this.selectedPools.has(m.pool)
    );
  }

  get groupedResults(): { year: number; isFinals: boolean; matches: PlayerMatch[] }[] {
    const groups: { year: number; isFinals: boolean; matches: PlayerMatch[] }[] = [];
    const seen = new Map<string, PlayerMatch[]>();

    this.results.forEach(m => {
      const key = `${m.year}-${m.isFinals}`;
      if (!seen.has(key)) seen.set(key, []);
      seen.get(key)!.push(m);
    });

    seen.forEach((matches, key) => {
      const [year, isFinals] = key.split('-');
      groups.push({ year: +year, isFinals: isFinals === 'true', matches });
    });

    return groups.sort((a, b) => b.year - a.year || (a.isFinals ? -1 : 1));
  }

  get playerClub(): string {
    if (this.allResults.length === 0) return '';
    const freq = new Map<string, number>();
    this.allResults.map(m => m.playerClub).filter(Boolean)
      .forEach(c => freq.set(c, (freq.get(c) ?? 0) + 1));
    let max = 0, club = '';
    freq.forEach((count, c) => { if (count > max) { max = count; club = c; } });
    return club;
  }

  get playerName(): string {
    if (this.allResults.length === 0) return '';
    const freq = new Map<string, number>();
    this.allResults.map(m => m.playerName).filter(Boolean)
      .forEach(n => freq.set(n, (freq.get(n) ?? 0) + 1));
    let max = 0, name = '';
    freq.forEach((count, n) => { if (count > max) { max = count; name = n; } });
    return name;
  }

  get wins(): number {
    return this.results.filter(m => m.playerWon === true).length;
  }

  get losses(): number {
    return this.results.filter(m => m.playerWon === false).length;
  }

  get halved(): number {
    return this.results.filter(m => m.playerWon === null).length;
  }

  get winRate(): number {
    const total = this.wins + this.losses + this.halved;
    return total > 0 ? Math.round((this.wins / total) * 100) : 0;
  }

  get finalsRecord(): string {
    const finalsMatches = this.results.filter(m => m.isFinals);
    const finalsWins = finalsMatches.filter(m => m.playerWon === true).length;
    return finalsMatches.length > 0 ? `${finalsWins}/${finalsMatches.length}` : '—';
  }

  divisionRecord(matches: PlayerMatch[]): string {
    const wins = matches.filter(m => m.playerWon === true).length;
    const losses = matches.filter(m => m.playerWon === false).length;
    const halved = matches.filter(m => m.playerWon === null).length;
    const total = wins + losses + halved;
    const pct = total > 0 ? Math.round(wins / total * 100) : 0;
    return `${wins}W ${losses}L ${halved}H · ${pct}%`;
  }

  formatResult(m: PlayerMatch): string {
    if (!m.result) return '';
    const r = m.result.trim();
    if (r.match(/^\d+ Hole/i)) {
      const holes = r.match(/^(\d+)/)?.[1] ?? '';
      if (m.playerWon === true) return `${holes} Up`;
      if (m.playerWon === false) return `${holes} Down`;
      return 'Halved';
    }
    if (r === 'A/S') return 'Halved';
    return r;
  }

  private executeSearch(query: string): void {
    this.isLoading.set(true)
    this.hasSearched = true;
    this.pennant.search(query).subscribe({
      next: (data) => {
        this.allResults = data;
        this.availableYears = [...new Set(data.map(m => m.year))].sort((a, b) => b - a);
        this.selectedYears = new Set(this.availableYears);
        this.availablePools = [...new Set(data.map(m => m.pool))].sort();
        this.selectedPools = new Set(this.availablePools);
        this.isLoading.set(false)
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false)
      }
    });
  }

  private handleSearchResults(data: PlayerMatch[]): void {
    this.allResults = data;
    this.availableYears = [...new Set(data.map(m => m.year))].sort((a, b) => b - a);
    this.selectedYears = new Set(this.availableYears);
    this.availablePools = [...new Set(data.map(m => m.pool))].sort();
    this.selectedPools = new Set(this.availablePools);
    this.isLoading.set(false)
  }
}