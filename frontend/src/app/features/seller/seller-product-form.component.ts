import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-seller-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './seller-product-form.component.html',
  styleUrl: './seller-product-form.component.css',
})
export class SellerProductFormComponent implements OnInit, AfterViewInit {
  private readonly fb = inject(FormBuilder);
  private readonly products = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  @ViewChild('nameInput') protected nameInput?: ElementRef<HTMLInputElement>;

  protected readonly error = signal('');
  protected readonly busy = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    price: [0, [Validators.required, Validators.min(0.01)]],
    stockQuantity: [0, [Validators.required, Validators.min(0)]],
  });

  protected productId: number | null = null;
  protected productName = signal('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.productId = null;
      return;
    }

    this.productId = Number(id);
    this.products.getById(this.productId).subscribe({
      next: (p) => {
        this.productName.set(p.name);
        this.form.patchValue({
          name: p.name,
          description: p.description ?? '',
          price: p.price,
          stockQuantity: p.stockQuantity,
        });
      },
      error: () => void this.router.navigateByUrl('/shop'),
    });
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => this.nameInput?.nativeElement.focus());
  }

  submit(): void {
    this.error.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.busy.set(true);
    const body = {
      name: v.name,
      description: v.description ? v.description : null,
      price: v.price,
      stockQuantity: v.stockQuantity,
    };

    const req$ =
      this.productId == null
        ? this.products.create(body)
        : this.products.update(this.productId, body);

    req$.subscribe({
      next: () => void this.router.navigateByUrl('/shop'),
      error: () => {
        this.error.set('บันทึกไม่สำเร็จ');
        this.busy.set(false);
      },
      complete: () => this.busy.set(false),
    });
  }

  protected async confirmDelete(): Promise<void> {
    if (this.productId == null) {
      return;
    }

    const result = await Swal.fire({
      title: 'ลบสินค้านี้?',
      html: `จะลบ <strong>${this.productName() || 'สินค้า'}</strong> (#${this.productId}) ถาวร`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'ลบ',
      cancelButtonText: 'ยกเลิก',
      confirmButtonColor: '#b91c1c',
      reverseButtons: true,
      focusCancel: true,
    });

    if (!result.isConfirmed) {
      return;
    }

    this.error.set('');
    this.busy.set(true);
    this.products.delete(this.productId).subscribe({
      next: async () => {
        this.busy.set(false);
        await Swal.fire({
          title: 'ลบแล้ว',
          icon: 'success',
          timer: 1800,
          showConfirmButton: false,
        });
        void this.router.navigateByUrl('/shop');
      },
      error: async () => {
        this.busy.set(false);
        await Swal.fire({
          title: 'ลบไม่สำเร็จ',
          text: 'ไม่มีสิทธิ์หรือสินค้าไม่มีอยู่แล้ว',
          icon: 'error',
        });
      },
    });
  }
}
