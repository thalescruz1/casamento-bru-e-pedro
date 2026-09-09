import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-traje',
  imports: [RevealDirective],
  templateUrl: './traje.component.html',
  styleUrl: './traje.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrajeComponent {}
