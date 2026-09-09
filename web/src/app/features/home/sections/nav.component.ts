import { ChangeDetectionStrategy, Component, HostListener, signal } from '@angular/core';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrl: './nav.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavComponent {
  readonly scrolled = signal(false);
  readonly mobileOpen = signal(false);
  readonly activeSection = signal<string>('');

  private readonly trackedSections = ['contagem', 'evento', 'traje', 'presentes', 'rsvp'];

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolled.set(window.scrollY > 40);
    for (const id of this.trackedSections) {
      const el = document.getElementById(id);
      if (!el) { continue; }
      const r = el.getBoundingClientRect();
      if (r.top <= 120 && r.bottom >= 120) {
        this.activeSection.set(id);
        return;
      }
    }
    this.activeSection.set('');
  }

  toggleMobile(): void {
    this.mobileOpen.update(v => !v);
  }

  goTo(event: Event, id: string, closeMobile = false): void {
    event.preventDefault();
    const target = id === 'top' ? document.body : document.getElementById(id);
    if (!target) { return; }
    const y = id === 'top' ? 0 : target.getBoundingClientRect().top + window.scrollY - 60;
    window.scrollTo({ top: y, behavior: 'smooth' });
    if (closeMobile) { this.mobileOpen.set(false); }
  }
}
