import { Injectable, computed, signal } from '@angular/core';
import type { CartLine, ProductDto } from '../models/api.types';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly _lines = signal<CartLine[]>([]);

  readonly lines = this._lines.asReadonly();

  readonly totalQuantity = computed(() =>
    this._lines().reduce((sum, l) => sum + l.quantity, 0),
  );

  readonly totalAmount = computed(() =>
    this._lines().reduce((sum, l) => sum + l.quantity * l.product.price, 0),
  );

  add(product: ProductDto, quantity: number): void {
    this._lines.update((lines) => {
      const idx = lines.findIndex((l) => l.product.id === product.id);
      if (idx === -1) {
        return [...lines, { product, quantity }];
      }
      const next = [...lines];
      const merged = next[idx]!;
      next[idx] = { product, quantity: merged.quantity + quantity };
      return next;
    });
  }

  setQuantity(productId: number, quantity: number): void {
    this._lines.update((lines) => {
      if (quantity <= 0) {
        return lines.filter((l) => l.product.id !== productId);
      }
      return lines.map((l) => (l.product.id === productId ? { ...l, quantity } : l));
    });
  }

  remove(productId: number): void {
    this._lines.update((lines) => lines.filter((l) => l.product.id !== productId));
  }

  clear(): void {
    this._lines.set([]);
  }
}
