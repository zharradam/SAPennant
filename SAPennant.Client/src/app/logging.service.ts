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

  // Anonymous identity: visitor id persists on this browser (localStorage),
  // session id lasts one browsing session per tab (sessionStorage). Both are
  // random — no personal information. Whether each id had to be created just
  // now tells us if this is a first visit, a returning visitor, or a reload.
  private readonly visitor = LoggingService.stableId(() => localStorage, 'sapennant_visitor', 6);
  private readonly session = LoggingService.stableId(() => sessionStorage, 'sapennant_session', 4);

  constructor(private http: HttpClient) {}

  get visitType(): 'first visit' | 'return' | 'reload' {
    if (this.visitor.isNew) return 'first visit';
    return this.session.isNew ? 'return' : 'reload';
  }

  private static stableId(store: () => Storage, key: string, length: number): { id: string; isNew: boolean } {
    try {
      const s = store();
      const existing = s.getItem(key);
      if (existing) return { id: existing, isNew: false };
      const id = crypto.randomUUID().replace(/-/g, '').slice(0, length);
      s.setItem(key, id);
      return { id, isNew: true };
    } catch {
      // storage blocked (private mode) — server falls back to IP hash
      return { id: '', isNew: false };
    }
  }

  info(message: string, context?: string) {
    this.send('info', message, context);
  }

  warn(message: string, context?: string) {
    this.send('warn', message, context);
  }

  error(message: string, context?: string) {
    this.send('error', message, context);
  }

  /// Structured usage event — shows up in Grafana under category=usage.
  /// `event` is a short verb (visit, tab, search, player, club, ...);
  /// `data` is the human detail.
  usage(event: string, data?: string) {
    this.send('usage', data ?? '', event);
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
      this.http.post(`${environment.apiUrl}/log`, {
        level, message, context,
        visitorId: this.visitor.id,
        sessionId: this.session.id
      }).subscribe({ error: () => {} });
    } catch {
      // Swallow — logging must never break the app.
    }
  }
}
