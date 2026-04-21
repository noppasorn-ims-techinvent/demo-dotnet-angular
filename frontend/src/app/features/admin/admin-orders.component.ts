import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import Swal from 'sweetalert2';
import { forkJoin } from 'rxjs';
import type { OrderDto, OrderLineDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { MarketplaceHubService } from '../../core/services/marketplace-hub.service';
import { OrderService } from '../../core/services/order.service';
import { orderStatusLabelTh } from '../../core/utils/order-status';

export interface SellerCancelBucket {
  sellerId: number;
  displayName: string;
  lines: OrderLineDto[];
}

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ThaiBahtPipe],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css',
})
export class AdminOrdersComponent {
  private readonly ordersApi = inject(OrderService);
  private readonly hub = inject(MarketplaceHubService);

  protected readonly orders = signal<OrderDto[]>([]);
  /** รายละเอียดเต็มสำหรับออเดอร์ที่รอยกเลิก (แยกร้าน) */
  protected readonly orderDetails = signal<Record<number, OrderDto>>({});
  protected readonly error = signal('');
  protected readonly busy = signal(false);
  protected readonly statusLabel = orderStatusLabelTh;

  protected readonly statusOptions = [
    { value: 0, label: 'รอชำระเงิน' },
    { value: 1, label: 'ชำระแล้ว' },
    { value: 2, label: 'จัดส่งแล้ว' },
    { value: 3, label: 'ยกเลิก' },
  ];

  constructor() {
    effect(() => {
      this.hub.ordersRefreshTick();
      untracked(() => this.reload());
    });
  }

  protected reload(): void {
    this.ordersApi.getAll().subscribe({
      next: (list) => {
        this.orders.set(list);
        const pending = list.filter((o) => o.status === 4);
        if (pending.length === 0) {
          this.orderDetails.set({});
          return;
        }
        forkJoin(pending.map((o) => this.ordersApi.getById(o.id))).subscribe({
          next: (details) => {
            const rec: Record<number, OrderDto> = {};
            pending.forEach((o, i) => {
              rec[o.id] = details[i]!;
            });
            this.orderDetails.set(rec);
          },
          error: () => this.error.set('โหลดรายละเอียดคำขอยกเลิกไม่สำเร็จ'),
        });
      },
      error: () => this.error.set('โหลดคำสั่งซื้อไม่สำเร็จ'),
    });
  }

  protected pendingSellerBuckets(detail: OrderDto): SellerCancelBucket[] {
    if (detail.status !== 4) {
      return [];
    }
    const map = new Map<number, SellerCancelBucket>();
    for (const line of detail.lines) {
      const st = line.lineCancellationState ?? 1;
      if (st !== 1 && st !== 0) {
        continue;
      }
      const cur = map.get(line.sellerId);
      if (cur) {
        cur.lines.push(line);
      } else {
        map.set(line.sellerId, {
          sellerId: line.sellerId,
          displayName: line.sellerDisplayName || `ผู้ขาย #${line.sellerId}`,
          lines: [line],
        });
      }
    }
    return [...map.values()];
  }

  protected updateStatus(order: OrderDto, status: number): void {
    this.busy.set(true);
    this.ordersApi.updateStatus(order.id, status).subscribe({
      next: () => this.reload(),
      error: (err: unknown) => {
        const msg =
          err &&
          typeof err === 'object' &&
          'error' in err &&
          err.error &&
          typeof err.error === 'object' &&
          'message' in err.error
            ? String((err.error as { message: string }).message)
            : 'อัปเดตสถานะไม่สำเร็จ';
        this.error.set(msg);
        this.busy.set(false);
      },
      complete: () => this.busy.set(false),
    });
  }

  protected async reviewCancellation(order: OrderDto, targetSellerUserId: number, approved: boolean): Promise<void> {
    const result = await Swal.fire({
      title: approved ? 'อนุมัติยกเลิก (เฉพาะร้านนี้)' : 'ไม่อนุมัติ — แยกเป็นออเดอร์ร้านนี้',
      input: 'textarea',
      inputLabel: approved ? 'หมายเหตุ (แสดงให้ลูกค้า)' : 'เหตุผลที่ไม่อนุมัติ',
      inputPlaceholder: approved ? 'เช่น ยืนยันยกเลิกตามคำขอ' : 'อธิบายเหตุผล…',
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

    this.error.set('');
    this.busy.set(true);
    this.ordersApi.reviewCancellation(order.id, approved, result.value.trim(), targetSellerUserId).subscribe({
      next: async () => {
        this.busy.set(false);
        await Swal.fire({
          icon: 'success',
          title: approved ? 'อนุมัติแล้ว' : 'บันทึกแล้ว',
          text: approved ? 'ยกเลิกเฉพาะสินค้าของร้านที่เลือก' : 'สินค้าของร้านนี้ถูกแยกไปออเดอร์ใหม่',
          timer: 2400,
          showConfirmButton: false,
        });
        this.reload();
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
