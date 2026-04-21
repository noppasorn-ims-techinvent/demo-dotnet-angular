import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { OrderDto } from '../models/api.types';
import { Observable } from 'rxjs';

export interface PlaceOrderLineRequest {
  productId: number;
  quantity: number;
}

export interface PlaceOrderRequest {
  lines: PlaceOrderLineRequest[];
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  placeOrder(body: PlaceOrderRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>('/api/Orders', body);
  }

  getMine(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>('/api/Orders/mine');
  }

  getAll(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>('/api/Orders');
  }

  updateStatus(orderId: number, status: number): Observable<OrderDto> {
    return this.http.patch<OrderDto>(`/api/Orders/${orderId}/status`, { status });
  }

  delete(orderId: number): Observable<void> {
    return this.http.delete<void>(`/api/Orders/${orderId}`);
  }
}
