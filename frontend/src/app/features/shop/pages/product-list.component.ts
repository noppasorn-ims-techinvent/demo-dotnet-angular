import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ProductDto } from '../../../core/models/api.types';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { MarketplaceHubService } from '../../../core/services/marketplace-hub.service';
import { ProductService } from '../../../core/services/product.service';
import { ThaiBahtPipe } from '../../../core/pipes/thai-baht.pipe';
import { ProductCardComponent } from '../components/product-card.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent, ThaiBahtPipe],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductListComponent {
  private readonly productsApi = inject(ProductService);
  protected readonly auth = inject(AuthService);
  protected readonly cart = inject(CartService);
  private readonly hub = inject(MarketplaceHubService);

  protected readonly products = signal<ProductDto[]>([]);
  protected readonly error = signal('');

  constructor() {
    effect(() => {
      this.hub.productDataRefreshTick();
      untracked(() => this.loadCatalog());
    });
  }

  private loadCatalog(): void {
    this.productsApi.getCatalog().subscribe({
      next: (items) => this.products.set(items),
      error: () => this.error.set('โหลดรายการสินค้าไม่สำเร็จ'),
    });
  }

  protected trackById(_index: number, item: ProductDto): number {
    return item.id;
  }

  protected onAddToCart(event: { product: ProductDto; quantity: number }): void {
    if (!this.auth.hasRole('Buyer')) {
      return;
    }

    this.cart.add(event.product, event.quantity);
  }

  protected removeFromCart(productId: number): void {
    this.cart.remove(productId);
  }
}
