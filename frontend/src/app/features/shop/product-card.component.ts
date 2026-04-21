import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ProductDto } from '../../core/models/api.types';
import { ThaiBahtPipe } from '../../core/pipes/thai-baht.pipe';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink, ThaiBahtPipe],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent implements OnChanges {
  @Input({ required: true }) product!: ProductDto;

  /** ส่งเหตุการณ์ไป parent (demo @Output — parent ใช้ (addToCart)="onAddToCart($event)") */
  @Output() readonly addToCart = new EventEmitter<{ product: ProductDto; quantity: number }>();

  protected readonly auth = inject(AuthService);

  /** Demo: log when @Input reference or fields change between change detection cycles. */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['product'] && !changes['product'].firstChange) {
      // Demo: `ngOnChanges` when the parent rebinds a different `product` instance.
    }
  }

  protected emitAdd(qty: number): void {
    if (!this.auth.hasRole('Buyer')) {
      return;
    }

    this.addToCart.emit({ product: this.product, quantity: qty });
  }
}
