import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const rolesGuard =
  (...allowed: string[]): CanActivateFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const ok = allowed.some((r) => auth.hasRole(r));
    return ok ? true : router.parseUrl('/shop');
  };
