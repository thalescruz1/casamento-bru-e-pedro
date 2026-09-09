import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-gifts-teaser',
  imports: [RouterLink, RevealDirective],
  templateUrl: './gifts-teaser.component.html',
  styleUrl: './gifts-teaser.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GiftsTeaserComponent {}
