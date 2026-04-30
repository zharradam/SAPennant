import { Component, OnInit, signal } from '@angular/core';
import { PennantService } from '../pennant.service';

@Component({
  selector: 'sa-pennant-team',
  standalone: false,
  templateUrl: './team-pennant.component.html',
  styleUrl: './team-pennant.component.scss',
})
export class TeamPennantComponent implements OnInit {
  isLoadingLeaderboard = signal(false);
  isLoadingRound = signal(false);
  isLoadingMatch = signal(false);

  leaderboard = signal<any[]>([]);
  roundMatches = signal<any[]>([]);
  expandedMatch = signal<any | null>(null);
  expandedMatchPlayers = signal<any[]>([]);
  champion = signal<any>(null);

  years: number[] = [];
  pools: string[] = [];
  rounds: string[] = [];

  selectedYear = new Date().getFullYear();
  selectedPool = '';
  selectedRound = '';

  divisions: string[] = [];
  divisionPools: Record<string, string[]> = {};
  selectedDivision = '';
  filteredPools: string[] = [];

  expandedClub = signal<string | null>(null);
  expandedClubRounds = signal<any[]>([]);
  isLoadingClubRounds = signal(false);
  activeRound = signal<string | null>(null);
  finalists = signal<string[]>([]);
  selectedPlayerModal = signal<string | null>(null);

  constructor(private pennant: PennantService) {}

  ngOnInit(): void {
    this.pennant.getFilters(new Date().getFullYear()).subscribe({
      next: filters => {
        this.years = filters.years;
        this.divisions = filters.divisions;
        this.divisionPools = filters.divisionPools;

        // Set selected year to the first (latest) year returned
        if (this.years.length > 0) {
          this.selectedYear = this.years[0];
        }

        this.pools = filters.pools;
        this.filteredPools = filters.pools;

        if (this.filteredPools.length > 0) {
          this.selectedPool = this.filteredPools.includes('Simpson Cup') 
            ? 'Simpson Cup' 
            : this.filteredPools[0];
          this.loadRoundsList();
        }
      },
      error: () => {
        setTimeout(() => this.ngOnInit(), 3000);
      }
    });
  }

  load(): void {
    if (!this.selectedYear || !this.selectedPool) return;
    this.expandedMatch.set(null);
    this.expandedMatchPlayers.set([]);
    this.expandedClub.set(null);
    this.expandedClubRounds.set([]);
    this.pennant.getFilters(this.selectedYear).subscribe(filters => {
      this.pools = filters.pools;
      this.divisionPools = filters.divisionPools;
      this.divisions = filters.divisions;

      // Reset division if it no longer exists for the selected year
      if (this.selectedDivision && !this.divisions.includes(this.selectedDivision)) {
        this.selectedDivision = '';
      }

      this.filteredPools = this.selectedDivision
        ? (this.divisionPools[this.selectedDivision] ?? filters.pools)
        : filters.pools;
      if (!this.filteredPools.includes(this.selectedPool)) {
        this.selectedPool = this.filteredPools.includes('Simpson Cup') ? 'Simpson Cup' : this.filteredPools[0];
      }
      this.loadRoundsList();
    });
  }

  loadRoundsList(): void {
    this.pennant.getTeamRoundsList(this.selectedYear, this.selectedPool).subscribe({
      next: (rounds) => {
        this.rounds = rounds;
        this.selectedRound = rounds.length > 0 ? rounds[0] : '';
        this.loadLeaderboard();
        this.loadRound();
        this.pennant.getTeamChampion(this.selectedYear, this.selectedPool).subscribe({
          next: (data) => this.champion.set(data),
          error: () => this.champion.set(null)
        });
      },
      error: () => {}
    });

    this.pennant.getActiveRound(this.selectedYear, this.selectedPool).subscribe({
      next: (data) => this.activeRound.set(data.activeRound),
      error: () => this.activeRound.set(null)
    });
  }

  loadLeaderboard(): void {
    this.isLoadingLeaderboard.set(true);
    this.pennant.getTeamLeaderboard(this.selectedYear, this.selectedPool).subscribe({
      next: (data) => {
        this.leaderboard.set(data);
        this.isLoadingLeaderboard.set(false);
      },
      error: () => this.isLoadingLeaderboard.set(false)
    });

    // Load finalists
    this.pennant.getFinalists(this.selectedYear, this.selectedPool).subscribe({
      next: (data) => this.finalists.set(data.finalists),
      error: () => this.finalists.set([])
    });
  }

