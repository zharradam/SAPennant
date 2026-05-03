import { ErrorHandler, Injectable } from '@angular/core';
import { LoggingService } from './logging.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  constructor(private logging: LoggingService) {}

  handleError(error: any): void {
    const message = error?.message ?? String(error);
    const stack = error?.stack ?? 'no stack';
    const url = window.location.href;
    const userAgent = navigator.userAgent;
    const timestamp = new Date().toISOString();
    const online = navigator.onLine;
    const errorType = error?.name ?? 'UnknownError';

    const originalError = error?.ngOriginalError;
    const originalMessage = originalError?.message ?? null;
    const originalStack = originalError?.stack ?? null;

    // Auto-recover from stale chunk errors
    const isChunkError = message.includes('Loading chunk') || 
                         message.includes('ChunkLoadError');
    if (isChunkError) {
      this.logging.warn('Chunk load error detected — reloading', 'GlobalErrorHandler');
      window.location.reload();
      return;
    }

    const fullMessage = [
      `[${timestamp}]`,
      `Type: ${errorType}`,
      `Message: ${message}`,
      originalMessage ? `Original: ${originalMessage}` : null,
      `URL: ${url}`,
      `Online: ${online}`,
      `UA: ${userAgent}`,
      `Stack: ${stack}`,
      originalStack ? `Original stack: ${originalStack}` : null,
    ].filter(Boolean).join(' | ');

    this.logging.error(fullMessage, 'GlobalErrorHandler');
    console.error(error);
  }
}