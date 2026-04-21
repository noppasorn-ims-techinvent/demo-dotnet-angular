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
import { catchError, finalize, throwError } from 'rxjs';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';

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
  /** ยอดรวมจากจำนวนในฟอร์ม × ราคาจากตะกร้า */
  protected readonly checkoutTotal = signal(0);

  protected readonly form = this.fb.group({
    lines: this.fb.array<FormGroup>([]),
  });

  get lineGroups(): FormArray<FormGroup> {
    return this.form.controls.lines as FormArray<FormGroup>;
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

  /** ราคารายแถว = ราคาต่อชิ้น × จำนวนในฟอร์ม */
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

  submit(): void {
    this.error.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.lineGroups.getRawValue() as Array<{ productId: number; quantity: number }>;
    this.busy.set(true);
    this.orders
      .placeOrder({ lines: raw.map((l) => ({ productId: l.productId, quantity: l.quantity })) })
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
              : 'สั่งซื้อไม่สำเร็จ';
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
