import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { InvoiceService, Invoice } from '../../shared/services/invoice.service';
import { AiService } from '../../shared/services/ai.services';

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
export class InvoiceListComponent implements OnInit, OnDestroy {
  invoices: Invoice[] = [];
  displayedColumns = ['number', 'status', 'items', 'actions'];
  loadingMap: Record<number, boolean> = {};
  error = '';
  aiSummary = '';
  alertMessage: string | null = null;
  alertType: 'success' | 'error' = 'success';
  private destroy$ = new Subject<void>();

  constructor(
    private invoiceService: InvoiceService,
    private aiService: AiService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    this.invoiceService.getAll().pipe(takeUntil(this.destroy$)).subscribe({
      next: (data) => {
        this.invoices = data;
        this.cdr.detectChanges();
      },
      error: () => (this.invoices = []),
    });
  }

  print(id: number) {
    this.loadingMap[id] = true;
    this.error = '';
    this.aiSummary = '';
    this.invoiceService.print(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (updated) => {
        const index = this.invoices.findIndex((i) => i.id === id);
        this.invoices[index] = updated;
        this.invoices = [...this.invoices];
        this.loadingMap[id] = false;
        this.showAlert('Nota fiscal fechada com sucesso!', 'success');
        this.aiService.summarizeInvoice(updated).pipe(takeUntil(this.destroy$)).subscribe({
          next: (summary) => {
            this.aiSummary = summary;
            this.cdr.detectChanges();
          },
        });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = err.error || 'Erro ao imprimir nota.';
        this.loadingMap[id] = false;
        this.cdr.detectChanges();
      },
    });
  }

  showAlert(message: string, type: 'success' | 'error') {
    this.alertMessage = message;
    this.alertType = type;
    setTimeout(() => {
      this.alertMessage = null;
      this.cdr.detectChanges();
    }, 5000);
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
