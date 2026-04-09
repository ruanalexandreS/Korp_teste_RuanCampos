import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { ProductService } from '../../shared/services/product.service';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
})
export class ProductFormComponent {
  form;

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      code: ['', Validators.required],
      description: ['', Validators.required],
      balance: [0, [Validators.required, Validators.min(0)]],
    });
  }

  submit() {
    if (this.form.invalid) return;
    this.productService.create(this.form.value as any).subscribe(() => {
      this.router.navigate(['/products']);
    });
  }
}
