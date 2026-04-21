import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import Swal from 'sweetalert2';
import type { ProductDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { AuthService } from '../../core/services/auth.service';
import { MarketplaceHubService } from '../../core/services/marketplace-hub.service';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-seller-stock',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ThaiBahtPipe],
  templateUrl: './seller-stock.component.html',
  styleUrl: './seller-stock.component.css',
})
export class SellerStockComponent {
  private readonly productsApi = inject(ProductService);
  protected readonly auth = inject(AuthService);
  private readonly hub = inject(MarketplaceHubService);

  protected readonly items = signal<ProductDto[]>([]);
  /** จำนวนร่างในแบบฟอร์ม (แก้แล้วกดบันทึก) */
  protected draft: Record<number, number> = {};
  protected readonly error = signal('');
  protected readonly busyId = signal<number | null>(null);

  constructor() {
    effect(() => {
      this.hub.productDataRefreshTick();
      untracked(() => this.reloadList());
    });
  }

  private reloadList(): void {
    const req$ = this.auth.hasRole('Admin')
      ? this.productsApi.getCatalog()
      : this.productsApi.getMine();
    req$.subscribe({
      next: (list) => {
        this.items.set(list);
        const d: Record<number, number> = {};
        for (const p of list) {
          d[p.id] = p.stockQuantity;
        }
        this.draft = d;
      },
      error: () => this.error.set('โหลดรายการไม่สำเร็จ'),
    });
  }

  protected canDeleteProduct(p: ProductDto): boolean {
    return this.auth.hasRole('Admin') || (this.auth.hasRole('Seller') && this.auth.user()?.id === p.sellerId);
  }

  protected save(p: ProductDto): void {
    const qty = this.draft[p.id];
    if (qty == null || qty < 0 || !Number.isFinite(qty)) {
      this.error.set('กรุณากรอกจำนวนคงเหลือเป็นตัวเลขที่ไม่ติดลบ');
      return;
    }
    const stockQuantity = Math.floor(Number(qty));
    this.error.set('');
    this.busyId.set(p.id);
    this.productsApi
      .update(p.id, {
        name: p.name,
        description: p.description ?? null,
        price: p.price,
        stockQuantity,
      })
      .subscribe({
        next: (updated) => {
          this.items.update((list) => list.map((x) => (x.id === p.id ? updated : x)));
          this.draft[p.id] = updated.stockQuantity;
          this.busyId.set(null);
        },
        error: () => {
          this.error.set('บันทึกไม่สำเร็จ');
          this.busyId.set(null);
        },
      });
  }

  protected async confirmDelete(p: ProductDto): Promise<void> {
    const result = await Swal.fire({
      title: 'ลบสินค้านี้?',
      html: `จะลบ <strong>${p.name}</strong> (#${p.id}) ถาวร`,
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
    this.busyId.set(p.id);
    this.productsApi.delete(p.id).subscribe({
      next: async () => {
        this.busyId.set(null);
        await Swal.fire({
          title: 'ลบแล้ว',
          icon: 'success',
          timer: 1600,
          showConfirmButton: false,
        });
        this.reloadList();
      },
      error: async () => {
        this.busyId.set(null);
        await Swal.fire({
          title: 'ลบไม่สำเร็จ',
          text: 'ไม่มีสิทธิ์หรือสินค้าไม่มีอยู่แล้ว',
          icon: 'error',
        });
      },
    });
  }
}
