import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import type { ProductDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ThaiBahtPipe],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css',
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly cart = inject(CartService);

  protected readonly product = signal<ProductDto | null>(null);

  ngOnInit(): void {
    const resolved = this.route.snapshot.data['product'] as ProductDto | null;
    if (!resolved) {
      void this.router.navigateByUrl('/shop');
      return;
    }

    this.product.set(resolved);
  }

  protected addOne(): void {
    if (!this.auth.hasRole('Buyer')) {
      return;
    }

    const p = this.product();
    if (p) {
      this.cart.add(p, 1);
    }
  }
}
