import { Directive, ElementRef, HostListener, Input, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

export type MaskPattern = 'cpfCnpj' | 'cep' | 'phone' | 'expiry' | 'cardNumber';

@Directive({
  selector: '[appMask]',
  standalone: true
})
export class MaskDirective {
  @Input({ required: true, alias: 'appMask' }) pattern!: MaskPattern;

  private readonly el = inject<ElementRef<HTMLInputElement>>(ElementRef);
  private readonly control = inject(NgControl, { optional: true });

  @HostListener('input')
  onInput(): void {
    const raw = this.el.nativeElement.value;
    const masked = this.applyMask(raw);
    if (masked === raw) {
      return;
    }
    this.el.nativeElement.value = masked;
    this.control?.control?.setValue(masked, { emitModelToViewChange: false, emitEvent: true });
  }

  private applyMask(value: string): string {
    const digits = value.replace(/\D/g, '');
    switch (this.pattern) {
      case 'cpfCnpj': return this.cpfCnpj(digits.slice(0, 14));
      case 'cep': return this.cep(digits.slice(0, 8));
      case 'phone': return this.phone(digits.slice(0, 11));
      case 'expiry': return this.expiry(digits.slice(0, 4));
      case 'cardNumber': return this.cardNumber(digits.slice(0, 19));
    }
  }

  private cpfCnpj(d: string): string {
    if (d.length === 0) { return ''; }
    if (d.length <= 11) {
      // CPF — 000.000.000-00
      let r = d.slice(0, 3);
      if (d.length > 3) { r += '.' + d.slice(3, 6); }
      if (d.length > 6) { r += '.' + d.slice(6, 9); }
      if (d.length > 9) { r += '-' + d.slice(9, 11); }
      return r;
    }
    // CNPJ — 00.000.000/0000-00
    let r = d.slice(0, 2) + '.' + d.slice(2, 5);
    if (d.length > 5) { r += '.' + d.slice(5, 8); }
    if (d.length > 8) { r += '/' + d.slice(8, 12); }
    if (d.length > 12) { r += '-' + d.slice(12, 14); }
    return r;
  }

  private cep(d: string): string {
    if (d.length <= 5) { return d; }
    return d.slice(0, 5) + '-' + d.slice(5, 8);
  }

  private phone(d: string): string {
    if (d.length === 0) { return ''; }
    if (d.length <= 2) { return '(' + d; }
    let r = '(' + d.slice(0, 2) + ') ';
    if (d.length <= 10) {
      // Fixo (11) 1234-5678
      r += d.slice(2, 6);
      if (d.length > 6) { r += '-' + d.slice(6, 10); }
    } else {
      // Celular (11) 91234-5678
      r += d.slice(2, 7);
      if (d.length > 7) { r += '-' + d.slice(7, 11); }
    }
    return r;
  }

  private expiry(d: string): string {
    if (d.length <= 2) { return d; }
    return d.slice(0, 2) + '/' + d.slice(2, 4);
  }

  private cardNumber(d: string): string {
    return d.match(/.{1,4}/g)?.join(' ') ?? '';
  }
}
