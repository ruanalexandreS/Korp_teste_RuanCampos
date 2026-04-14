import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormArray } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { RouterModule, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { InvoiceService } from '../../shared/services/invoice.service';
import { ProductService, Product } from '../../shared/services/product.service';

@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    RouterModule,
  ],
  templateUrl: './invoice-form.component.html',
  styleUrl: './invoice-form.component.scss',
})
export class InvoiceFormComponent implements OnInit, OnDestroy {
  products: Product[] = [];
  form;
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private invoiceService: InvoiceService,
    private productService: ProductService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      items: this.fb.array([]),
    });
  }

  ngOnInit() {
    this.productService.getAll().pipe(takeUntil(this.destroy$)).subscribe((data) => (this.products = data));
    this.addItem();
  }

  get items() {
    return this.form.get('items') as FormArray;
  }

  addItem() {
    this.items.push(
      this.fb.group({
        productId: [null, Validators.required],
        quantity: [1, [Validators.required, Validators.min(1)]],
      }),
    );
  }

  removeItem(i: number) {
    this.items.removeAt(i);
  }

  submit() {
    if (this.form.invalid) return;
    const items = this.items.controls.map((c) => {
      const product = this.products.find((p) => p.id === c.value.productId)!;
      return {
        productId: product.id!,
        productCode: product.code,
        productDescription: product.description,
        quantity: c.value.quantity,
      };
    });
    this.invoiceService.create({ items }).pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.router.navigate(['/invoices']);
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
