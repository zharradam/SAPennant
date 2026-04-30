import { Component, OnInit, signal, ViewChild } from '@angular/core';
import { PennantService } from '../pennant.service';
import { PlayerMatch } from '../models/pennant.models';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { InsightsService } from '../insights.service';
import { getClubLogo } from '../data/club-logos';
import { LoggingService } from '../logging.service';
import { PlayerStatsComponent } from '../player-stats/player-stats.component';

@Component({
  selector: 'sa-pennant-search',
  standalone: false,
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
})
export class SearchComponent implements OnInit {
  @ViewChild('statsRef') statsRef!: PlayerStatsComponent;

  query = '';
  allResults: PlayerMatch[] = [];
  isLoading = signal(false);
  isSlowResponse = signal(false);
  hasSearched = false;
  suggestions = signal<string[]>([]);
  showSuggestions = signal(false);

  slowTimeout: any = null;

  private suggestSubject = new Subject<string>();
  private searchSubject = new Subject<string>();
  private searchSource = 'search';

  constructor(
    private pennant: PennantService,
    private insights: InsightsService,
    private logging: LoggingService
  ) {
    this.searchSubject.pipe(
      debounceTime(0),
      distinctUntilChanged(),
      switchMap(q => {
        if (!q || q.length < 2) {
          this.allResults = [];
          this.hasSearched = false;
          this.isLoading.set(false);
          this.clearPlayerUrl();
          return [];
        }
        this.isLoading.set(true);
        this.isSlowResponse.set(false);
        clearTimeout(this.slowTimeout);
        this.slowTimeout = setTimeout(() => this.isSlowResponse.set(true), 2000);
        this.hasSearched = true;
        return this.pennant.search(q, this.searchSource);
      })
    ).subscribe({
      next: (data) => {
        clearTimeout(this.slowTimeout);
        this.isSlowResponse.set(false);
        this.handleSearchResults(data);
      },
      error: (err) => {
        clearTimeout(this.slowTimeout);
        this.isSlowResponse.set(false);
        this.logging.error(`Search failed for "${this.query}": ${err?.message ?? err}`, 'SearchComponent');
        this.isLoading.set(false);
      }
    });

    this.suggestSubject.pipe(
      debounceTime(300),
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
    const params = new URLSearchParams(window.location.search);
    const urlPlayer = params.get('player');
    if (urlPlayer) {
      this.query = urlPlayer;
      this.executeSearch(urlPlayer, 'url');
      return;
    }

    const pending = this.pennant.pendingSearch();
    if (pending) {
      this.query = pending;
      this.pennant.pendingSearch.set('');
      this.executeSearch(this.query, 'leaderboard');
    }
  }

  onQueryChange(): void {
    this.suggestSubject.next(this.query);
    if (this.allResults.length > 0) {
      this.allResults = [];
      this.hasSearched = false;
      this.clearPlayerUrl();
    }
  }

  doSearch(): void {
    this.searchSource = 'search';
    this.showSuggestions.set(false);
    this.searchSubject.next(this.query);
  }

  selectSuggestion(name: string): void {
    this.logging.info(`Suggestion selected: "${name}"`, 'SearchComponent');
    this.searchSource = 'suggestion';
    this.query = name;
    this.showSuggestions.set(false);
    this.suggestions.set([]);
    this.searchSubject.next(name);
  }

  hideSuggestions(): void {
    setTimeout(() => this.showSuggestions.set(false), 200);
  }

  get playerClub(): string {
    if (this.allResults.length === 0) return '';
    return [...this.allResults]
      .sort((a, b) => b.year - a.year || (b.sortDate ?? '').localeCompare(a.sortDate ?? ''))[0].playerClub ?? '';
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

  getClubLogoUrl(): string | null {
    return getClubLogo(this.playerClub);
  }

  private setPlayerUrl(name: string): void {
    const url = `${window.location.pathname}?player=${encodeURIComponent(name)}`;
    history.replaceState({}, '', url);
  }

  private clearPlayerUrl(): void {
    history.replaceState({}, '', window.location.pathname);
  }

  private executeSearch(query: string, source: string = 'search'): void {
    this.searchSource = source;
    this.isLoading.set(true);
    this.isSlowResponse.set(false);
    clearTimeout(this.slowTimeout);
    this.slowTimeout = setTimeout(() => this.isSlowResponse.set(true), 2000);
    this.hasSearched = true;
    this.pennant.search(query, source).subscribe({
      next: (data) => {
        clearTimeout(this.slowTimeout);
        this.isSlowResponse.set(false);
        this.handleSearchResults(data);
      },
      error: (err) => {
        clearTimeout(this.slowTimeout);
        this.isSlowResponse.set(false);
        this.logging.error(`Search failed for "${query}": ${err?.message ?? err}`, 'SearchComponent');
        this.isLoading.set(false);
      }
    });
  }

  private handleSearchResults(data: PlayerMatch[]): void {
    this.insights.trackEvent('PlayerSearch', {
      query: this.query,
      source: this.searchSource
    });
    this.logging.info(`Player search: "${this.query}" via ${this.searchSource} — ${data.length} results`, 'SearchComponent');
    this.allResults = data;
    this.isLoading.set(false);
    if (data.length > 0) {
      this.setPlayerUrl(this.playerName);
    }
  }
}