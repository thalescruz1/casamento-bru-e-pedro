import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ContributionsAdminService } from '../../../core/services/contributions.service';
import {
  AdminContribution,
  ContributionStatus,
  ContributionStatusPaid,
  ContributionStatusPending,
  ContributionStatusRefused
} from '../../../core/models/contribution.models';

@Component({
  selector: 'app-admin-contributions',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './admin-contributions.component.html',
  styleUrl: './admin-contributions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminContributionsComponent implements OnInit {
  private readonly service = inject(ContributionsAdminService);

  readonly items = signal<AdminContribution[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly filter = signal<'all' | 'paid' | 'pending'>('all');

  readonly filtered = computed(() => {
    const f = this.filter();
    const all = this.items();
    if (f === 'all') { return all; }
    const target: ContributionStatus = f === 'paid' ? ContributionStatusPaid : ContributionStatusPending;
    return all.filter(i => i.status === target);
  });

  readonly stats = computed(() => {
    const all = this.items();
    const paid = all.filter(i => i.status === ContributionStatusPaid);
    const totalPaid = paid.reduce((acc, i) => acc + i.amount, 0);
    return {
      total: all.length,
      paid: paid.length,
      pending: all.filter(i => i.status === ContributionStatusPending).length,
      totalPaid
    };
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.listAll().subscribe({
      next: data => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: (err: { message: string }) => {
        this.error.set(err?.message ?? 'Erro ao carregar contribuições.');
        this.loading.set(false);
      }
    });
  }

  statusLabel(status: ContributionStatus): string {
    switch (status) {
      case ContributionStatusPaid: return 'Pago';
      case ContributionStatusPending: return 'Pendente';
      case ContributionStatusRefused: return 'Recusado';
      default: return '—';
    }
  }

  statusClass(status: ContributionStatus): string {
    switch (status) {
      case ContributionStatusPaid: return 'yes';
      case ContributionStatusPending: return 'pending';
      case ContributionStatusRefused: return 'no';
      default: return '';
    }
  }
}
