import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface Product {
  id?: number;
  code: string;
  description: string;
  balance: number;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private url = environment.stockServiceUrl + '/products';

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<Product[]>(this.url);
  }

  create(product: Product) {
    return this.http.post<Product>(this.url, product);
  }
}