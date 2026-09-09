import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ReceivedAdminService } from '../../../core/services/received.service';
import { PaymentMethod, ReceivedItem, ReceivedType } from '../../../core/models/received.models';

type Filter = 'all' | 'gift' | 'contribution' | 'pix' | 'card';

@Component({
  selector: 'app-admin-received',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './admin-received.component.html',
  styleUrl: './admin-received.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminReceivedComponent implements OnInit {
  private readonly service = inject(ReceivedAdminService);

  readonly items = signal<ReceivedItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly filter = signal<Filter>('all');

  readonly filtered = computed(() => {
    const f = this.filter();
    const all = this.items();
    switch (f) {
      case 'all': return all;
      case 'gift':
      case 'contribution': return all.filter(i => i.type === f);
      case 'pix':
      case 'card': return all.filter(i => i.paymentMethod === f);
    }
  });

  readonly stats = computed(() => {
    const all = this.items();
    const gifts = all.filter(i => i.type === 'gift');
    const contributions = all.filter(i => i.type === 'contribution');
    const pix = all.filter(i => i.paymentMethod === 'pix');
    const total = all.reduce((acc, i) => acc + i.amount, 0);
    return {
      count: all.length,
      gifts: gifts.length,
      contributions: contributions.length,
      pix: pix.length,
      card: all.length - pix.length,
      total
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
        this.error.set(err?.message ?? 'Erro ao carregar itens recebidos.');
        this.loading.set(false);
      }
    });
  }

  typeLabel(type: ReceivedType): string {
    return type === 'gift' ? 'Presente' : 'Valor livre';
  }

  typeClass(type: ReceivedType): string {
    return type === 'gift' ? 'chip chip--gift' : 'chip chip--contrib';
  }

  methodLabel(method: PaymentMethod): string {
    return method === 'pix' ? 'Pix' : 'Cartão';
  }

  methodClass(method: PaymentMethod): string {
    return method === 'pix' ? 'chip chip--pix' : 'chip chip--card';
  }
}
