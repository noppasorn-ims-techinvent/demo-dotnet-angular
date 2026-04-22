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
    return this.http.post<OrderDto>('/api/Orders/PlaceOrder', body);
  }

  getMine(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>('/api/Orders/GetMine');
  }

  getById(orderId: number): Observable<OrderDto> {
    return this.http.get<OrderDto>(`/api/Orders/GetById/${orderId}`);
  }

  simulatePayment(orderId: number, body: SimulatePaymentRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>(`/api/Orders/SimulatePayment/${orderId}/simulate-payment`, {
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
    return this.http.post<OrderDto>(`/api/Orders/RequestCancellation/${orderId}/request-cancellation`, { reason });
  }

  /** แอดมินต้องส่ง targetSellerUserId — ผู้ขายไม่ต้องส่ง (พิจารณาเฉพาะร้านตัวเอง) */
  reviewCancellation(
    orderId: number,
    approved: boolean,
    note: string,
    targetSellerUserId?: number,
  ): Observable<OrderDto> {
    const body: { approved: boolean; note: string; targetSellerUserId?: number } = { approved, note };
    const validTarget =
      typeof targetSellerUserId === 'number' &&
      Number.isFinite(targetSellerUserId) &&
      targetSellerUserId >= 1;
    if (validTarget) {
      body.targetSellerUserId = targetSellerUserId;
    }
    const q = validTarget ? `?targetSellerUserId=${encodeURIComponent(String(targetSellerUserId))}` : '';
    return this.http.post<OrderDto>(`/api/Orders/ReviewCancellation/${orderId}/review-cancellation${q}`, body);
  }

  /** คำสั่งซื้อที่มีสินค้าของร้านคุณ (บรรทัดและยอดรวมเฉพาะสินค้าของร้าน) */
  getForMyStore(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>('/api/Orders/GetForMyStore');
  }

  getAll(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>('/api/Orders/GetAll');
  }

  updateStatus(orderId: number, status: number): Observable<OrderDto> {
    return this.http.patch<OrderDto>(`/api/Orders/UpdateStatus/${orderId}/status`, { status });
  }

  delete(orderId: number): Observable<void> {
    return this.http.delete<void>(`/api/Orders/Delete/${orderId}`);
  }
}
