import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Garante que requisições à própria API carreguem cookies (cookie httpOnly de sessão).
 */
export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }
  return next(req.clone({ withCredentials: true }));
};
