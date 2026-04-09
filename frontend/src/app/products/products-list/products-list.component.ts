import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { RouterModule } from '@angular/router';
import { ProductService, Product } from '../../shared/services/product.service';
import { AiService } from '../../shared/services/ai.services';

@Component({
  selector: 'app-products-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    RouterModule,
  ],
  templateUrl: './products-list.component.html',
  styleUrl: './products-list.component.scss',
})
export class ProductsListComponent implements OnInit {
  products: Product[] = [];
  displayedColumns = ['code', 'description', 'balance'];
  stockAlert = '';

  constructor(
    private productService: ProductService,
    private aiService: AiService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    this.productService.getAll().subscribe({
      next: (data) => {
        this.products = data;
        const alert$ = this.aiService.checkLowStock(data);
        if (alert$) {
          alert$.subscribe({
            next: (alert) => {
              this.stockAlert = alert;
              this.cdr.detectChanges();
            },
          });
        }
        this.cdr.detectChanges();
      },
      error: () => (this.products = []),
    });
  }
}
