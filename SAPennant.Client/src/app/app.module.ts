import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, provideHttpClient, withInterceptors } from '@angular/common/http';

import { App } from './app.component';
import { SearchComponent } from './search/search.component';
import { LeaderboardComponent } from './leaderboard/leaderboard.component';
import { AdminComponent } from './admin/admin.component';
import { YearCountPipe } from './year-count.pipe';
import { PoolCountPipe } from './pool-count.pipe';
import { authInterceptor } from './auth.interceptor';
import { ClubSearchComponent } from './club-search/club-search.component';
import { HandicapComponent } from './handicap/handicap.component';
import { TeamPennantComponent } from './team-pennant/team-pennant.component';

@NgModule({
  declarations: [
    App,
    SearchComponent,
    LeaderboardComponent,
    AdminComponent,
    YearCountPipe,
    PoolCountPipe,
    ClubSearchComponent,
    HandicapComponent,
    TeamPennantComponent,
  ],
  imports: [BrowserModule, FormsModule, HttpClientModule],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
  bootstrap: [App],
})
export class AppModule {}
