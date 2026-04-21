import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { OrderDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { MarketplaceHubService } from '../../core/services/marketplace-hub.service';
import { OrderService } from '../../core/services/order.service';

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

  protected statusLabel(value: number): string {
    switch (value) {
      case 0:
        return 'รอดำเนินการ';
      case 1:
        return 'ชำระแล้ว';
      case 2:
        return 'จัดส่งแล้ว';
      case 3:
        return 'ยกเลิก';
      default:
        return String(value);
    }
  }
}
