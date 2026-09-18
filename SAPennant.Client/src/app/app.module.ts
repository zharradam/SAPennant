import { NgModule, provideBrowserGlobalErrorListeners, ErrorHandler } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { App } from './app.component';
import { SearchComponent } from './search/search.component';
import { LeaderboardComponent } from './leaderboard/leaderboard.component';
import { AdminComponent } from './admin/admin.component';
import { authInterceptor } from './auth.interceptor';
import { ClubSearchComponent } from './club-search/club-search.component';
import { HandicapComponent } from './handicap/handicap.component';
import { TeamPennantComponent } from './team-pennant/team-pennant.component';
import { HonourRollComponent } from './honour-roll/honour-roll.component';
import { GlobalErrorHandler } from './global-error-handler';
import { PlayerStatsComponent } from './player-stats/player-stats.component';
import { PlayerModalComponent } from './player-modal/player-modal.component';

@NgModule({
  declarations: [
    App,
    SearchComponent,
    LeaderboardComponent,
    AdminComponent,
    ClubSearchComponent,
    HandicapComponent,
    TeamPennantComponent,
    HonourRollComponent,
    PlayerStatsComponent,
    PlayerModalComponent,
  ],
  imports: [
    BrowserModule,
    FormsModule,
  ],
  providers: [
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
  bootstrap: [App],
})
export class AppModule {}
