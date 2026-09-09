import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval, startWith } from 'rxjs';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

const TARGET = new Date('2027-02-12T15:00:00-03:00').getTime();
const pad = (n: number) => n.toString().padStart(2, '0');

@Component({
  selector: 'app-countdown',
  imports: [RevealDirective],
  templateUrl: './countdown.component.html',
  styleUrl: './countdown.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CountdownComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  readonly tiles = signal({ days: '00', hours: '00', minutes: '00', seconds: '00' });

  ngOnInit(): void {
    interval(1000)
      .pipe(startWith(0), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.update());
  }

  private update(): void {
    const diff = TARGET - Date.now();
    if (diff <= 0) {
      this.tiles.set({ days: '00', hours: '00', minutes: '00', seconds: '00' });
      return;
    }
    const days = Math.floor(diff / 86_400_000);
    const hours = Math.floor(diff / 3_600_000) % 24;
    const minutes = Math.floor(diff / 60_000) % 60;
    const seconds = Math.floor(diff / 1_000) % 60;
    this.tiles.set({ days: pad(days), hours: pad(hours), minutes: pad(minutes), seconds: pad(seconds) });
  }
}
