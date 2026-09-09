import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RsvpService } from '../../../core/services/rsvp.service';
import { RsvpListItem, AttendanceSim, AttendanceNao, Attendance } from '../../../core/models/rsvp.models';

@Component({
  selector: 'app-admin-rsvps',
  imports: [DatePipe],
  templateUrl: './admin-rsvps.component.html',
  styleUrl: './admin-rsvps.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminRsvpsComponent implements OnInit {
  private readonly rsvpService = inject(RsvpService);

  readonly items = signal<RsvpListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly filter = signal<'all' | 'sim' | 'nao'>('all');

  readonly exportUrl = this.rsvpService.exportCsvUrl();

  readonly filtered = computed(() => {
    const f = this.filter();
    const all = this.items();
    if (f === 'all') { return all; }
    const target: Attendance = f === 'sim' ? AttendanceSim : AttendanceNao;
    return all.filter(i => i.attend === target);
  });

  readonly stats = computed(() => {
    const all = this.items();
    const sim = all.filter(i => i.attend === AttendanceSim);
    const attendees = sim.reduce((acc, i) => acc + 1 + i.guests, 0);
    return {
      total: all.length,
      sim: sim.length,
      nao: all.length - sim.length,
      attendees
    };
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.rsvpService.list(0, 500).subscribe({
      next: data => {
        this.items.set(data.items);
        this.total.set(data.total);
        this.loading.set(false);
      },
      error: (err: { message: string }) => {
        this.error.set(err?.message ?? 'Erro ao carregar confirmações.');
        this.loading.set(false);
      }
    });
  }

  attendLabel(attend: Attendance): string {
    return attend === AttendanceSim ? 'Sim' : 'Não';
  }
}
