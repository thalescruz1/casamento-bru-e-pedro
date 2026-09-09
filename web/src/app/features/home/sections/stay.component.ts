import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-stay',
  imports: [RevealDirective],
  templateUrl: './stay.component.html',
  styleUrl: './stay.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StayComponent {}
