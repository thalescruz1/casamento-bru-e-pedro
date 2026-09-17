import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-hospedagem',
  imports: [RevealDirective],
  templateUrl: './hospedagem.component.html',
  styleUrl: './hospedagem.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HospedagemComponent {}
