import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import Swal from 'sweetalert2';
import type { OrderDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ThaiBahtPipe],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css',
})
export class AdminOrdersComponent implements OnInit {
  private readonly ordersApi = inject(OrderService);

  protected readonly orders = signal<OrderDto[]>([]);
  protected readonly error = signal('');
  protected readonly busy = signal(false);

  protected readonly statusOptions = [
    { value: 0, label: 'รอดำเนินการ' },
    { value: 1, label: 'ชำระแล้ว' },
    { value: 2, label: 'จัดส่งแล้ว' },
    { value: 3, label: 'ยกเลิก' },
  ];

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.ordersApi.getAll().subscribe({
      next: (o) => this.orders.set(o),
      error: () => this.error.set('โหลดคำสั่งซื้อไม่สำเร็จ'),
    });
  }

  protected updateStatus(order: OrderDto, status: number): void {
    this.busy.set(true);
    this.ordersApi.updateStatus(order.id, status).subscribe({
      next: () => this.reload(),
      error: () => {
        this.error.set('อัปเดตสถานะไม่สำเร็จ');
        this.busy.set(false);
      },
      complete: () => this.busy.set(false),
    });
  }

  protected deleteOrder(order: OrderDto): void {
    void this.confirmAndDelete(order);
  }

  private async confirmAndDelete(order: OrderDto): Promise<void> {
    const result = await Swal.fire({
      title: 'ลบคำสั่งซื้อนี้หรือไม่?',
      html: `คำสั่งซื้อ <strong>#${order.id}</strong> จะถูกลบ<br/><span style="color:#6b7280;font-size:0.95rem">สต็อกสินค้าจะถูกคืนตามรายการในคำสั่งซื้อ</span>`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'ลบ',
      cancelButtonText: 'ยกเลิก',
      confirmButtonColor: '#b91c1c',
      cancelButtonColor: '#6b7280',
      reverseButtons: true,
      focusCancel: true,
    });

    if (!result.isConfirmed) {
      return;
    }

    this.error.set('');
    this.busy.set(true);
    this.ordersApi.delete(order.id).subscribe({
      next: async () => {
        this.busy.set(false);
        await Swal.fire({
          title: 'ลบแล้ว',
          text: `ลบคำสั่งซื้อ #${order.id} แล้ว`,
          icon: 'success',
          timer: 2200,
          showConfirmButton: false,
        });
        this.reload();
      },
      error: async () => {
        this.busy.set(false);
        await Swal.fire({
          title: 'ลบไม่สำเร็จ',
          text: 'ไม่สามารถลบคำสั่งซื้อนี้ได้ ลองอีกครั้ง',
          icon: 'error',
          confirmButtonColor: '#2563eb',
        });
      },
    });
  }
}
