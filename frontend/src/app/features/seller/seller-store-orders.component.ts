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

  /** รอยกเลิก + มีสินค้าร้านนี้ในออเดอร์ — รวมข้อมูลเก่าที่บรรทัดยังเป็น 0 แต่สถานะออเดอร์เป็น 4 */
  protected needsMyCancellationReview(o: OrderDto): boolean {
    return o.status === 4 && o.lines.length > 0;
  }

  protected async reviewCancellation(order: OrderDto, approved: boolean): Promise<void> {
    const result = await Swal.fire({
      title: approved ? 'อนุมัติยกเลิก (เฉพาะร้านคุณ)' : 'ไม่อนุมัติ — แยกเป็นออเดอร์ร้านคุณ',
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
    const storeSellerId = order.lines[0]?.sellerId;
    const targetSeller =
      typeof storeSellerId === 'number' && storeSellerId >= 1 ? storeSellerId : undefined;
    this.ordersApi.reviewCancellation(order.id, approved, result.value.trim(), targetSeller).subscribe({
      next: async () => {
        this.busy.set(false);
        await Swal.fire({ icon: 'success', title: 'บันทึกแล้ว', timer: 1800, showConfirmButton: false });
        this.load();
      },
      error: async (err: unknown) => {
        this.busy.set(false);
        const msg =
          err &&
          typeof err === 'object' &&
          'error' in err &&
          err.error &&
          typeof err.error === 'object' &&
          'message' in err.error
            ? String((err.error as { message: string }).message)
            : 'ดำเนินการไม่สำเร็จ';
        await Swal.fire({ icon: 'error', title: 'ดำเนินการไม่สำเร็จ', text: msg });
      },
    });
  }
}
