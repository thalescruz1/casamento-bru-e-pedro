import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
    title: 'Heloisa & Thales — 08.08.26'
  },
  {
    path: 'presentes',
    loadComponent: () => import('./features/gifts/gifts-page.component').then(m => m.GiftsPageComponent),
    title: 'Presentes — Heloisa & Thales'
  },
  {
    path: 'presentes/obrigado',
    loadComponent: () => import('./features/gifts/gift-thank-you.component').then(m => m.GiftThankYouComponent),
    title: 'Obrigado — Heloisa & Thales'
  },
  {
    path: 'admin/login',
    loadComponent: () => import('./features/admin/login/admin-login.component').then(m => m.AdminLoginComponent),
    title: 'Entrar — Painel'
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', redirectTo: 'received', pathMatch: 'full' },
      {
        path: 'received',
        loadComponent: () => import('./features/admin/received/admin-received.component').then(m => m.AdminReceivedComponent)
      },
      {
        path: 'rsvps',
        loadComponent: () => import('./features/admin/rsvps/admin-rsvps.component').then(m => m.AdminRsvpsComponent)
      },
      {
        path: 'gifts',
        loadComponent: () => import('./features/admin/gifts/admin-gifts.component').then(m => m.AdminGiftsComponent)
      },
      {
        path: 'contributions',
        loadComponent: () => import('./features/admin/contributions/admin-contributions.component').then(m => m.AdminContributionsComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
