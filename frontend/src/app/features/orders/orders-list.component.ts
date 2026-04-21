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
  selector: 'app-orders-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ThaiBahtPipe],
  templateUrl: './orders-list.component.html',
  styleUrl: './orders-list.component.css',
})
export class OrdersListComponent {
  private readonly ordersApi = inject(OrderService);
  private readonly hub = inject(MarketplaceHubService);

  protected readonly orders = signal<OrderDto[]>([]);
  protected readonly error = signal('');
  protected readonly busy = signal(false);
  protected readonly statusLabel = orderStatusLabelTh;

  constructor() {
    effect(() => {
      this.hub.ordersRefreshTick();
      untracked(() => this.load());
    });
  }

  private load(): void {
    this.ordersApi.getMine().subscribe({
      next: (o) => this.orders.set(o),
      error: () => this.error.set('โหลดคำสั่งซื้อไม่สำเร็จ'),
    });
  }

  protected canPay(o: OrderDto): boolean {
    return o.status === 0;
  }

  protected canRequestCancel(o: OrderDto): boolean {
    return o.status === 0 || o.status === 1;
  }

  protected async requestCancel(o: OrderDto): Promise<void> {
    const result = await Swal.fire({
      title: `ขอยกเลิก #${o.id}`,
      input: 'textarea',
      inputLabel: 'เหตุผลที่ต้องการยกเลิก',
      inputPlaceholder: 'อธิบายสั้น ๆ …',
      showCancelButton: true,
      confirmButtonText: 'ส่งคำขอ',
      cancelButtonText: 'ยกเลิก',
      confirmButtonColor: '#b45309',
      cancelButtonColor: '#6b7280',
      reverseButtons: true,
      inputValidator: (v) => {
        if (!v?.trim()) {
          return 'กรุณากรอกเหตุผล';
        }
        return null;
      },
    });

    if (!result.isConfirmed || !result.value?.trim()) {
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.ordersApi.requestCancellation(o.id, result.value.trim()).subscribe({
      next: () => {
        this.busy.set(false);
        void Swal.fire({
          icon: 'success',
          title: 'ส่งคำขอแล้ว',
          text: 'รอร้านค้าหรือผู้ดูแลระบบพิจารณา',
          timer: 2800,
          showConfirmButton: false,
        });
        this.load();
      },
      error: (err: unknown) => {
        this.busy.set(false);
        const msg =
          err &&
          typeof err === 'object' &&
          'error' in err &&
          err.error &&
          typeof err.error === 'object' &&
          'message' in err.error
            ? String((err.error as { message: string }).message)
            : 'ส่งคำขอไม่สำเร็จ';
        this.error.set(msg);
      },
    });
  }
}
