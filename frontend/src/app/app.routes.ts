import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'products',
    loadComponent: () =>
      import('./products/products-list/products-list.component').then(
        (m) => m.ProductsListComponent,
      ),
  },
  {
    path: 'products/new',
    loadComponent: () =>
      import('./products/product-form/product-form.component').then((m) => m.ProductFormComponent),
  },
  {
    path: 'invoices',
    loadComponent: () =>
      import('./invoices/invoice-list/invoice-list.component').then((m) => m.InvoiceListComponent),
  },
  {
    path: 'invoices/new',
    loadComponent: () =>
      import('./invoices/invoice-form/invoice-form.component').then((m) => m.InvoiceFormComponent),
  },
];
