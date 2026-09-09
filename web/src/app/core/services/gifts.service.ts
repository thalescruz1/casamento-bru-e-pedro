import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AdminGift,
  CheckoutCardPayload,
  CheckoutCardResult,
  CheckoutPixResult,
  ConfirmPixPayload,
  CreateOrUpdateGiftPayload,
  Gift,
  UploadedImage
} from '../models/gift.models';

@Injectable({ providedIn: 'root' })
export class GiftsService {
  private readonly http = inject(HttpClient);

  listAvailable(): Observable<Gift[]> {
    return this.http.get<Gift[]>('/api/gifts');
  }

  getPixInfo(giftId: string): Observable<CheckoutPixResult> {
    return this.http.get<CheckoutPixResult>(`/api/gifts/${giftId}/checkout/pix`);
  }

  confirmManualPix(giftId: string, payload: ConfirmPixPayload): Observable<CheckoutCardResult> {
    return this.http.post<CheckoutCardResult>(`/api/gifts/${giftId}/confirm-pix`, payload);
  }

  checkoutCard(giftId: string, payload: CheckoutCardPayload): Observable<CheckoutCardResult> {
    return this.http.post<CheckoutCardResult>(`/api/gifts/${giftId}/checkout/card`, payload);
  }
}

@Injectable({ providedIn: 'root' })
export class GiftsAdminService {
  private readonly http = inject(HttpClient);

  listAll(): Observable<AdminGift[]> {
    return this.http.get<AdminGift[]>('/api/gifts/admin');
  }

  create(payload: CreateOrUpdateGiftPayload): Observable<AdminGift> {
    return this.http.post<AdminGift>('/api/gifts', payload);
  }

  update(id: string, payload: CreateOrUpdateGiftPayload): Observable<AdminGift> {
    return this.http.put<AdminGift>(`/api/gifts/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/gifts/${id}`);
  }

  uploadImage(file: File): Observable<UploadedImage> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<UploadedImage>('/api/gifts/upload-image', form);
  }
}
