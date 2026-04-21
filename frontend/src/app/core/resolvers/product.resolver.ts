import { inject } from '@angular/core';
import { ResolveFn, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import type { ProductDto } from '../models/api.types';
import { ProductService } from '../services/product.service';

export const productResolver: ResolveFn<ProductDto | null> = (route) => {
  const products = inject(ProductService);
  const router = inject(Router);
  const id = Number(route.paramMap.get('id'));
  if (!Number.isFinite(id)) {
    void router.navigateByUrl('/shop');
    return of(null);
  }

  return products.getById(id).pipe(
    catchError(() => {
      void router.navigateByUrl('/shop');
      return of(null);
    }),
  );
};
