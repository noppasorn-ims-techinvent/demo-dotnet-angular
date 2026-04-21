import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { OrderDto } from '../models/api.types';
import { Observable } from 'rxjs';

export interface SimulatePaymentRequest {
  paymentMethod: 'card' | 'bank';
  cardNumber?: string;
  cardHolder?: string;
  cardExpiry?: string;
  cardCvv?: string;
  bankCode?: string;
  bankAccountNumber?: string;
}

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

  getById(orderId: number): Observable<OrderDto> {
    return this.http.get<OrderDto>(`/api/Orders/${orderId}`);
  }

  simulatePayment(orderId: number, body: SimulatePaymentRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>(`/api/Orders/${orderId}/simulate-payment`, {
      paymentMethod: body.paymentMethod,
      cardNumber: body.cardNumber,
      cardHolder: body.cardHolder,
      cardExpiry: body.cardExpiry,
      cardCvv: body.cardCvv,
      bankCode: body.bankCode,
      bankAccountNumber: body.bankAccountNumber,
    });
  }

  requestCancellation(orderId: number, reason: string): Observable<OrderDto> {
    return this.http.post<OrderDto>(`/api/Orders/${orderId}/request-cancellation`, { reason });
  }

  reviewCancellation(orderId: number, approved: boolean, note: string): Observable<OrderDto> {
    return this.http.post<OrderDto>(`/api/Orders/${orderId}/review-cancellation`, { approved, note });
  }

  /** คำสั่งซื้อที่มีสินค้าของร้านคุณ (บรรทัดและยอดรวมเฉพาะสินค้าของร้าน) */
  getForMyStore(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>('/api/Orders/for-store');
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
