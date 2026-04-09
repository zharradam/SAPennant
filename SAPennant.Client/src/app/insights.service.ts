import { Injectable } from '@angular/core';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class InsightsService {
  private appInsights: ApplicationInsights | null = null;

  constructor() {
    if (environment.appInsightsConnectionString) {
      this.appInsights = new ApplicationInsights({
        config: {
          connectionString: environment.appInsightsConnectionString,
          enableAutoRouteTracking: false
        }
      });
      this.appInsights.loadAppInsights();
    }
  }

  trackTabView(tabName: string): void {
    this.appInsights?.trackPageView({ name: tabName });
  }

  trackEvent(name: string, properties?: Record<string, string>): void {
    this.appInsights?.trackEvent({ name }, properties);
  }
}