import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReceivedItem } from '../models/received.models';

@Injectable({ providedIn: 'root' })
export class ReceivedAdminService {
  private readonly http = inject(HttpClient);

  listAll(): Observable<ReceivedItem[]> {
    return this.http.get<ReceivedItem[]>('/api/received/admin');
  }
}
