import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NavComponent } from './sections/nav.component';
import { HeroComponent } from './sections/hero.component';
import { CountdownComponent } from './sections/countdown.component';
import { EventsComponent } from './sections/events.component';
import { TrajeComponent } from './sections/traje.component';
import { HospedagemComponent } from './sections/hospedagem.component';
import { GiftsTeaserComponent } from './sections/gifts-teaser.component';
import { RsvpFormComponent } from './sections/rsvp-form.component';
import { FooterComponent } from './sections/footer.component';

@Component({
  selector: 'app-home',
  imports: [
    NavComponent,
    HeroComponent,
    CountdownComponent,
    EventsComponent,
    TrajeComponent,
    HospedagemComponent,
    GiftsTeaserComponent,
    RsvpFormComponent,
    FooterComponent
  ],
  template: `
    <app-nav />
    <main>
      <app-hero />
      <div class="lace" aria-hidden="true"></div>
      <app-countdown />
      <div class="lace" aria-hidden="true"></div>
      <app-events />
      <app-traje />
      <app-hospedagem />
      <div class="lace" aria-hidden="true"></div>
      <app-gifts-teaser />
      <app-rsvp-form />
    </main>
    <app-footer />
  `,
  styles: [`
    .lace {
      height: 26px;
      background: url('/assets/bp/svg/renda-azul.svg') repeat-x center / 240px 26px;
      margin: -1px 0;
      position: relative;
      z-index: 2;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent {}
