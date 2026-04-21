import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnInit,
  QueryList,
  ViewChildren,
  inject,
  signal,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, concatMap, finalize, throwError } from 'rxjs';
import { ThaiBahtPipe } from '../../../core/pipes/thai-baht.pipe';
import { CartService } from '../../../core/services/cart.service';
import { OrderService, type SimulatePaymentRequest } from '../../../core/services/order.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ThaiBahtPipe],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css',
})
export class CheckoutComponent implements OnInit, AfterViewInit {
  private readonly fb = inject(FormBuilder);
  protected readonly cart = inject(CartService);
  private readonly orders = inject(OrderService);
  private readonly router = inject(Router);

  @ViewChildren('qty') private readonly qtyInputs!: QueryList<ElementRef<HTMLInputElement>>;

  protected readonly error = signal('');
  protected readonly busy = signal(false);
  protected readonly checkoutTotal = signal(0);
  /** 1 = สรุปตะกร้า, 2 = จำลองชำระเงิน */
  protected readonly step = signal<1 | 2>(1);

  protected readonly form = this.fb.group({
    lines: this.fb.array<FormGroup>([]),
    payment: this.fb.group({
      method: this.fb.nonNullable.control<'card' | 'bank'>('card'),
      cardNumber: this.fb.control('', [Validators.required]),
      cardHolder: this.fb.control('', [Validators.required, Validators.minLength(2)]),
      cardExpiry: this.fb.control('', [Validators.required]),
      cardCvv: this.fb.control('', [Validators.required]),
      bankCode: this.fb.control(''),
      bankAccountNumber: this.fb.control(''),
    }),
  });

  get lineGroups(): FormArray<FormGroup> {
    return this.form.controls.lines as FormArray<FormGroup>;
  }

  protected get payment(): FormGroup {
    return this.form.controls.payment as FormGroup;
  }

  /** แสดงบนพรีวิวบัตร — กลุ่มละ 4 หลัก จุดแทนช่องว่าง */
  protected cardMaskedPreview(): string {
    const digits = String(this.payment.get('cardNumber')?.value ?? '')
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
    const lines = this.cart.lines();
    if (lines.length === 0) {
      void this.router.navigateByUrl('/shop');
      return;
    }

    for (const line of lines) {
      this.lineGroups.push(
        this.fb.group({
          productId: this.fb.nonNullable.control(line.product.id),
          quantity: this.fb.nonNullable.control(line.quantity, [
            Validators.required,
            Validators.min(1),
            Validators.max(line.product.stockQuantity),
          ]),
        }),
      );
    }

    this.recalcCheckoutTotal();
    this.form.controls.lines.valueChanges.subscribe(() => this.recalcCheckoutTotal());
    this.setPaymentMethod('card');
  }

  protected lineProductName(i: number): string {
    const pid = this.lineGroups.at(i)?.get('productId')?.value;
    if (typeof pid !== 'number') {
      return '';
    }
    return this.cart.lines().find((l) => l.product.id === pid)?.product.name ?? '';
  }

  protected lineUnitPrice(i: number): number {
    const pid = this.lineGroups.at(i)?.get('productId')?.value;
    if (typeof pid !== 'number') {
      return 0;
    }
    return this.cart.lines().find((l) => l.product.id === pid)?.product.price ?? 0;
  }

  protected lineAmount(i: number): number {
    const g = this.lineGroups.at(i);
    if (!g) {
      return 0;
    }
    const pid = g.get('productId')?.value;
    const qty = Number(g.get('quantity')?.value ?? 0);
    if (typeof pid !== 'number' || !Number.isFinite(qty)) {
      return 0;
    }
    const line = this.cart.lines().find((l) => l.product.id === pid);
    if (!line) {
      return 0;
    }
    return line.product.price * qty;
  }

  private recalcCheckoutTotal(): void {
    const raw = this.lineGroups.getRawValue() as Array<{ productId: number; quantity: number }>;
    let sum = 0;
    for (const r of raw) {
      const line = this.cart.lines().find((l) => l.product.id === r.productId);
      if (line && r.quantity != null) {
        sum += line.product.price * Number(r.quantity);
      }
    }
    this.checkoutTotal.set(sum);
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => this.qtyInputs.first?.nativeElement.focus());
  }

  protected removeLine(index: number): void {
    const group = this.lineGroups.at(index);
    const productId = group?.get('productId')?.value;
    if (typeof productId !== 'number') {
      return;
    }

    this.cart.remove(productId);
    this.lineGroups.removeAt(index);
    this.recalcCheckoutTotal();

    if (this.lineGroups.length === 0) {
      void this.router.navigateByUrl('/shop');
    }
  }

  protected goToPaymentStep(): void {
    this.error.set('');
    if (this.lineGroups.invalid) {
      this.lineGroups.markAllAsTouched();
      return;
    }
    this.setPaymentMethod(this.payment.get('method')?.value ?? 'card');
    this.step.set(2);
  }

  protected backToCartStep(): void {
    this.step.set(1);
  }

  protected setPaymentMethod(m: 'card' | 'bank'): void {
    this.payment.patchValue({ method: m });
    if (m === 'card') {
      this.payment.get('cardNumber')?.setValidators([Validators.required]);
      this.payment.get('cardHolder')?.setValidators([Validators.required, Validators.minLength(2)]);
      this.payment.get('cardExpiry')?.setValidators([Validators.required]);
      this.payment.get('cardCvv')?.setValidators([Validators.required]);
      this.payment.get('bankCode')?.clearValidators();
      this.payment.get('bankAccountNumber')?.clearValidators();
    } else {
      this.payment.get('bankCode')?.setValidators([Validators.required]);
      this.payment.get('bankAccountNumber')?.setValidators([Validators.required]);
      this.payment.get('cardNumber')?.clearValidators();
      this.payment.get('cardHolder')?.clearValidators();
      this.payment.get('cardExpiry')?.clearValidators();
      this.payment.get('cardCvv')?.clearValidators();
    }
    ['cardNumber', 'cardHolder', 'cardExpiry', 'cardCvv', 'bankCode', 'bankAccountNumber'].forEach((k) =>
      this.payment.get(k)?.updateValueAndValidity(),
    );
  }

  submit(): void {
    this.error.set('');
    if (this.lineGroups.invalid) {
      this.lineGroups.markAllAsTouched();
      return;
    }
    this.setPaymentMethod(this.payment.get('method')?.value ?? 'card');
    if (this.payment.invalid) {
      this.payment.markAllAsTouched();
      return;
    }

    const raw = this.lineGroups.getRawValue() as Array<{ productId: number; quantity: number }>;
    const pay = this.payment.getRawValue() as {
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
      .placeOrder({ lines: raw.map((l) => ({ productId: l.productId, quantity: l.quantity })) })
      .pipe(
        concatMap((order) => this.orders.simulatePayment(order.id, simBody)),
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
        next: () => {
          this.cart.clear();
          void this.router.navigateByUrl('/orders');
        },
      });
  }
}
