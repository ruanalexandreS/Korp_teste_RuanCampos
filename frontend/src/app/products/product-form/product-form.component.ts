import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
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
export class ProductFormComponent implements OnDestroy {
  form;
  loadingAi = false;
  duplicateCodeError = '';
  alertMessage: string | null = null;
  alertType: 'error' | 'success' = 'error';
  private destroy$ = new Subject<void>();

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

    this.form.get('code')?.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      if (this.duplicateCodeError) {
        this.duplicateCodeError = '';
        this.form.get('code')?.setErrors(null);
      }
    });
  }

  suggestDescription() {
    const code = this.form.get('code')?.value;
    if (!code) return;
    this.loadingAi = true;
    this.aiService.suggestDescription(code).pipe(takeUntil(this.destroy$)).subscribe({
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
    this.productService.create(value as any).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.showAlert('Produto salvo com sucesso!', 'success');
        this.router.navigate(['/products']);
      },
      error: (err) => {
        if (err.status === 409) {
          this.duplicateCodeError = err.error?.error || 'Código de produto duplicado.';
          this.form.get('code')?.setErrors({ duplicate: true });
          this.showAlert(this.duplicateCodeError, 'error');
        }
      },
    });
  }

  showAlert(message: string, type: 'error' | 'success') {
    this.alertMessage = message;
    this.alertType = type;
    setTimeout(() => (this.alertMessage = null), 5000);
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
