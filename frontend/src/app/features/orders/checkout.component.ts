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
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
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

    this.form.controls.lines.valueChanges.subscribe(() => {
      /* Demo: react to quantity edits (could sync back to cart). */
    });
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
