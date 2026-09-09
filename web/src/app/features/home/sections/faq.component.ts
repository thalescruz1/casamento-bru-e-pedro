import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

interface FaqItem {
  q: string;
  a: string;
}

@Component({
  selector: 'app-faq',
  imports: [RevealDirective],
  templateUrl: './faq.component.html',
  styleUrl: './faq.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FaqComponent {
  readonly openIndex = signal<number | null>(null);

  readonly items: readonly FaqItem[] = [
    {
      q: 'Qual é o traje?',
      a:
        'Traje social.<br/><br/>' +
        '<strong>Para elas:</strong> as madrinhas usarão preto. Para as convidadas, sugerimos vestidos longos, sapatos com salto bloco ou sem salto, devido ao piso irregular do local da cerimônia.<br/><br/>' +
        '<strong>Para eles:</strong> camisa e calça sociais, acompanhadas de terno e gravata.'
    },
    {
      q: 'Qual é o clima esperado para o dia?',
      a: 'Em agosto, São Roque tem clima típico de inverno. Durante o dia, as temperaturas costumam ser amenas, e ao entardecer e à noite o frio aumenta. Recomendamos planejar seu traje considerando essa mudança de temperatura para garantir conforto em todos os momentos da festa.'
    },
    {
      q: 'Vai ter transporte saindo de São Paulo?',
      a: 'Não. Ofereceremos um transfer exclusivamente dos dois hotéis indicados até o local da cerimônia. Caso tenha interesse, sinalize essa opção no momento da confirmação de presença.'
    },
    {
      q: 'Qual a data limite para confirmar?',
      a: 'Pedimos que todas as confirmações sejam feitas até 30 de junho de 2026, pelo formulário deste site.'
    },
    {
      q: 'Tem estacionamento no local?',
      a: 'Sim. O Château du Plas oferece estacionamento gratuito, com serviço de manobrista na chegada.'
    },
    {
      q: 'Vai chover?',
      a: 'Esperamos que não! Mas, em caso de chuva, toda a cerimônia e festa acontecem em ambientes cobertos e aquecidos do château, sem alteração de horário.'
    }
  ];

  toggle(index: number): void {
    this.openIndex.set(this.openIndex() === index ? null : index);
  }
}
