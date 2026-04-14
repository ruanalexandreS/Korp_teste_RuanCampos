import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AiService {
  constructor(private http: HttpClient) {}

  suggestDescription(code: string): Observable<string> {
    return this.http
      .post<{ suggestion: string }>(
        `${environment.stockServiceUrl}/api/products/suggest-description`,
        { code },
      )
      .pipe(map((res) => res.suggestion));
  }

  summarizeInvoice(invoice: any): Observable<string> {
    return this.http
      .post<{ summary: string }>(
        `${environment.billingServiceUrl}/api/invoices/${invoice.id}/summarize`,
        {},
      )
      .pipe(map((res) => res.summary));
  }

  checkLowStock(products: any[]): Observable<string> {
    return this.http
      .post<{ alert: string }>(
        `${environment.stockServiceUrl}/api/products/check-low-stock`,
        { products },
      )
      .pipe(map((res) => res.alert));
  }
}
