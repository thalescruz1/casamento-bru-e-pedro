import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="admin-top">
      <div>
        <h1>Painel · Bruna &amp; Pedro</h1>
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
    :host { display: block; min-height: 100vh; background: var(--paper-2); }
    .admin-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 24px;
      padding: 24px 32px;
      background: var(--paper);
      border-bottom: 1px solid rgba(20,17,13,0.12);
    }
    .admin-top h1 {
      font-family: var(--serif);
      font-weight: 400;
      font-size: 20px;
      letter-spacing: 0.22em;
      text-transform: uppercase;
    }
    .who {
      font-family: var(--italic);
      font-style: italic;
      color: var(--ink-soft);
      font-size: 13px;
      margin-top: 4px;
    }
    .who button {
      text-decoration: underline;
      color: inherit;
      padding: 0;
      font: inherit;
      background: transparent;
      border: 0;
      cursor: pointer;
    }
    nav { display: flex; gap: 16px; }
    nav a {
      font-family: var(--serif);
      font-size: 12px;
      letter-spacing: 0.26em;
      text-transform: uppercase;
      color: var(--ink-soft);
      padding: 8px 14px;
      border-bottom: 1px solid transparent;
      transition: border-color 0.2s, color 0.2s;
    }
    nav a:hover, nav a.active { color: var(--ink); border-bottom-color: var(--ink); }
    .admin-body { padding: 40px 32px 80px; max-width: 1280px; margin: 0 auto; }
    @media (max-width: 700px) {
      .admin-top { flex-direction: column; align-items: flex-start; }
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
