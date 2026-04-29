import { ErrorHandler, Injectable } from '@angular/core';
import { LoggingService } from './logging.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  constructor(private logging: LoggingService) {}

  handleError(error: any): void {
    this.logging.error(error?.message ?? String(error), 'GlobalErrorHandler');
    console.error(error);
  }
}