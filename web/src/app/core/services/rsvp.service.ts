import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RsvpConfirmation, RsvpList, SubmitRsvpPayload } from '../models/rsvp.models';

@Injectable({ providedIn: 'root' })
export class RsvpService {
  private readonly http = inject(HttpClient);

  submit(payload: SubmitRsvpPayload): Observable<RsvpConfirmation> {
    return this.http.post<RsvpConfirmation>('/api/rsvp', payload);
  }

  list(skip = 0, take = 50): Observable<RsvpList> {
    return this.http.get<RsvpList>(`/api/rsvp?skip=${skip}&take=${take}`);
  }

  exportCsvUrl(): string {
    return '/api/rsvp/export';
  }
}
