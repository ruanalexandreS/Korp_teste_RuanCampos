import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AiService {
  private url = 'https://api.groq.com/openai/v1/chat/completions';
  private apiKey = import.meta.env['NG_APP_GROQ_API_KEY'];

  constructor(private http: HttpClient) {
    console.log('API KEY:', import.meta.env['NG_APP_GROQ_API_KEY']);
  }

  suggestDescription(code: string) {
    return this.http
      .post<any>(
        this.url,
        {
          model: 'llama-3.3-70b-versatile',
          messages: [
            {
              role: 'user',
              content: `Você é um assistente de cadastro de produtos. 
          Com base no código do produto "${code}", sugira uma descrição curta e objetiva em português.
          Responda apenas com a descrição, sem explicações.`,
            },
          ],
          max_tokens: 100,
        },
        {
          headers: {
            Authorization: `Bearer ${this.apiKey}`,
            'Content-Type': 'application/json',
          },
        },
      )
      .pipe(map((res) => res.choices[0].message.content.trim()));
  }

  summarizeInvoice(invoice: any) {
    const itemsText = invoice.items
      .map((i: any) => `${i.productDescription} (x${i.quantity})`)
      .join(', ');

    return this.http
      .post<any>(
        this.url,
        {
          model: 'llama-3.3-70b-versatile',
          messages: [
            {
              role: 'user',
              content: `Gere um resumo curto e profissional em português para a seguinte nota fiscal:
        Número: NF-${invoice.number}
        Produtos: ${itemsText}
        Status: ${invoice.status}
        Responda apenas com o resumo, sem títulos ou explicações.`,
            },
          ],
          max_tokens: 150,
        },
        {
          headers: {
            Authorization: `Bearer ${this.apiKey}`,
            'Content-Type': 'application/json',
          },
        },
      )
      .pipe(map((res) => res.choices[0].message.content.trim()));
  }

  checkLowStock(products: any[]) {
    const lowStock = products.filter((p) => p.balance <= 5);
    if (lowStock.length === 0) return null;

    const productsText = lowStock.map((p) => `${p.description} (saldo: ${p.balance})`).join(', ');

    return this.http
      .post<any>(
        this.url,
        {
          model: 'llama-3.3-70b-versatile',
          messages: [
            {
              role: 'user',
              content: `Você é um assistente de controle de estoque. Os seguintes produtos estão com saldo baixo (5 ou menos unidades): ${productsText}. 
        Gere um alerta curto e direto em português recomendando reposição. Máximo 2 frases.`,
            },
          ],
          max_tokens: 100,
        },
        {
          headers: {
            Authorization: `Bearer ${this.apiKey}`,
            'Content-Type': 'application/json',
          },
        },
      )
      .pipe(map((res) => res.choices[0].message.content.trim()));
  }
}
