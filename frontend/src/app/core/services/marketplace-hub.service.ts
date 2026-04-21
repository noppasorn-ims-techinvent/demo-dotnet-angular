import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HttpTransportType, HubConnectionState } from '@microsoft/signalr';
import Swal from 'sweetalert2';
import type { UserDto } from '../models/api.types';

function statusLabelThai(status: string): string {
  const map: Record<string, string> = {
    Pending: 'รอดำเนินการ',
    Paid: 'ชำระแล้ว',
    Shipped: 'จัดส่งแล้ว',
    Cancelled: 'ยกเลิก',
  };
  return map[status] ?? status;
}

/** toast ให้อยู่เหนือ navbar Bootstrap (sticky ~1030) */
const toastMixin = Swal.mixin({
  toast: true,
  position: 'top-end',
  showConfirmButton: false,
  customClass: { container: 'swal2-toast-above-nav' },
});

@Injectable({ providedIn: 'root' })
export class MarketplaceHubService {
  private hub?: HubConnection;
  private lastUser?: UserDto;
  private lastToken = '';

  /** ให้หน้า orders โหลดรายการใหม่ */
  readonly ordersRefreshTick = signal(0);
  /** ให้หน้าร้านค้า / ปรับสต็อก โหลดรายการสินค้าใหม่ */
  readonly productDataRefreshTick = signal(0);
  /** true หลัง start + join กลุ่มสำเร็จ (ดีบัก) */
  readonly connected = signal(false);

  async connectForUser(user: UserDto, accessToken: string): Promise<void> {
    await this.disconnect();

    const roles = user.roles ?? [];
    const needsHub = roles.includes('Buyer') || roles.includes('Seller') || roles.includes('Admin');
    if (!needsHub) {
      this.connected.set(false);
      return;
    }

    if (!accessToken?.trim()) {
      console.warn('[MarketplaceHub] ไม่มี access token — ข้ามการต่อ SignalR');
      this.connected.set(false);
      return;
    }

    this.lastUser = user;
    this.lastToken = accessToken;

    this.hub = new HubConnectionBuilder()
      .withUrl('/hubs/marketplace', {
        accessTokenFactory: () => this.lastToken,
        transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
        skipNegotiation: false,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    this.registerHandlers();

    this.hub.onreconnected(async () => {
      console.info('[MarketplaceHub] reconnect แล้ว — join กลุ่มใหม่');
      if (this.lastUser) {
        await this.joinAllGroups(this.lastUser);
      }
    });

    this.hub.onclose((err) => {
      this.connected.set(false);
      if (err) {
        console.warn('[MarketplaceHub] connection ปิด:', err.message);
      }
    });

    try {
      await this.hub.start();
      await this.joinAllGroups(user);
      this.connected.set(this.hub.state === HubConnectionState.Connected);
      if (this.connected()) {
        console.info('[MarketplaceHub] เชื่อมต่อและ join กลุ่มสำเร็จ');
      }
    } catch (e) {
      this.connected.set(false);
      console.error('[MarketplaceHub] start/join ล้มเหลว:', e);
      void toastMixin.fire({
        icon: 'error',
        title: 'แจ้งเตือนแบบเรียลไทม์ใช้ไม่ได้',
        text: 'ให้รัน backend ก่อน (พอร์ตเดียวกับ proxy ค่าเริ่มต้น 5197) หรือตั้ง BACKEND_URL แล้ว npm start',
        timer: 9000,
        timerProgressBar: true,
      });
    }
  }

  private registerHandlers(): void {
    if (!this.hub) {
      return;
    }

    this.hub.on('orderStatusChanged', (payload: { orderId: number; status: string }) => {
      this.ordersRefreshTick.update((n) => n + 1);
      void toastMixin.fire({
        icon: 'info',
        title: `คำสั่งซื้อ #${payload.orderId}`,
        text: `สถานะ: ${statusLabelThai(payload.status)}`,
        timer: 5000,
        timerProgressBar: true,
      });
    });

    this.hub.on('buyerOrderDeletedByAdmin', (payload: { orderId: number }) => {
      this.ordersRefreshTick.update((n) => n + 1);
      void toastMixin.fire({
        icon: 'warning',
        title: 'คำสั่งซื้อถูกลบ',
        text: `ผู้ดูแลระบบลบคำสั่งซื้อ #${payload.orderId} แล้ว`,
        timer: 6000,
        timerProgressBar: true,
      });
    });

    this.hub.on('sellerOrderRemovedByAdmin', (payload: { orderId: number }) => {
      this.productDataRefreshTick.update((n) => n + 1);
      void toastMixin.fire({
        icon: 'warning',
        title: 'คำสั่งซื้อถูกลบโดยแอดมิน',
        text: `คำสั่งซื้อ #${payload.orderId} (มีสินค้าของคุณ) — สต็อกถูกคืนแล้ว`,
        timer: 6500,
        timerProgressBar: true,
      });
    });

    this.hub.on(
      'sellerNewOrder',
      (payload: { orderId: number; buyerId: number; totalAmount: number; lineCount: number }) => {
        this.productDataRefreshTick.update((n) => n + 1);
        void toastMixin.fire({
          icon: 'success',
          title: 'มีคำสั่งซื้อใหม่',
          text: `ออเดอร์ #${payload.orderId} · ผู้ซื้อ #${payload.buyerId} · ${payload.lineCount} รายการ · ยอด ${payload.totalAmount.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} บาท`,
          timer: 7000,
          timerProgressBar: true,
        });
      },
    );

    this.hub.on('productUpdatedByAdmin', (payload: { productId: number; name: string }) => {
      this.productDataRefreshTick.update((n) => n + 1);
      void toastMixin.fire({
        icon: 'info',
        title: 'แอดมินแก้ไขสินค้า',
        text: `${payload.name} (#${payload.productId})`,
        timer: 5500,
        timerProgressBar: true,
      });
    });

    this.hub.on('productUpdatedBySeller', (payload: { productId: number; sellerId: number; name: string }) => {
      this.productDataRefreshTick.update((n) => n + 1);
      void toastMixin.fire({
        icon: 'info',
        title: 'ผู้ขายอัปเดตสินค้า',
        text: `${payload.name} (#${payload.productId})`,
        timer: 5500,
        timerProgressBar: true,
      });
    });
  }

  private async joinAllGroups(user: UserDto): Promise<void> {
    if (!this.hub) {
      return;
    }
    const roles = user.roles ?? [];
    if (roles.includes('Buyer')) {
      try {
        await this.hub.invoke('JoinBuyerGroup', user.id);
      } catch (e) {
        console.error('[MarketplaceHub] JoinBuyerGroup ล้มเหลว', e);
      }
    }
    if (roles.includes('Seller')) {
      try {
        await this.hub.invoke('JoinSellerGroup', user.id);
      } catch (e) {
        console.error('[MarketplaceHub] JoinSellerGroup ล้มเหลว', e);
      }
    }
    if (roles.includes('Admin')) {
      try {
        await this.hub.invoke('JoinAdminGroup');
      } catch (e) {
        console.error('[MarketplaceHub] JoinAdminGroup ล้มเหลว', e);
      }
    }
  }

  async disconnect(): Promise<void> {
    this.lastUser = undefined;
    this.lastToken = '';
    this.connected.set(false);
    if (!this.hub) {
      return;
    }

    try {
      await this.hub.stop();
    } catch {
      /* ignore */
    } finally {
      this.hub = undefined;
    }
  }
}
