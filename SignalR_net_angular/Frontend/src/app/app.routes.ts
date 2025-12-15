import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { ItemsPageComponent } from './components/Items/items-page.component';
import { ChatComponent } from './components/Chat/chat.component';
import { LoginComponent } from './components/Auth/login.component';
import { AuthService } from './services/auth.service';

const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/auth']);
};

export const routes: Routes = [
  { path: 'items', component: ItemsPageComponent }, // view CRUD mới
  { path: 'chat', component: ChatComponent },       // view chat cũ
  { path: 'auth', component: LoginComponent },       // view chat cũ
  { path: '', redirectTo: 'items', pathMatch: 'full' }
];