  loadRound(): void {
    if (!this.selectedRound) return;
    this.isLoadingRound.set(true);
    this.expandedMatch.set(null);
    this.expandedClub.set(null);
    this.expandedClubRounds.set([]);
    this.pennant.getTeamRound(this.selectedYear, this.selectedPool, this.selectedRound).subscribe({
      next: (data) => {
        this.roundMatches.set(data);
        this.isLoadingRound.set(false);
      },
      error: () => this.isLoadingRound.set(false)
    });
  }

  toggleMatch(match: any): void {
    const current = this.expandedMatch();
    if (current && current.homeClub === match.homeClub && current.awayClub === match.awayClub) {
      this.expandedMatch.set(null);
      this.expandedMatchPlayers.set([]);
      return;
    }

    this.expandedMatch.set(match);
    this.expandedMatchPlayers.set([]);
    this.isLoadingMatch.set(true);

    this.pennant.getTeamMatch(this.selectedYear, this.selectedPool, this.selectedRound, match.homeClub, match.awayClub).subscribe({
      next: (data) => {
        this.expandedMatchPlayers.set(data);
        this.isLoadingMatch.set(false);
      },
      error: () => this.isLoadingMatch.set(false)
    });
  }

  isExpanded(match: any): boolean {
    const current = this.expandedMatch();
    return !!current && current.homeClub === match.homeClub && current.awayClub === match.awayClub;
  }

  formatScore(home: number, away: number): string {
    return `${home % 1 === 0 ? home : home.toFixed(1)} - ${away % 1 === 0 ? away : away.toFixed(1)}`;
  }

  getMatchResult(home: number, away: number): 'home' | 'away' | 'tied' {
    if (home > away) return 'home';
    if (away > home) return 'away';
    return 'tied';
  }

  getArrowWidth(home: number, away: number): number {
    const margin = Math.abs(home - away);
    const maxMargin = 7;  // ← maximum possible margin (7 points)
    const maxWidth = 150; // ← max arrow length in px — increase this to make arrows longer
    return Math.max(Math.round((margin / maxMargin) * maxWidth), 20); // ← minimum arrow length
  }

  formatResult(result: string, playerWon: boolean | null): string {
    const formatted = PennantService.formatResult(result, playerWon);
    if (playerWon === true) return `won ${formatted}`;
    if (playerWon === false) return `lost ${formatted}`;
    return 'halved';
  }

  toggleClub(club: string): void {
    if (this.expandedClub() === club) {
      this.expandedClub.set(null);
      this.expandedClubRounds.set([]);
      return;
    }
    this.expandedClub.set(club);
    this.expandedClubRounds.set([]);
    this.isLoadingClubRounds.set(true);
    this.pennant.getClubRounds(this.selectedYear, this.selectedPool, club).subscribe({
      next: (data) => {
        this.expandedClubRounds.set(data);
        this.isLoadingClubRounds.set(false);
      },
      error: () => this.isLoadingClubRounds.set(false)
    });
  }

  isClubExpanded(club: string): boolean {
    return this.expandedClub() === club;
  }

  getClubArrowWidth(clubPoints: number, opponentPoints: number): number {
    const margin = Math.abs(clubPoints - opponentPoints);
    const maxMargin = 7;
    const maxWidth = 60;
    return Math.max(Math.round((margin / maxMargin) * maxWidth), 12);
  }

  navigateToRound(round: string, opponent: string, isHome: boolean): void {
    this.selectedRound = round;
    this.pennant.getTeamRound(this.selectedYear, this.selectedPool, round).subscribe({
      next: (data) => {
        this.roundMatches.set(data);
        this.isLoadingRound.set(false);
        // find and expand the relevant match
        const match = data.find((m: any) => 
          isHome ? m.awayClub === opponent : m.homeClub === opponent
        );
        if (match) {
          this.toggleMatch(match);
        }
      },
      error: () => this.isLoadingRound.set(false)
    });
    this.isLoadingRound.set(true);
    this.expandedMatch.set(null);
    this.expandedMatchPlayers.set([]);
  }

  onDivisionChange(): void {
    this.expandedClub.set(null);
    this.expandedClubRounds.set([]);  
    this.filteredPools = this.selectedDivision
      ? (this.divisionPools[this.selectedDivision] ?? this.pools)
      : this.pools;
    this.selectedPool = this.filteredPools.includes('Simpson Cup') ? 'Simpson Cup' : this.filteredPools[0];
    this.load();
  }

  isFinalist(club: string): boolean {
    return this.finalists().includes(club);
  }

  isLastFinalist(club: string, index: number): boolean {
    if (!this.isFinalist(club)) return false;
    const nextRow = this.leaderboard()[index + 1];
    return nextRow ? !this.isFinalist(nextRow.club) : false;
  }

  openPlayerModal(name: string): void {
    this.selectedPlayerModal.set(name);
  }

  closePlayerModal(): void {
    this.selectedPlayerModal.set(null);
  }
}