import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-invite',
  imports: [RevealDirective],
  templateUrl: './invite.component.html',
  styleUrl: './invite.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InviteComponent {}
