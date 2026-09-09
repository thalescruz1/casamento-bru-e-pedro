import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RsvpService } from '../../../core/services/rsvp.service';
import { AttendanceSim, AttendanceNao } from '../../../core/models/rsvp.models';
import { RevealDirective } from '../../../shared/directives/reveal.directive';
import { MaskDirective } from '../../../shared/directives/mask.directive';

type FieldKey = 'name' | 'email' | 'phone' | 'attend' | 'guests';

@Component({
  selector: 'app-rsvp-form',
  imports: [ReactiveFormsModule, RevealDirective, MaskDirective],
  templateUrl: './rsvp-form.component.html',
  styleUrl: './rsvp-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RsvpFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly rsvpService = inject(RsvpService);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    phone: ['', [Validators.pattern(/^\D*(\d\D*){10,13}$/)]],
    attend: [null as unknown as number, [Validators.required]],
    guests: [0, [Validators.required, Validators.min(0), Validators.max(10)]],
    guestNames: ['', [Validators.maxLength(240)]],
    restrictions: ['', [Validators.maxLength(500)]]
  });

  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly serverError = signal<string | null>(null);

  hasError(field: FieldKey): boolean {
    const control = this.form.controls[field];
    return control.invalid && (control.dirty || control.touched);
  }

  submit(): void {
    this.serverError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const value = this.form.getRawValue();
    const attend = Number(value.attend) === AttendanceSim ? AttendanceSim : AttendanceNao;
    this.rsvpService
      .submit({
        name: value.name.trim(),
        email: value.email.trim(),
        phone: value.phone.trim(),
        attend,
        guests: Number(value.guests) || 0,
        guestNames: value.guestNames?.trim() ? value.guestNames.trim() : null,
        restrictions: value.restrictions?.trim() ? value.restrictions.trim() : null
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.submitted.set(true);
          document.getElementById('rsvp')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        },
        error: (err: { message: string }) => {
          this.submitting.set(false);
          this.serverError.set(err?.message ?? 'Não foi possível enviar. Tente novamente em alguns instantes.');
        }
      });
  }
}
