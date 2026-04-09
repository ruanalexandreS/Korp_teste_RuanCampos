import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  { path: 'products', loadComponent: () => import('./products/products-list/products-list.component').then(m => m.ProductsListComponent) },
  { path: 'invoices', loadComponent: () => import('./invoices/invoice-list/invoice-list.component').then(m => m.InvoiceListComponent) },
];