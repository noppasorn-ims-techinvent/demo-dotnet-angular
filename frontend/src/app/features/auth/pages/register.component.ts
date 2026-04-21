import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './login.component.css',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly error = signal('');

  protected readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  submit(): void {
    this.error.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.auth.register(v.email, v.password, v.displayName).subscribe({
      next: () => void this.router.navigateByUrl('/shop'),
      error: (err: { status?: number }) => {
        if (err?.status === 409) {
          this.error.set('อีเมลนี้ถูกใช้สมัครแล้ว');
        } else {
          this.error.set('สมัครสมาชิกไม่สำเร็จ');
        }
      },
    });
  }
}
