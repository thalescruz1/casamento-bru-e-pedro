import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GiftsService } from '../../core/services/gifts.service';
import { CheckoutPixResult, Gift, GiftStatusAvailable } from '../../core/models/gift.models';
import { MaskDirective } from '../../shared/directives/mask.directive';
import { cpfCnpjValidator, digitsLengthValidator, expiryValidator } from '../../shared/validators/payment-validators';
import { ContributionModalComponent } from '../contributions/contribution-modal.component';

type Step = 'info' | 'method' | 'pix' | 'card' | 'success-card' | 'success-pix' | 'pending-card';
type BuyerField = 'name' | 'email' | 'message';
type CardField =
  | 'document' | 'phone' | 'postalCode' | 'addressNumber'
  | 'holderName' | 'number' | 'expiry' | 'ccv';

@Component({
  selector: 'app-gifts-page',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, MaskDirective, ContributionModalComponent],
  templateUrl: './gifts-page.component.html',
  styleUrl: './gifts-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GiftsPageComponent implements OnInit {
  private readonly giftsService = inject(GiftsService);
  private readonly fb = inject(FormBuilder);

  readonly gifts = signal<Gift[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly sortMode = signal<'default' | 'asc' | 'desc'>('asc');
  readonly priceRange = signal<'all' | 'lt-150' | '150-500' | 'gte-500'>('all');

  readonly visibleGifts = computed<Gift[]>(() => {
    const items = [...this.gifts()];
    const range = this.priceRange();
    const sort = this.sortMode();

    let filtered = items;
    if (range === 'lt-150') {
      filtered = items.filter(g => g.price < 150);
    } else if (range === '150-500') {
      filtered = items.filter(g => g.price >= 150 && g.price <= 500);
    } else if (range === 'gte-500') {
      filtered = items.filter(g => g.price > 500);
    }

    if (sort === 'asc') {
      filtered = [...filtered].sort((a, b) => a.price - b.price);
    } else if (sort === 'desc') {
      filtered = [...filtered].sort((a, b) => b.price - a.price);
    }

    return filtered;
  });

  readonly selected = signal<Gift | null>(null);
  readonly showContributionModal = signal(false);
  readonly step = signal<Step>('info');
  readonly processing = signal(false);
  readonly processingError = signal<string | null>(null);

  readonly pixInfo = signal<CheckoutPixResult | null>(null);
  readonly pixCopied = signal(false);

  readonly AvailableStatus = GiftStatusAvailable;

  readonly buyerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    message: ['', [Validators.maxLength(600)]]
  });

  readonly cardForm = this.fb.nonNullable.group({
    document: ['', [Validators.required, cpfCnpjValidator]],
    phone: ['', [Validators.required, digitsLengthValidator(10, 11)]],
    postalCode: ['', [Validators.required, digitsLengthValidator(8, 8)]],
    addressNumber: ['', [Validators.required, Validators.maxLength(10)]],
    holderName: ['', [Validators.required, Validators.minLength(2)]],
    number: ['', [Validators.required, digitsLengthValidator(13, 19)]],
    expiry: ['', [Validators.required, expiryValidator]],
    ccv: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.giftsService.listAvailable().subscribe({
      next: data => {
        this.gifts.set(data);
        this.loading.set(false);
      },
      error: (err: { message: string }) => {
        this.error.set(err?.message ?? 'Não foi possível carregar os presentes.');
        this.loading.set(false);
      }
    });
  }

  hasBuyerError(field: BuyerField): boolean {
    const ctrl = this.buyerForm.controls[field];
    return ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  hasCardError(field: CardField): boolean {
    const ctrl = this.cardForm.controls[field];
    return ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  openModal(gift: Gift): void {
    if (gift.status !== this.AvailableStatus) return;
    this.selected.set(gift);
    this.step.set('info');
    this.processingError.set(null);
    this.pixInfo.set(null);
    this.pixCopied.set(false);
    this.buyerForm.reset({ name: '', email: '', message: '' });
    this.cardForm.reset({
      document: '', phone: '', postalCode: '', addressNumber: '',
      holderName: '', number: '', expiry: '', ccv: ''
    });
  }

  closeModal(): void {
    if (this.processing()) return;
    const wasSuccess = this.step() === 'success-card' || this.step() === 'success-pix';
    this.selected.set(null);
    if (wasSuccess) {
      // Recarrega a lista pra refletir o item já indisponível
      this.load();
    }
  }

  goToMethod(): void {
    if (this.buyerForm.invalid) {
      this.buyerForm.markAllAsTouched();
      return;
    }
    this.step.set('method');
    this.processingError.set(null);
  }

  pickPix(): void {
    const gift = this.selected();
    if (!gift) return;

    this.processing.set(true);
    this.processingError.set(null);

    this.giftsService.getPixInfo(gift.id).subscribe({
      next: result => {
        this.pixInfo.set(result);
        this.step.set('pix');
        this.processing.set(false);
      },
      error: (err: { message: string }) => {
        this.processing.set(false);
        this.processingError.set(err?.message ?? 'Não foi possível carregar a chave Pix.');
      }
    });
  }

  pickCard(): void {
    this.step.set('card');
    this.processingError.set(null);
  }

  copyPix(): void {
    const key = this.pixInfo()?.pixKey;
    if (!key) return;
    navigator.clipboard.writeText(key).then(() => {
      this.pixCopied.set(true);
      setTimeout(() => this.pixCopied.set(false), 3000);
    });
  }

  copyPixPayload(): void {
    const payload = this.pixInfo()?.qrCodePayload;
    if (!payload) return;
    navigator.clipboard.writeText(payload).then(() => {
      this.pixCopied.set(true);
      setTimeout(() => this.pixCopied.set(false), 3000);
    });
  }

  pixQrSrc(): string {
    const b64 = this.pixInfo()?.qrCodeImageBase64 ?? '';
    return `data:image/png;base64,${b64}`;
  }

  confirmPixPayment(): void {
    const gift = this.selected();
    if (!gift) return;

    this.processing.set(true);
    this.processingError.set(null);

    const buyer = this.buyerForm.getRawValue();
    const message = buyer.message?.trim() || null;

    this.giftsService.confirmManualPix(gift.id, {
      name: buyer.name.trim(),
      email: buyer.email.trim(),
      message
    }).subscribe({
      next: () => {
        this.processing.set(false);
        this.step.set('success-pix');
      },
      error: (err: { message: string }) => {
        this.processing.set(false);
        this.processingError.set(err?.message ?? 'Não foi possível registrar o pagamento.');
      }
    });
  }

  submitCard(): void {
    const gift = this.selected();
    if (!gift) return;

    if (this.cardForm.invalid) {
      this.cardForm.markAllAsTouched();
      return;
    }

    this.processing.set(true);
    this.processingError.set(null);

    const buyer = this.buyerForm.getRawValue();
    const card = this.cardForm.getRawValue();
    const message = buyer.message?.trim() || null;

    const expiryParts = /^(\d{2})\s*\/\s*(\d{2}|\d{4})$/.exec(card.expiry.trim())!;
    const month = expiryParts[1];
    let year = expiryParts[2];
    if (year.length === 2) {
      year = `20${year}`;
    }

    this.giftsService.checkoutCard(gift.id, {
      name: buyer.name.trim(),
      email: buyer.email.trim(),
      document: card.document.trim(),
      phone: card.phone.trim(),
      postalCode: card.postalCode.trim(),
      addressNumber: card.addressNumber.trim(),
      message,
      card: {
        holderName: card.holderName.trim(),
        number: card.number.replace(/\D/g, ''),
        expiryMonth: month,
        expiryYear: year,
        ccv: card.ccv.trim()
      }
    }).subscribe({
      next: result => {
        this.processing.set(false);
        if (result.status === 'Refused') {
          this.processingError.set('O pagamento foi recusado. Verifique os dados do cartão e tente novamente.');
          return;
        }
        if (result.status === 'Pending') {
          this.step.set('pending-card');
          return;
        }
        this.step.set('success-card');
      },
      error: (err: { message: string }) => {
        this.processing.set(false);
        this.processingError.set(err?.message ?? 'Não foi possível processar o cartão.');
      }
    });
  }

  openContributionModal(): void {
    this.showContributionModal.set(true);
  }

  closeContributionModal(): void {
    this.showContributionModal.set(false);
  }

  pixKeyTypeLabel(type: string | undefined): string {
    switch ((type ?? '').toLowerCase()) {
      case 'email': return 'E-mail';
      case 'cpf': return 'CPF';
      case 'cnpj': return 'CNPJ';
      case 'phone': return 'Telefone';
      case 'random': return 'Chave aleatória';
      default: return 'Chave';
    }
  }
}
