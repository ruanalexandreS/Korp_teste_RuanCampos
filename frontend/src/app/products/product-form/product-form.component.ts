import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterModule } from '@angular/router';
import { ProductService } from '../../shared/services/product.service';
import { AiService } from '../../shared/services/ai.services';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatCardModule,
    RouterModule,
  ],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
})
export class ProductFormComponent {
  form;
  loadingAi = false;

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private aiService: AiService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      code: ['', Validators.required],
      description: ['', Validators.required],
      balance: [0, [Validators.required, Validators.min(0)]],
    });
  }

  suggestDescription() {
    const code = this.form.get('code')?.value;
    if (!code) return;
    this.loadingAi = true;
    this.aiService.suggestDescription(code).subscribe({
      next: (description) => {
        this.form.patchValue({ description });
        this.loadingAi = false;
      },
      error: () => (this.loadingAi = false),
    });
  }

  submit() {
    if (this.form.invalid) return;
    const value = {
      ...this.form.value,
      balance: parseInt(this.form.value.balance as any, 10),
    };
    this.productService.create(value as any).subscribe(() => {
      this.router.navigate(['/products']);
    });
  }
}
