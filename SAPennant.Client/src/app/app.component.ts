import { Component, signal, OnInit } from '@angular/core';
import { PennantService } from './pennant.service';
import { retry } from 'rxjs/operators';

@Component({
  selector: 'sa-pennant-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.scss'
})
export class App implements OnInit {
  activeTab = signal<'search' | 'club' | 'leaderboard' | 'admin'>('search');
  selectedPlayer = signal('');
  isLoadingApi = signal(true);

  constructor(private pennant: PennantService) {}

  ngOnInit(): void {
    this.pennant.refreshLastUpdated().pipe(
      retry({ count: 10, delay: 3000 })
    ).subscribe({
      next: () => this.isLoadingApi.set(false),
      error: () => this.isLoadingApi.set(false)
    });
  }

  navigateToPlayer(name: string): void {
    this.pennant.pendingSearch.set(name);
    this.activeTab.set('search');
  }

  get lastUpdated() {
     return this.pennant.lastUpdated; 
  }
}