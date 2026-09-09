import { ChangeDetectionStrategy, Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ContributionsService } from '../../core/services/contributions.service';
import { CheckoutContributionPixResult } from '../../core/models/contribution.models';
import { MaskDirective } from '../../shared/directives/mask.directive';
import { cpfCnpjValidator, digitsLengthValidator, expiryValidator } from '../../shared/validators/payment-validators';

type Step = 'info' | 'method' | 'pix' | 'card' | 'success-card' | 'success-pix' | 'pending-card';
type BuyerField = 'amount' | 'name' | 'email' | 'message';
type CardField =
  | 'document' | 'phone' | 'postalCode' | 'addressNumber'
  | 'holderName' | 'number' | 'expiry' | 'ccv';

const MIN_AMOUNT = 5;

@Component({
  selector: 'app-contribution-modal',
  imports: [ReactiveFormsModule, CurrencyPipe, MaskDirective],
  templateUrl: './contribution-modal.component.html',
  styleUrl: './contribution-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ContributionModalComponent {
  private readonly contributionsService = inject(ContributionsService);
  private readonly fb = inject(FormBuilder);

  @Output() readonly closed = new EventEmitter<void>();

  readonly step = signal<Step>('info');
  readonly processing = signal(false);
  readonly processingError = signal<string | null>(null);

  readonly pixInfo = signal<CheckoutContributionPixResult | null>(null);
  readonly pixCopied = signal(false);

  readonly buyerForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(MIN_AMOUNT)]],
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

  amountValue(): number {
    return Number(this.buyerForm.controls.amount.value) || 0;
  }

  hasBuyerError(field: BuyerField): boolean {
    const ctrl = this.buyerForm.controls[field];
    return ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  hasCardError(field: CardField): boolean {
    const ctrl = this.cardForm.controls[field];
    return ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  close(): void {
    if (this.processing()) return;
    this.closed.emit();
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
    const amount = this.amountValue();
    if (amount < MIN_AMOUNT) return;

    this.processing.set(true);
    this.processingError.set(null);

    this.contributionsService.getPixInfo(amount).subscribe({
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
    this.processing.set(true);
    this.processingError.set(null);

    const buyer = this.buyerForm.getRawValue();
    const message = buyer.message?.trim() || null;

    this.contributionsService.confirmManualPix({
      amount: Number(buyer.amount),
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

    this.contributionsService.checkoutCard({
      amount: Number(buyer.amount),
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
