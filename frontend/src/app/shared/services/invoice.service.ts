import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface InvoiceItem {
  productId: number;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface Invoice {
  id?: number;
  number?: number;
  status?: string;
  items: InvoiceItem[];
}

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private url = environment.billingServiceUrl + '/invoices';

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<Invoice[]>(this.url);
  }

  create(invoice: Invoice) {
    return this.http.post<Invoice>(this.url, invoice);
  }

  print(id: number) {
    const key = `print-${id}-${Date.now()}`;
    return this.http.post<Invoice>(
      `${this.url}/${id}/print`,
      {},
      {
        headers: { 'Idempotency-Key': key },
      },
    );
  }
}
