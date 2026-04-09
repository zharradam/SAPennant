import { Component, signal, OnInit } from '@angular/core';
import { PennantService } from './pennant.service';

@Component({
  selector: 'sa-pennant-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.scss'
})
export class App implements OnInit {
  activeTab = signal<'search' | 'club' | 'leaderboard' | 'admin'>('search');
  selectedPlayer = signal('');

  constructor(private pennant: PennantService) {}

  ngOnInit(): void {
    this.pennant.refreshLastUpdated();
  }

  navigateToPlayer(name: string): void {
    this.pennant.pendingSearch.set(name);
    this.activeTab.set('search');
  }

  get lastUpdated() {
     return this.pennant.lastUpdated; 
  }
}