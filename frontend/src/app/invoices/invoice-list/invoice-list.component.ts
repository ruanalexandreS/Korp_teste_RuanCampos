import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { RouterModule } from '@angular/router';
import { InvoiceService, Invoice } from '../../shared/services/invoice.service';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    RouterModule,
  ],
  templateUrl: './invoice-list.component.html',
  styleUrl: './invoice-list.component.scss',
})
export class InvoiceListComponent implements OnInit {
  invoices: Invoice[] = [];
  displayedColumns = ['number', 'status', 'items', 'actions'];
  loading = false;
  error = '';

  constructor(private invoiceService: InvoiceService) {}

  ngOnInit() {
    this.invoiceService.getAll().subscribe((data) => (this.invoices = data));
  }

  print(id: number) {
    this.loading = true;
    this.error = '';
    this.invoiceService.print(id).subscribe({
      next: (updated) => {
        const index = this.invoices.findIndex((i) => i.id === id);
        this.invoices[index] = updated;
        this.invoices = [...this.invoices];
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error || 'Erro ao imprimir nota.';
        this.loading = false;
      },
    });
  }
}
