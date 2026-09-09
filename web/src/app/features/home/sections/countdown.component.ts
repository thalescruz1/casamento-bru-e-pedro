import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, signal } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-countdown',
  imports: [RevealDirective],
  templateUrl: './countdown.component.html',
  styleUrl: './countdown.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CountdownComponent implements OnInit, OnDestroy {
  readonly days = signal('—');
  readonly hours = signal('—');
  readonly minutes = signal('—');
  readonly seconds = signal('—');

  private readonly target = new Date('2026-08-08T15:00:00-03:00').getTime();
  private intervalId?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.tick();
    this.intervalId = setInterval(() => this.tick(), 1000);
  }

  ngOnDestroy(): void {
    if (this.intervalId) { clearInterval(this.intervalId); }
  }

  private tick(): void {
    const diff = this.target - Date.now();
    if (diff <= 0) {
      this.days.set('00'); this.hours.set('00'); this.minutes.set('00'); this.seconds.set('00');
      return;
    }
    const pad = (n: number) => String(Math.max(0, n)).padStart(2, '0');
    this.days.set(pad(Math.floor(diff / 86_400_000)));
    this.hours.set(pad(Math.floor(diff / 3_600_000) % 24));
    this.minutes.set(pad(Math.floor(diff / 60_000) % 60));
    this.seconds.set(pad(Math.floor(diff / 1000) % 60));
  }
}
