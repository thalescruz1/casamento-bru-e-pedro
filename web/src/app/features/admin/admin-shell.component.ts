import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="admin-top">
      <div class="admin-top__brand">
        <p class="eyebrow">Painel</p>
        <h1 class="script">Bruna e Pedro</h1>
        @if (auth.currentUser(); as user) {
          <p class="who">
            Olá, {{ user.displayName }} — <button type="button" (click)="logout()">sair</button>
          </p>
        }
      </div>
      <nav>
        <a routerLink="received" routerLinkActive="active">Recebidos</a>
        <a routerLink="rsvps" routerLinkActive="active">Confirmações</a>
        <a routerLink="gifts" routerLinkActive="active">Presentes</a>
        <a routerLink="contributions" routerLinkActive="active">Contribuições</a>
      </nav>
    </header>
    <main class="admin-body">
      <router-outlet />
    </main>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--paper); }

    .admin-top {
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
      gap: 32px;
      padding: 28px 40px 20px;
      background: var(--white);
      border-bottom: 1px solid var(--line);
      flex-wrap: wrap;
    }

    .admin-top__brand { display: flex; flex-direction: column; gap: 2px; }

    .admin-top h1 {
      font-family: var(--script);
      font-weight: 400;
      color: var(--blue);
      font-size: 46px;
      line-height: 1;
      margin-top: 2px;
    }

    .who {
      font-family: var(--body);
      font-size: 12px;
      color: var(--ink-soft);
      margin-top: 8px;
    }
    .who button {
      text-decoration: underline;
      color: var(--blue-deep);
      padding: 0;
      font: inherit;
      background: transparent;
      border: 0;
      cursor: pointer;

      &:hover { color: var(--blue-ink); }
    }

    nav { display: flex; gap: 4px; flex-wrap: wrap; }
    nav a {
      font-family: var(--body);
      font-weight: 500;
      font-size: 11px;
      letter-spacing: 0.24em;
      text-transform: uppercase;
      color: var(--ink-soft);
      padding: 8px 16px;
      border: 1px solid transparent;
      border-radius: 999px;
      transition: background 0.2s, color 0.2s, border-color 0.2s;
    }
    nav a:hover { color: var(--blue-deep); }
    nav a.active {
      color: var(--white);
      background: var(--blue-deep);
      border-color: var(--blue-deep);
    }

    .admin-body { padding: 40px 40px 80px; max-width: 1280px; margin: 0 auto; }

    @media (max-width: 700px) {
      .admin-top { flex-direction: column; align-items: flex-start; padding: 24px 24px 20px; }
      .admin-body { padding: 32px 24px 60px; }
      .admin-top h1 { font-size: 38px; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminShellComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigateByUrl('/admin/login')
    });
  }
}
