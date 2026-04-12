import { Component, OnInit, signal } from '@angular/core';
import { PennantService } from '../pennant.service';

@Component({
  selector: 'sa-pennant-honour-roll',
  standalone: false,
  templateUrl: './honour-roll.component.html',
  styleUrl: './honour-roll.component.scss',
})
export class HonourRollComponent implements OnInit {
  isLoading = signal(false);
  results = signal<any[]>([]);
  narratives: Record<string, string> = {};
  narrativeExpanded = false;

  competitions: string[] = [];
  pools: string[] = [];
  clubs: string[] = [];

  selectedCompetition = 'Men\'s';
  selectedPool = '';
  selectedYear: number | undefined = undefined;
  selectedClub = '';

  years: number[] = [];
  groupBy: 'year' | 'pool' = 'year';

  constructor(private pennant: PennantService) {}

  ngOnInit(): void {
    this.pennant.getHonourRollNarratives().subscribe(data => {
      data.forEach(n => this.narratives[n.competition] = n.narrative);
    });

    this.pennant.getHonourRollFilters().subscribe(filters => {
      this.competitions = filters.competitions;
      this.clubs = filters.clubs;
      this.loadFiltersForCompetition();
      this.load();
    });
  }

  loadFiltersForCompetition(): void {
    this.pennant.getHonourRollFilters(this.selectedCompetition).subscribe(filters => {
      this.pools = filters.pools;
    });
  }

  onCompetitionChange(): void {
    this.selectedPool = '';
    this.selectedYear = undefined;
    this.narrativeExpanded = false;
    this.loadFiltersForCompetition();
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.pennant.getHonourRoll({
      competition: this.selectedCompetition || undefined,
      pool: this.selectedPool || undefined,
      year: this.selectedYear,
      club: this.selectedClub || undefined,
    }).subscribe({
      next: (data) => {
        this.results.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  get currentNarrative(): string {
    return this.narratives[this.selectedCompetition] ?? '';
  }

  get narrativeTitle(): string {
    return `About ${this.selectedCompetition}'s Pennant`;
  }

  toggleNarrative(): void {
    this.narrativeExpanded = !this.narrativeExpanded;
  }

  // Group results by year for display
  get groupedResults(): { year: number; entries: any[] }[] {
    const map = new Map<number, any[]>();
    for (const r of this.results()) {
      if (!map.has(r.year)) map.set(r.year, []);
      map.get(r.year)!.push(r);
    }
    return Array.from(map.entries())
      .map(([year, entries]) => ({ year, entries }))
      .sort((a, b) => b.year - a.year);
  }

  get uniqueYears(): number[] {
    return [...new Set(this.results().map(r => r.year))].sort((a, b) => b - a);
  }

  onGroupByChange(): void {
  // no reload needed, just re-renders
  }

  get groupedByPool(): { pool: string; entries: any[] }[] {
    const map = new Map<string, any[]>();
    for (const r of this.results()) {
      const key = r.pool;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(r);
    }
    return Array.from(map.entries())
      .map(([pool, entries]) => ({ pool, entries: entries.sort((a, b) => b.year - a.year) }))
      .sort((a, b) => a.pool.localeCompare(b.pool));
  }
}