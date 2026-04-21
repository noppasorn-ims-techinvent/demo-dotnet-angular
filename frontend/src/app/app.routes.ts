import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { rolesGuard } from './core/guards/roles.guard';
import { productResolver } from './core/resolvers/product.resolver';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'shop' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/pages/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/pages/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'shop',
    loadComponent: () => import('./features/shop/pages/product-list.component').then((m) => m.ProductListComponent),
    canActivate: [authGuard],
  },
  {
    path: 'shop/product/:id',
    loadComponent: () => import('./features/shop/pages/product-detail.component').then((m) => m.ProductDetailComponent),
    canActivate: [authGuard],
    resolve: { product: productResolver },
  },
  {
    path: 'seller/stock',
    loadComponent: () =>
      import('./features/seller/pages/seller-stock.component').then((m) => m.SellerStockComponent),
    canActivate: [authGuard, rolesGuard('Seller', 'Admin')],
  },
  {
    path: 'seller/orders',
    loadComponent: () =>
      import('./features/seller/pages/seller-store-orders.component').then((m) => m.SellerStoreOrdersComponent),
    canActivate: [authGuard, rolesGuard('Seller', 'Admin')],
  },
  {
    path: 'seller/products/new',
    loadComponent: () =>
      import('./features/seller/pages/seller-product-form.component').then((m) => m.SellerProductFormComponent),
    canActivate: [authGuard, rolesGuard('Seller', 'Admin')],
  },
  {
    path: 'seller/products/:id/edit',
    loadComponent: () =>
      import('./features/seller/pages/seller-product-form.component').then((m) => m.SellerProductFormComponent),
    canActivate: [authGuard, rolesGuard('Seller', 'Admin')],
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/orders/pages/checkout.component').then((m) => m.CheckoutComponent),
    canActivate: [authGuard, rolesGuard('Buyer')],
  },
  {
    path: 'orders/:id/pay',
    loadComponent: () => import('./features/orders/pages/order-pay.component').then((m) => m.OrderPayComponent),
    canActivate: [authGuard, rolesGuard('Buyer')],
  },
  {
    path: 'orders',
    loadComponent: () => import('./features/orders/pages/orders-list.component').then((m) => m.OrdersListComponent),
    canActivate: [authGuard, rolesGuard('Buyer')],
  },
  {
    path: 'admin/orders',
    loadComponent: () => import('./features/admin/pages/admin-orders.component').then((m) => m.AdminOrdersComponent),
    canActivate: [authGuard, rolesGuard('Admin')],
  },
  { path: '**', redirectTo: 'shop' },
];
