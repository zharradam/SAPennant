import { Component, OnInit, signal } from '@angular/core';
import { PennantService } from '../pennant.service';
import { PlayerMatch } from '../models/pennant.models';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { InsightsService } from '../insights.service';
import { getClubLogo } from '../data/club-logos';

@Component({
  selector: 'sa-pennant-search',
  standalone: false,
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
})
export class SearchComponent implements OnInit {
  query = '';
  allResults: PlayerMatch[] = [];
  isLoading = signal(false);
  isSlowResponse = signal(false);
  hasSearched = false;
  availableYears: number[] = [];
  selectedYears = new Set<number>();
  availablePools: string[] = [];
  selectedPools = new Set<string>();
  suggestions = signal<string[]>([]);
  showSuggestions = signal(false);
  shareState = signal<'idle' | 'sharing' | 'copied'>('idle');

  slowTimeout: any = null;

  private suggestSubject = new Subject<string>();
  private searchSubject = new Subject<string>();
  private searchSource = 'search';

  constructor(private pennant: PennantService, private insights: InsightsService) {
    this.searchSubject.pipe(
      debounceTime(0),
      distinctUntilChanged(),
      switchMap(q => {
        if (!q || q.length < 2) {
          this.allResults = [];
          this.availableYears = [];
          this.selectedYears.clear();
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
        console.error(err);
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
    // Check for ?player= in URL first
    const params = new URLSearchParams(window.location.search);
    const urlPlayer = params.get('player');
    if (urlPlayer) {
      this.query = urlPlayer;
      this.executeSearch(urlPlayer, 'url');
      return;
    }

    // Fall back to pending search from leaderboard/club navigation
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
      this.availableYears = [];
      this.selectedYears.clear();
      this.availablePools = [];
      this.selectedPools.clear();
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
    this.searchSource = 'suggestion';
    this.query = name;
    this.showSuggestions.set(false);
    this.suggestions.set([]);
    this.searchSubject.next(name);
  }

  hideSuggestions(): void {
    setTimeout(() => this.showSuggestions.set(false), 200);
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

  get yearRange(): string {
    if (this.availableYears.length === 0) return '';
    const sorted = [...this.availableYears].sort();
    if (sorted.length === 1) return `${sorted[0]}`;
    return `${sorted[0]}–${sorted[sorted.length - 1]}`;
  }

  get allPools(): string {
    return [...new Set(this.allResults.map(m => m.pool))].sort().join(', ');
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
    return PennantService.formatResult(m.result, m.playerWon);
  }

  async sharePlayer(): Promise<void> {
    const name = this.playerName;
    const club = this.playerClub;
    const url = `https://sapennantgolf.com/?player=${encodeURIComponent(name)}`;
    const shareText = `${name} · ${club}\n${this.results.length} matches · ${this.wins}W ${this.losses}L ${this.halved}H · ${this.winRate}% win rate\n\n${url}`;

    const W = 600, H = 280;
    const canvas = document.createElement('canvas');
    canvas.width = W;
    canvas.height = H;
    const ctx = canvas.getContext('2d')!;

    // Background
    ctx.fillStyle = '#0f1e3d';
    ctx.fillRect(0, 0, W, H);

    // Avatar circle
    const initials = name.split(' ').map((n: string) => n[0]).slice(0, 2).join('').toUpperCase();
    const logoUrl = getClubLogo(club);

    if (logoUrl) {
      await new Promise<void>((resolve) => {
        const img = new Image();
        img.onload = () => {
          // White circle background
          ctx.fillStyle = '#ffffff';
          ctx.beginPath();
          ctx.arc(52, 45, 28, 0, Math.PI * 2);
          ctx.fill();
          ctx.save();
          ctx.beginPath();
          ctx.arc(52, 45, 26, 0, Math.PI * 2);
          ctx.clip();
          ctx.drawImage(img, 24, 17, 56, 56);
          ctx.restore();
          resolve();
        };
        img.onerror = () => resolve(); // fall back silently
        img.src = logoUrl;
      });
    } else {
      // Initials fallback
      ctx.fillStyle = 'rgba(255,255,255,0.15)';
      ctx.beginPath();
      ctx.arc(52, 45, 28, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 14px Arial';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(initials, 52, 45);
    }

    // Player name
    ctx.textAlign = 'left';
    ctx.textBaseline = 'alphabetic';
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 22px Arial';
    ctx.fillText(name, 92, 44);

    // Club + year range
    ctx.fillStyle = 'rgba(255,255,255,0.55)';
    ctx.font = '14px Arial';
    ctx.fillText(`${club} · ${this.yearRange}`, 92, 66);

    // Divider
    ctx.strokeStyle = 'rgba(255,255,255,0.12)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(24, 90);
    ctx.lineTo(W - 24, 90);
    ctx.stroke();

    // Stats
    const stats = [
      { label: 'PLAYED',   value: String(this.results.length), color: '#ffffff' },
      { label: 'WON',      value: String(this.wins),           color: '#5db83a' },
      { label: 'LOST',     value: String(this.losses),         color: '#e05555' },
      { label: 'HALVED',   value: String(this.halved),         color: '#c47d1a' },
      { label: 'WIN RATE', value: `${this.winRate}%`,          color: '#7eb8f5' },
    ];
    const colW = (W - 48) / stats.length;
    stats.forEach((s, i) => {
      const cx = 24 + colW * i + colW / 2;
      ctx.textAlign = 'center';
      ctx.fillStyle = s.color;
      ctx.font = 'bold 30px Arial';
      ctx.fillText(s.value, cx, 148);
      ctx.fillStyle = 'rgba(255,255,255,0.4)';
      ctx.font = '11px Arial';
      ctx.fillText(s.label, cx, 168);
    });

    // Divider
    ctx.strokeStyle = 'rgba(255,255,255,0.12)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(24, 185);
    ctx.lineTo(W - 24, 185);
    ctx.stroke();

    // Branding
    ctx.textAlign = 'left';
    ctx.fillStyle = 'rgba(255,255,255,0.3)';
    ctx.font = 'bold 11px Arial';
    ctx.fillText('SA PENNANT GOLF · SOUTH AUSTRALIA', 24, 210);

    // URL
    ctx.fillStyle = 'rgba(255,255,255,0.4)';
    ctx.font = '11px Arial';
    ctx.fillText(url, 24, 228);

    // Pools right-aligned
    ctx.textAlign = 'right';
    ctx.fillStyle = 'rgba(255,255,255,0.25)';
    ctx.font = '11px Arial';
    ctx.fillText(this.allPools, W - 24, 228);

    // Replace the entire toBlob callback with this:
    canvas.toBlob(async (blob) => {
      if (!blob || blob.size === 0) return;

      const file = new File([blob], 'pennant-stats.png', { type: 'image/png' });
      const isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);

      if (isMobile && navigator.share && navigator.canShare && navigator.canShare({ files: [file] })) {
        // Mobile — native share sheet with image
        try {
          this.shareState.set('sharing');
          await navigator.share({ files: [file], text: shareText });
          this.shareState.set('idle');
          return;
        } catch {
          this.shareState.set('idle');
        }
      }

      // Desktop — download the image + copy URL to clipboard
      const imageUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = imageUrl;
      a.download = `${name.replace(/\s+/g, '-').toLowerCase()}-pennant-stats.png`;
      a.click();
      URL.revokeObjectURL(imageUrl);

      try {
        await navigator.clipboard.writeText(`https://sapennantgolf.com/?player=${encodeURIComponent(name)}`);
        this.shareState.set('copied');
        setTimeout(() => this.shareState.set('idle'), 2000);
      } catch {
        this.shareState.set('idle');
      }
    }, 'image/png');
  }

  private roundRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number): void {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
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
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  private handleSearchResults(data: PlayerMatch[]): void {
    this.insights.trackEvent('PlayerSearch', {
      query: this.query,
      source: this.searchSource
    });
    this.allResults = data;
    this.availableYears = [...new Set(data.map(m => m.year))].sort((a, b) => b - a);
    this.selectedYears = new Set(this.availableYears);
    this.availablePools = [...new Set(data.map(m => m.pool))].sort();
    this.selectedPools = new Set(this.availablePools);
    this.isLoading.set(false);

    // Update URL with the resolved player name
    if (data.length > 0) {
      this.setPlayerUrl(this.playerName);
    }
  }

  getClubLogoUrl(): string | null {
    return getClubLogo(this.playerClub);
  }
}