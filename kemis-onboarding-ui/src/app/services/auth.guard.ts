import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const cdeGuard: CanActivateFn = () => {
  const router = inject(Router);

  // Nothing to check while the page is rendered on a server.
  if (typeof localStorage === 'undefined') {
    return true;
  }

  try {
    const token = localStorage.getItem('kemis_token');
    const user = JSON.parse(localStorage.getItem('kemis_user') ?? 'null');

    if (token && user?.role?.trim().toUpperCase() === 'CDE') {
      return true;
    }
  } catch {
    // Fall through to the sign-in page.
  }

  return router.createUrlTree(['/login']);
};