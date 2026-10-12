import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  // Attach the admin JWT to every call to our own API; protected endpoints
  // (sync, backup, ...) need it and anonymous ones ignore it.
  if (token && req.url.startsWith(environment.apiUrl)) {
    req = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
  }

  return next(req).pipe(
    catchError(err => {
      // Token rejected (expired/revoked): drop it so the UI shows logged out.
      if (err.status === 401) authService.logout();
      return throwError(() => err);
    })
  );
};
