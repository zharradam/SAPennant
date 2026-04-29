import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class LoggingService {
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
    this.http.post(`${environment.apiUrl}/log`, { level, message, context })
      .subscribe({ error: () => {} });
  }
}