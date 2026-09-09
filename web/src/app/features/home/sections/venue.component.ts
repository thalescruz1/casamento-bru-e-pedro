import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-venue',
  imports: [RevealDirective],
  templateUrl: './venue.component.html',
  styleUrl: './venue.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VenueComponent {
  static readonly MapsUrl = 'https://maps.app.goo.gl/hCD4rmo7K7P62ibBA';
  static readonly WazeUrl = 'https://waze.com/ul/h6gy3xg5um';

  readonly mapsUrl = VenueComponent.MapsUrl;
  readonly wazeUrl = VenueComponent.WazeUrl;

  openMap(): void {
    window.open(this.mapsUrl, '_blank', 'noopener,noreferrer');
  }

  openWaze(): void {
    window.open(this.wazeUrl, '_blank', 'noopener,noreferrer');
  }
}
