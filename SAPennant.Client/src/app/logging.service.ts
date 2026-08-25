import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class LoggingService {
  // Cap how many log entries we ship per minute so a misbehaving loop
  // (or an error handler reacting to our own failed log POSTs) can't
  // flood the API.
  private static readonly MAX_PER_MINUTE = 20;
  private windowStart = 0;
  private sentInWindow = 0;

  constructor(private http: HttpClient) {}

  info(message: string, context?: string) {
    this.send('info', message, context);
  }

  warn(message: string, context?: string) {
    this.send('warn', message, context);
  }

  error(message: string, context?: string) {
    this.send('error', message, context);
  }

  private send(level: string, message: string, context?: string) {
    // Never log errors about the log endpoint itself — that's how loops start.
    if (message.includes('/api/log')) return;

    const now = Date.now();
    if (now - this.windowStart > 60_000) {
      this.windowStart = now;
      this.sentInWindow = 0;
    }
    if (++this.sentInWindow > LoggingService.MAX_PER_MINUTE) return;

    try {
      this.http.post(`${environment.apiUrl}/log`, { level, message, context })
        .subscribe({ error: () => {} });
    } catch {
      // Swallow — logging must never break the app.
    }
  }
}
