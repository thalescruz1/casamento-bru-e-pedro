import { Pipe, PipeTransform } from '@angular/core';
import { AdminGift, GiftStatus } from '../../core/models/gift.models';

@Pipe({ name: 'filterByStatus' })
export class FilterByStatusPipe implements PipeTransform {
  transform(items: readonly AdminGift[] | null | undefined, status: GiftStatus): number {
    if (!items) { return 0; }
    return items.filter(i => i.status === status).length;
  }
}
