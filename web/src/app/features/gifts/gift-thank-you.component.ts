import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-gift-thank-you',
  imports: [RouterLink],
  template: `
    <section class="thank">
      <img src="assets/bp/images/monogram.png" alt="Monograma Bruna e Pedro" class="thank__mono" />
      <div class="eyebrow">Recebido</div>
      <h1>Obrigado pelo<br/>carinho.</h1>
      <p>
        Sua contribuição será confirmada em instantes. Se pagou por Pix, a confirmação é imediata;
        no cartão, pode levar alguns minutos — a gente cuida do resto.
      </p>
      <a class="btn btn--solid" routerLink="/presentes">Voltar à lista</a>
    </section>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; }
    .thank {
      max-width: 620px;
      margin: 0 auto;
      padding: 140px 32px 120px;
      text-align: center;
    }
    .thank__mono { width: 90px; margin: 0 auto 32px; opacity: 0.9; }
    h1 {
      font-family: var(--serif);
      font-weight: 300;
      font-size: clamp(40px, 5vw, 64px);
      letter-spacing: 0.12em;
      text-transform: uppercase;
      margin: 18px 0 22px;
    }
    p {
      font-family: var(--serif-body);
      font-size: 17px;
      line-height: 1.75;
      color: var(--ink-soft);
      margin-bottom: 32px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GiftThankYouComponent {}
