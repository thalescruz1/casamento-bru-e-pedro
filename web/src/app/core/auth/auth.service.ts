import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, tap } from 'rxjs';

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
}

interface LoginResponse {
  user: CurrentUser;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly user = signal<CurrentUser | null>(null);
  private readonly loaded = signal(false);

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);

  login(email: string, password: string): Observable<CurrentUser> {
    return this.http
      .post<LoginResponse>('/api/auth/login', { email, password }, { withCredentials: true })
      .pipe(
        tap(r => {
          this.user.set(r.user);
          this.loaded.set(true);
        }),
        map(r => r.user)
      );
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', {}, { withCredentials: true }).pipe(
      tap(() => {
        this.user.set(null);
        this.loaded.set(true);
      })
    );
  }

  /** Busca o usuário atual do cookie. Cacheado após a primeira chamada bem-sucedida ou 401. */
  loadMe(force = false): Observable<CurrentUser | null> {
    if (this.loaded() && !force) {
      return of(this.user());
    }

    return this.http.get<CurrentUser>('/api/auth/me', { withCredentials: true }).pipe(
      tap(user => {
        this.user.set(user);
        this.loaded.set(true);
      }),
      catchError(() => {
        this.user.set(null);
        this.loaded.set(true);
        return of(null);
      })
    );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(
      '/api/auth/change-password',
      { currentPassword, newPassword },
      { withCredentials: true }
    );
  }
}
