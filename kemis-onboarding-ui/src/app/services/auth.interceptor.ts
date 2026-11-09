
import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  // localStorage is only available in the browser.
  if (typeof localStorage === 'undefined') {
    return next(req);
  }

  const token = localStorage.getItem('kemis_token');

  if (token) {
    const authRequest = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });

    return next(authRequest);
  }

  return next(req);
};
