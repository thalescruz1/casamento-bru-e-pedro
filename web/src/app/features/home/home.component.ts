import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NavComponent } from './sections/nav.component';
import { HeroComponent } from './sections/hero.component';
import { CountdownComponent } from './sections/countdown.component';
import { InviteComponent } from './sections/invite.component';
import { EventsComponent } from './sections/events.component';
import { VenueComponent } from './sections/venue.component';
import { RsvpFormComponent } from './sections/rsvp-form.component';
import { GiftsTeaserComponent } from './sections/gifts-teaser.component';
import { StayComponent } from './sections/stay.component';
import { FaqComponent } from './sections/faq.component';
import { FooterComponent } from './sections/footer.component';

@Component({
  selector: 'app-home',
  imports: [
    NavComponent,
    HeroComponent,
    CountdownComponent,
    InviteComponent,
    EventsComponent,
    VenueComponent,
    RsvpFormComponent,
    GiftsTeaserComponent,
    StayComponent,
    FaqComponent,
    FooterComponent
  ],
  template: `
    <app-nav />
    <main id="top">
      <app-hero />
      <app-countdown />
      <app-invite />
      <app-events />
      <app-venue />
      <app-rsvp-form />
      <app-gifts-teaser />
      <app-stay />
      <app-faq />
    </main>
    <app-footer />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent {}
