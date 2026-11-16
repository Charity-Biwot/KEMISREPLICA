
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  email = '';
  password = '';
  message = '';
  isSubmitting = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  submitLogin(): void {
    if (this.isSubmitting) {
      return;
    }

    this.message = '';

    if (!this.email.trim() || !this.password) {
      this.message = 'Please enter your email and password.';
      return;
    }

    this.isSubmitting = true;

    this.authService.login(this.email.trim(), this.password).subscribe({
      next: (response) => {
        const role = response.role?.trim().toUpperCase();

        if (role === 'SCDE') {
          this.router.navigate(['/scde-dashboard']);
        } else if (role === 'CDE') {
          this.router.navigate(['/cde-dashboard']);
        } else {
          this.authService.logout();
          this.message = 'Your account does not have dashboard access.';
        }

        this.isSubmitting = false;
      },

      error: (error) => {
        console.error('Login error:', error);
        this.message = 'Login failed. Check your email and password.';
        this.isSubmitting = false;
      }
    });
  }
}
