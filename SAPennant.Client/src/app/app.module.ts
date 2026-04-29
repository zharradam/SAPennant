import { NgModule, provideBrowserGlobalErrorListeners, ErrorHandler } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, provideHttpClient, withInterceptors } from '@angular/common/http';
import { environment } from '../environments/environment';

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
import { HonourRollComponent } from './honour-roll/honour-roll.component';
import { ServiceWorkerModule } from '@angular/service-worker';
import { ScrollHintDirective } from './directives/scroll-hint.directive';
import { GlobalErrorHandler } from './global-error-handler';

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
    HonourRollComponent,
    ScrollHintDirective,
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpClientModule,
    ServiceWorkerModule.register('ngsw-worker.js', {
      enabled: environment.production,
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
  providers: [
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
  bootstrap: [App],
})
export class AppModule {}
