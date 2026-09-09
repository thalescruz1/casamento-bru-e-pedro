import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RsvpService } from '../../../core/services/rsvp.service';
import { AttendanceSim, AttendanceNao } from '../../../core/models/rsvp.models';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

type FieldKey = 'name' | 'email' | 'phone' | 'attend' | 'guests' | 'guestNames';

@Component({
  selector: 'app-rsvp-form',
  imports: [ReactiveFormsModule, RevealDirective],
  templateUrl: './rsvp-form.component.html',
  styleUrl: './rsvp-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RsvpFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly rsvpService = inject(RsvpService);

  readonly AttendSim = AttendanceSim;
  readonly AttendNao = AttendanceNao;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    phone: ['', [Validators.required, Validators.pattern(/^\D*(\d\D*){10,13}$/)]],
    attend: [null as unknown as number, [Validators.required]],
    guests: [null as unknown as number, [Validators.required]],
    guestNames: ['', [Validators.maxLength(240)]],
    restrictions: ['', [Validators.maxLength(500)]]
  });

  private readonly guestsValue = signal<number | null>(null);
  readonly showGuestNames = computed(() => this.guestsValue() === 1);

  readonly submitting = signal(false);
  readonly success = signal(false);
  readonly attended = signal(false);
  readonly serverError = signal<string | null>(null);

  constructor() {
    this.form.controls.guests.valueChanges.subscribe(v => {
      const guestsValue = v ?? null;
      this.guestsValue.set(guestsValue);
      const ctrl = this.form.controls.guestNames;
      if (guestsValue === 1) {
        ctrl.addValidators([Validators.required, Validators.minLength(2)]);
      } else {
        ctrl.clearValidators();
        ctrl.setValue('', { emitEvent: false });
      }
      ctrl.updateValueAndValidity();
    });
  }

  hasError(field: FieldKey): boolean {
    const control = this.form.controls[field];
    return control.invalid && (control.dirty || control.touched);
  }

  onSubmit(): void {
    this.serverError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const value = this.form.getRawValue();
    this.rsvpService
      .submit({
        name: value.name.trim(),
        email: value.email.trim(),
        phone: value.phone.trim(),
        attend: value.attend === this.AttendSim ? this.AttendSim : this.AttendNao,
        guests: Number(value.guests) || 0,
        guestNames: value.guestNames?.trim() ? value.guestNames.trim() : null,
        restrictions: value.restrictions?.trim() ? value.restrictions.trim() : null
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.attended.set(value.attend === this.AttendSim);
          this.success.set(true);
          document.getElementById('rsvp')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        },
        error: (err: { message: string }) => {
          this.submitting.set(false);
          this.serverError.set(err?.message ?? 'Não foi possível enviar. Tente novamente em alguns instantes.');
        }
      });
  }
}
