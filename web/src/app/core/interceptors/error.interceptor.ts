import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const message = err.error?.message ?? err.message ?? 'Erro inesperado.';
      return throwError(() => ({ status: err.status, code: err.error?.error ?? 'unknown', message }));
    })
  );
};
