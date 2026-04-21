import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import Swal from 'sweetalert2';
import type { OrderDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { MarketplaceHubService } from '../../core/services/marketplace-hub.service';
import { OrderService } from '../../core/services/order.service';
import { orderStatusLabelTh } from '../../core/utils/order-status';

@Component({
  selector: 'app-seller-store-orders',
  standalone: true,
  imports: [CommonModule, RouterLink, ThaiBahtPipe],
  templateUrl: './seller-store-orders.component.html',
  styleUrl: './seller-store-orders.component.css',
})
export class SellerStoreOrdersComponent {
  private readonly ordersApi = inject(OrderService);
  private readonly hub = inject(MarketplaceHubService);

  protected readonly orders = signal<OrderDto[]>([]);
  protected readonly error = signal('');
  protected readonly busy = signal(false);
  protected readonly statusLabel = orderStatusLabelTh;

  constructor() {
    effect(() => {
      this.hub.storeOrdersRefreshTick();
      untracked(() => this.load());
    });
  }

  private load(): void {
    this.ordersApi.getForMyStore().subscribe({
      next: (o) => this.orders.set(o),
      error: () => this.error.set('โหลดออเดอร์เข้าร้านไม่สำเร็จ'),
    });
  }

  protected async reviewCancellation(order: OrderDto, approved: boolean): Promise<void> {
    const result = await Swal.fire({
      title: approved ? 'อนุมัติยกเลิกคำสั่งซื้อ' : 'ไม่อนุมัติคำขอยกเลิก',
      input: 'textarea',
      inputLabel: approved ? 'หมายเหตุ (แสดงให้ลูกค้า)' : 'เหตุผลที่ไม่อนุมัติ',
      showCancelButton: true,
      confirmButtonText: 'ยืนยัน',
      cancelButtonText: 'ยกเลิก',
      confirmButtonColor: approved ? '#15803d' : '#b91c1c',
      cancelButtonColor: '#6b7280',
      reverseButtons: true,
      inputValidator: (v) => (!v?.trim() ? 'กรุณากรอกข้อความ' : null),
    });

    if (!result.isConfirmed || !result.value?.trim()) {
      return;
    }

    this.busy.set(true);
    this.ordersApi.reviewCancellation(order.id, approved, result.value.trim()).subscribe({
      next: async () => {
        this.busy.set(false);
        await Swal.fire({ icon: 'success', title: 'บันทึกแล้ว', timer: 1800, showConfirmButton: false });
        this.load();
      },
      error: async () => {
        this.busy.set(false);
        await Swal.fire({ icon: 'error', title: 'ดำเนินการไม่สำเร็จ' });
      },
    });
  }
}
