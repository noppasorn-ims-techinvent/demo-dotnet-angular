import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, throwError } from 'rxjs';
import type { OrderDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { orderStatusLabelTh } from '../../core/utils/order-status';
import { OrderService, type SimulatePaymentRequest } from '../../core/services/order.service';

@Component({
  selector: 'app-order-pay',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ThaiBahtPipe],
  templateUrl: './order-pay.component.html',
  styleUrl: './order-pay.component.css',
})
export class OrderPayComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orders = inject(OrderService);

  protected readonly order = signal<OrderDto | null>(null);
  protected readonly error = signal('');
  protected readonly loadError = signal('');
  protected readonly busy = signal(false);

  protected readonly form = this.fb.group({
    method: this.fb.nonNullable.control<'card' | 'bank'>('card'),
    cardNumber: this.fb.control('', [Validators.required]),
    cardHolder: this.fb.control('', [Validators.required, Validators.minLength(2)]),
    cardExpiry: this.fb.control('', [Validators.required]),
    cardCvv: this.fb.control('', [Validators.required]),
    bankCode: this.fb.control(''),
    bankAccountNumber: this.fb.control(''),
  });

  protected readonly statusLabel = orderStatusLabelTh;

  protected cardMaskedPreview(): string {
    const digits = String(this.form.get('cardNumber')?.value ?? '')
      .replace(/\D/g, '')
      .slice(0, 19);
    const groups: string[] = [];
    for (let g = 0; g < 4; g++) {
      const start = g * 4;
      const chunk = digits.slice(start, start + 4);
      if (chunk.length === 0) {
        groups.push('••••');
      } else if (chunk.length < 4) {
        groups.push(chunk + '\u2022'.repeat(4 - chunk.length));
      } else {
        groups.push(chunk);
      }
    }
    return groups.join(' ');
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id < 1) {
      void this.router.navigateByUrl('/orders');
      return;
    }

    this.orders.getById(id).subscribe({
      next: (o) => {
        if (o.status !== 0) {
          this.loadError.set('คำสั่งซื้อนี้ไม่ได้อยู่ในสถานะรอชำระเงินแล้ว');
          this.order.set(o);
          return;
        }
        this.order.set(o);
        this.setPaymentMethod('card');
      },
      error: () => this.loadError.set('โหลดคำสั่งซื้อไม่สำเร็จ'),
    });
  }

  protected setPaymentMethod(m: 'card' | 'bank'): void {
    this.form.patchValue({ method: m });
    if (m === 'card') {
      this.form.get('cardNumber')?.setValidators([Validators.required]);
      this.form.get('cardHolder')?.setValidators([Validators.required, Validators.minLength(2)]);
      this.form.get('cardExpiry')?.setValidators([Validators.required]);
      this.form.get('cardCvv')?.setValidators([Validators.required]);
      this.form.get('bankCode')?.clearValidators();
      this.form.get('bankAccountNumber')?.clearValidators();
    } else {
      this.form.get('bankCode')?.setValidators([Validators.required]);
      this.form.get('bankAccountNumber')?.setValidators([Validators.required]);
      this.form.get('cardNumber')?.clearValidators();
      this.form.get('cardHolder')?.clearValidators();
      this.form.get('cardExpiry')?.clearValidators();
      this.form.get('cardCvv')?.clearValidators();
    }
    ['cardNumber', 'cardHolder', 'cardExpiry', 'cardCvv', 'bankCode', 'bankAccountNumber'].forEach((k) =>
      this.form.get(k)?.updateValueAndValidity(),
    );
  }

  protected submit(): void {
    const o = this.order();
    if (!o || o.status !== 0) {
      return;
    }
    this.setPaymentMethod(this.form.get('method')?.value ?? 'card');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.error.set('');
    const pay = this.form.getRawValue() as {
      method: 'card' | 'bank';
      cardNumber: string;
      cardHolder: string;
      cardExpiry: string;
      cardCvv: string;
      bankCode: string;
      bankAccountNumber: string;
    };
    const simBody: SimulatePaymentRequest =
      pay.method === 'card'
        ? {
            paymentMethod: 'card',
            cardNumber: pay.cardNumber,
            cardHolder: pay.cardHolder,
            cardExpiry: pay.cardExpiry,
            cardCvv: pay.cardCvv,
          }
        : {
            paymentMethod: 'bank',
            bankCode: pay.bankCode,
            bankAccountNumber: pay.bankAccountNumber,
          };

    this.busy.set(true);
    this.orders
      .simulatePayment(o.id, simBody)
      .pipe(
        finalize(() => this.busy.set(false)),
        catchError((err: unknown) => {
          const message =
            err &&
            typeof err === 'object' &&
            'error' in err &&
            err.error &&
            typeof err.error === 'object' &&
            'message' in err.error
              ? String((err.error as { message: string }).message)
              : 'ชำระเงินไม่สำเร็จ';
          this.error.set(message);
          return throwError(() => err);
        }),
      )
      .subscribe({
        next: () => void this.router.navigateByUrl('/orders'),
      });
  }
}
