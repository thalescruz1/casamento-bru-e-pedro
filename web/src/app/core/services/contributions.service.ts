import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AdminContribution,
  CheckoutContributionCardPayload,
  CheckoutContributionCardResult,
  CheckoutContributionPixResult,
  ConfirmContributionPixPayload,
  ConfirmContributionResult
} from '../models/contribution.models';

@Injectable({ providedIn: 'root' })
export class ContributionsService {
  private readonly http = inject(HttpClient);

  getPixInfo(amount: number): Observable<CheckoutContributionPixResult> {
    const params = new HttpParams().set('amount', amount.toFixed(2));
    return this.http.get<CheckoutContributionPixResult>('/api/contributions/checkout/pix', { params });
  }

  confirmManualPix(payload: ConfirmContributionPixPayload): Observable<ConfirmContributionResult> {
    return this.http.post<ConfirmContributionResult>('/api/contributions/confirm-pix', payload);
  }

  checkoutCard(payload: CheckoutContributionCardPayload): Observable<CheckoutContributionCardResult> {
    return this.http.post<CheckoutContributionCardResult>('/api/contributions/checkout/card', payload);
  }
}

@Injectable({ providedIn: 'root' })
export class ContributionsAdminService {
  private readonly http = inject(HttpClient);

  listAll(): Observable<AdminContribution[]> {
    return this.http.get<AdminContribution[]>('/api/contributions/admin');
  }
}
