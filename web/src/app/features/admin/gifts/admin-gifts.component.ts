import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GiftsAdminService } from '../../../core/services/gifts.service';
import {
  AdminGift,
  GiftStatus,
  GiftStatusAvailable,
  GiftStatusPaid,
  GiftStatusCanceled
} from '../../../core/models/gift.models';
import { FilterByStatusPipe } from '../../../shared/pipes/filter-by-status.pipe';

@Component({
  selector: 'app-admin-gifts',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, FilterByStatusPipe],
  templateUrl: './admin-gifts.component.html',
  styleUrl: './admin-gifts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminGiftsComponent implements OnInit {
  private readonly service = inject(GiftsAdminService);
  private readonly fb = inject(FormBuilder);

  readonly gifts = signal<AdminGift[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly editing = signal<AdminGift | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly uploading = signal(false);
  readonly imagePreviewUrl = signal<string | null>(null);
  readonly imageBlobName = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
    description: ['', [Validators.required, Validators.maxLength(500)]],
    price: [0, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.listAll().subscribe({
      next: data => {
        this.gifts.set(data);
        this.loading.set(false);
      },
      error: (err: { message: string }) => {
        this.error.set(err?.message ?? 'Erro ao carregar presentes.');
        this.loading.set(false);
      }
    });
  }

  statusLabel(status: GiftStatus): string {
    switch (status) {
      case GiftStatusAvailable: return 'Disponível';
      case GiftStatusPaid: return 'Pago';
      case GiftStatusCanceled: return 'Cancelado';
      default: return 'Desconhecido';
    }
  }

  statusClass(status: GiftStatus): string {
    switch (status) {
      case GiftStatusAvailable: return 'st-available';
      case GiftStatusPaid: return 'st-paid';
      case GiftStatusCanceled: return 'st-canceled';
      default: return '';
    }
  }

  newGift(): void {
    this.editing.set(null);
    this.form.reset({ title: '', description: '', price: 0 });
    this.imagePreviewUrl.set(null);
    this.imageBlobName.set(null);
    this.formError.set(null);
    this.showForm.set(true);
  }

  edit(gift: AdminGift): void {
    this.editing.set(gift);
    this.form.reset({
      title: gift.title,
      description: gift.description,
      price: gift.price
    });
    this.imagePreviewUrl.set(gift.imageUrl);
    this.imageBlobName.set(gift.imageBlobName);
    this.formError.set(null);
    this.showForm.set(true);
  }

  closeForm(): void {
    if (this.saving() || this.uploading()) { return; }
    this.showForm.set(false);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    if (!file) { return; }

    this.uploading.set(true);
    this.formError.set(null);
    this.service.uploadImage(file).subscribe({
      next: result => {
        this.imageBlobName.set(result.blobName);
        this.imagePreviewUrl.set(result.url);
        this.uploading.set(false);
      },
      error: (err: { message: string }) => {
        this.uploading.set(false);
        this.formError.set(err?.message ?? 'Falha ao enviar imagem.');
      }
    });
    input.value = '';
  }

  removeImage(): void {
    this.imageBlobName.set(null);
    this.imagePreviewUrl.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const payload = {
      title: value.title.trim(),
      description: value.description.trim(),
      price: Number(value.price),
      imageBlobName: this.imageBlobName()
    };

    this.saving.set(true);
    this.formError.set(null);

    const current = this.editing();
    const call$ = current
      ? this.service.update(current.id, payload)
      : this.service.create(payload);

    call$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.load();
      },
      error: (err: { message: string }) => {
        this.saving.set(false);
        this.formError.set(err?.message ?? 'Não foi possível salvar.');
      }
    });
  }

  delete(gift: AdminGift): void {
    if (!window.confirm(`Remover "${gift.title}"? Esta ação só funciona para itens disponíveis.`)) { return; }
    this.service.delete(gift.id).subscribe({
      next: () => this.load(),
      error: (err: { message: string }) => this.error.set(err?.message ?? 'Erro ao remover.')
    });
  }
}
