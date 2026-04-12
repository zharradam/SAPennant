import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map } from 'rxjs';
import { PlayerMatch, ClubPlayer } from './models/pennant.models';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PennantService {
  private readonly API_URL = environment.apiUrl;
  pendingSearch = signal('');
  lastUpdated = signal('');
  searchMode = signal<'player' | 'club'>('player');

  constructor(private http: HttpClient) {}

  search(query: string, source: string = 'search'): Observable<PlayerMatch[]> {
    return this.http.get<PlayerMatch[]>(
      `${this.API_URL}/search?q=${query}&source=${source}`
    );
}

  getSuggestions(query: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.API_URL}/search/suggestions?q=${query}`);
  }

  getFilters(year?: number): Observable<any> {
    const query = year ? `?year=${year}` : '';
    return this.http.get<any>(`${this.API_URL}/search/filters${query}`);
  }

  getLeaderboard(params: {
    year?: number;
    division?: string;
    pool?: string;
    minGames?: number;
  }): Observable<any[]> {
    const query = new URLSearchParams();
    if (params.year) query.set('year', params.year.toString());
    if (params.division) query.set('division', params.division);
    if (params.pool) query.set('pool', params.pool);
    if (params.minGames) query.set('minGames', params.minGames.toString());
    return this.http.get<any[]>(`${this.API_URL}/search/leaderboard?${query.toString()}`);
  }

  getLastUpdated(): Observable<{ lastUpdated: string; display: string }> {
    return this.http.get<{ lastUpdated: string; display: string }>(`${this.API_URL}/search/last-updated`);
  }

  getAdminSeasons(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/sync/seasons`);
  }

  refreshYear(year: number): Observable<any> {
    return this.http.post<any>(`${this.API_URL}/sync/refresh/${year}`, {});
  }

  updateFinalsId(year: number, finalsId: number): Observable<any> {
    return this.http.put<any>(`${this.API_URL}/sync/seasons/${year}/finals-id`, { finalsId });
  }

  syncAll(): Observable<any> {
    return this.http.post<any>(`${this.API_URL}/sync/run`, {});
  }

  login(username: string, password: string): Observable<{ token: string }> {
    return this.http.post<{ token: string }>(`${this.API_URL}/auth/login`, { username, password });
  }

  getClubSuggestions(query: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.API_URL}/search/clubs/search?q=${encodeURIComponent(query)}`);
  }

  getClubPlayers(clubName: string, params: {
    year?: number;
    division?: string;
    pool?: string;
  } = {}): Observable<ClubPlayer[]> {
    const query = new URLSearchParams();
    query.set('clubName', clubName);
    if (params.year) query.set('year', params.year.toString());
    if (params.division) query.set('division', params.division);
    if (params.pool) query.set('pool', params.pool);

    return this.http.get<ClubPlayer[]>(
      `${this.API_URL}/search/clubs/players?${query.toString()}`
    );
  }

  refreshLastUpdated(): Observable<void> {
    return this.getLastUpdated().pipe(
      tap(data => this.lastUpdated.set(data.display)),
      map(() => void 0)
    );
  }

  getHandicapLeaderboard(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/search/handicap-leaderboard`);
  }

  getHandicapHistory(playerName: string): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.API_URL}/search/handicap-history/${encodeURIComponent(playerName)}`
    );
  }

  getTeamLeaderboard(year: number, pool: string): Observable<any[]> {
    const query = new URLSearchParams();
    query.set('year', year.toString());
    query.set('pool', pool);
    return this.http.get<any[]>(`${this.API_URL}/teampennant/leaderboard?${query.toString()}`);
  }

  getTeamRound(year: number, pool: string, round: string): Observable<any[]> {
    const query = new URLSearchParams();
    query.set('year', year.toString());
    query.set('pool', pool);
    query.set('round', round);
    return this.http.get<any[]>(`${this.API_URL}/teampennant/rounds?${query.toString()}`);
  }

  getTeamMatch(year: number, pool: string, round: string, home: string, away: string): Observable<any[]> {
    const query = new URLSearchParams();
    query.set('year', year.toString());
    query.set('pool', pool);
    query.set('round', round);
    query.set('home', home);
    query.set('away', away);
    return this.http.get<any[]>(`${this.API_URL}/teampennant/match?${query.toString()}`);
  }

  getTeamRoundsList(year: number, pool: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.API_URL}/teampennant/rounds-list?year=${year}&pool=${pool}`);
  }

  getTeamChampion(year: number, pool: string): Observable<any> {
    return this.http.get<any>(`${this.API_URL}/teampennant/champion?year=${year}&pool=${pool}`);
  }

  getClubRounds(year: number, pool: string, club: string): Observable<any[]> {
    const query = new URLSearchParams();
    query.set('year', year.toString());
    query.set('pool', pool);
    query.set('club', club);
    return this.http.get<any[]>(`${this.API_URL}/teampennant/club-rounds?${query.toString()}`);
  }

  static formatResult(result: string, playerWon: boolean | null): string {
    if (!result) return '';
    const r = result.trim();
    if (r.match(/^\d+ Hole/i)) {
      const holes = r.match(/^(\d+)/)?.[1] ?? '';
      if (playerWon === true) return `${holes} up`;
      if (playerWon === false) return `${holes} down`;
      return 'Halved';
    }
    if (r === 'A/S') return 'Halved';
    return r;
  }

  syncUnsettled(): Observable<any> {
    return this.http.post<any>(`${this.API_URL}/sync/sync-unsettled`, {});
  }

  getSyncStatus(): Observable<{ enabled: boolean }> {
    return this.http.get<{ enabled: boolean }>(`${this.API_URL}/sync/sync-status`);
  }

  toggleSync(enabled: boolean): Observable<{ enabled: boolean }> {
    return this.http.post<{ enabled: boolean }>(`${this.API_URL}/sync/sync-toggle`, enabled);
  }

  getActiveRound(year: number, pool: string): Observable<{ activeRound: string | null }> {
    return this.http.get<{ activeRound: string | null }>(`${this.API_URL}/teampennant/active-round?year=${year}&pool=${pool}`);
  }
}