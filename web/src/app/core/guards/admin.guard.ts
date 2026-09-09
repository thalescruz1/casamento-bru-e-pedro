import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.loadMe().pipe(
    map(user => {
      if (user) {
        return true;
      }
      const redirect = encodeURIComponent(state.url);
      return router.parseUrl(`/admin/login?redirect=${redirect}`) as UrlTree;
    })
  );
};
