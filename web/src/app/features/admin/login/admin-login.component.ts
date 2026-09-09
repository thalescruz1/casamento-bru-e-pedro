import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-admin-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './admin-login.component.html',
  styleUrl: './admin-login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminLoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  onSubmit(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email.trim(), password).subscribe({
      next: () => {
        this.submitting.set(false);
        const target = this.route.snapshot.queryParamMap.get('redirect') ?? '/admin';
        this.router.navigateByUrl(target);
      },
      error: (err: { status?: number; message?: string }) => {
        this.submitting.set(false);
        if (err?.status === 429) {
          this.error.set('Muitas tentativas. Tente de novo em alguns minutos.');
        } else if (err?.status === 400) {
          this.error.set('E-mail ou senha inválidos.');
        } else {
          this.error.set(err?.message ?? 'Não foi possível entrar.');
        }
      }
    });
  }
}
