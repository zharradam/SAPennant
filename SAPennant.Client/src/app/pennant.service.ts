import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PlayerMatch, ClubPlayer } from './models/pennant.models';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PennantService {
  private readonly API_URL = environment.apiUrl;
  pendingSearch = signal('');
  searchMode = signal<'player' | 'club'>('player');

  constructor(private http: HttpClient) {}

  search(query: string): Observable<PlayerMatch[]> {
    return this.http.get<PlayerMatch[]>(`${this.API_URL}/search?q=${query}`);
  }

  getSuggestions(query: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.API_URL}/search/suggestions?q=${query}`);
  }

  getFilters(): Observable<any> {
    return this.http.get<any>(`${this.API_URL}/search/filters`);
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
    if (params.year) query.set('year', params.year.toString());
    if (params.division) query.set('division', params.division);
    if (params.pool) query.set('pool', params.pool);
    return this.http.get<ClubPlayer[]>(
      `${this.API_URL}/search/clubs/${encodeURIComponent(clubName)}/players?${query.toString()}`
    );
  }
}