
import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    // CDE institution details
    path: 'cde-institution-details/:id',
    renderMode: RenderMode.Server
  },
  {
    // SCDE institution details
    path: 'scde-institution-details/:id',
    renderMode: RenderMode.Server
  },
  {
    // QASO institution details
    path: 'qaso-institution-details/:id',
    renderMode: RenderMode.Server
  },
  {
    // Other routes
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
