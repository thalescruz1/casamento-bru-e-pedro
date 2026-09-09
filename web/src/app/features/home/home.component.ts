import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NavComponent } from './sections/nav.component';
import { HeroComponent } from './sections/hero.component';
import { CountdownComponent } from './sections/countdown.component';
import { EventsComponent } from './sections/events.component';
import { TrajeComponent } from './sections/traje.component';
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
    GiftsTeaserComponent,
    RsvpFormComponent,
    FooterComponent
  ],
  template: `
    <app-nav />
    <main>
      <app-hero />
      <app-countdown />
      <app-events />
      <app-traje />
      <app-gifts-teaser />
      <app-rsvp-form />
    </main>
    <app-footer />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent {}
