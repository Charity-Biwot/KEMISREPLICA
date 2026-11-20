
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

function checkRoles(allowedRoles: string[]) {
  const router = inject(Router);

  // Preserve the existing server-rendering behaviour.
  if (typeof localStorage === 'undefined') {
    return true;
  }

  try {
    const token = localStorage.getItem('kemis_token');
    const user = JSON.parse(
      localStorage.getItem('kemis_user') ?? 'null'
    );

    const userRole = user?.role?.trim().toUpperCase();

    if (token && allowedRoles.includes(userRole)) {
      return true;
    }
  } catch {
    // Invalid stored user data is treated as unauthenticated.
  }

  return router.createUrlTree(['/login']);
}

// Keep this guard for existing CDE-only routes.
export const cdeGuard: CanActivateFn = () => {
  return checkRoles(['CDE']);
};

// Use this guard for the shared shell and role-specific routes.
// Set allowed roles in the route's data property.
export const roleGuard: CanActivateFn = (route) => {
  const allowedRoles = route.data['roles'] as string[] | undefined;

  return checkRoles(
    (allowedRoles ?? []).map(role => role.trim().toUpperCase())
  );
};
