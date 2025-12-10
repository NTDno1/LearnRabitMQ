import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  isLoginMode = true; // true = login, false = register
  username = '';
  email = '';
  password = '';
  displayName = '';
  errorMessage = '';
  isLoading = false;

  constructor(private authService: AuthService) {}

  toggleMode(): void {
    this.isLoginMode = !this.isLoginMode;
    this.errorMessage = '';
    this.clearForm();
  }

  clearForm(): void {
    this.username = '';
    this.email = '';
    this.password = '';
    this.displayName = '';
  }

  async onSubmit(): Promise<void> {
    if (!this.username || !this.password) {
      this.errorMessage = 'Vui lòng điền đầy đủ thông tin';
      return;
    }

    if (!this.isLoginMode && !this.email) {
      this.errorMessage = 'Vui lòng nhập email';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    try {
      if (this.isLoginMode) {
        await this.authService.login({
          username: this.username,
          password: this.password
        }).toPromise();
      } else {
        await this.authService.register({
          username: this.username,
          email: this.email,
          password: this.password,
          displayName: this.displayName || undefined
        }).toPromise();
      }
      
      // Reload page để app.component detect user mới
      window.location.reload();
    } catch (error: any) {
      this.errorMessage = error.error?.message || 'Đăng nhập/Đăng ký thất bại';
    } finally {
      this.isLoading = false;
    }
  }
}

