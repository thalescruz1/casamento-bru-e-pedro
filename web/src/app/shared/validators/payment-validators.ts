import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Valida que o input é um CPF (11 dígitos) ou CNPJ (14 dígitos).
 * Não faz checksum — apenas contagem.
 */
export function cpfCnpjValidator(control: AbstractControl): ValidationErrors | null {
  const raw = (control.value ?? '') as string;
  if (!raw) return null;
  const digits = raw.replace(/\D/g, '').length;
  return digits === 11 || digits === 14 ? null : { invalidDocument: true };
}

/**
 * Valida que a quantidade de dígitos extraídos está entre `min` e `max`.
 */
export function digitsLengthValidator(min: number, max: number) {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = (control.value ?? '') as string;
    if (!raw) return null;
    const len = raw.replace(/\D/g, '').length;
    return len >= min && len <= max ? null : { invalidLength: true };
  };
}

/**
 * Valida formato `MM/AA` ou `MM/AAAA` (com mês entre 01 e 12).
 */
export function expiryValidator(control: AbstractControl): ValidationErrors | null {
  const raw = ((control.value ?? '') as string).trim();
  if (!raw) return null;
  const match = /^(\d{2})\s*\/\s*(\d{2}|\d{4})$/.exec(raw);
  if (!match) return { invalidExpiry: true };
  const month = Number(match[1]);
  if (month < 1 || month > 12) return { invalidExpiry: true };
  return null;
}
