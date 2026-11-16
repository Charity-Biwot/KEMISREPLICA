
import { Routes } from '@angular/router';

import { Login } from './login/login';
import { InstitutionOnboarding } from './institution-onboarding/institution-onboarding';
import { ScdeReport } from './scde-report/scde-report';
import { ScdeDashboard } from './scde-dashboard/scde-dashboard';
import { ScdeInstitutionDetails } from './scde-institution-details/scde-institution-details';

import { CdeDashboard } from './cde-dashboard/cde-dashboard';
import { CdeInstitutionApproval } from './cde-institution-approval/cde-institution-approval';
import { CdeInstitutionDetails } from './cde-institution-details/cde-institution-details';

import { CdeShell } from './cde-shell/cde-shell';
import { cdeGuard } from './services/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: Login
  },
  {
    path: 'institution-onboarding',
    component: InstitutionOnboarding
  },
  {
    path: 'scde-report',
    component: ScdeReport
  },
  {
    path: 'scde-dashboard',
    component: ScdeDashboard
  },
  {
    path: 'scde-institution-details/:id',
    component: ScdeInstitutionDetails
  },

  // Full CDE dashboard
  {
    path: 'cde-dashboard',
    component: CdeDashboard,
    canActivate: [cdeGuard]
  },

  // Existing CDE shared shell
  {
    path: '',
    component: CdeShell,
    canActivate: [cdeGuard],
    children: [
      {
        path: 'cde-institution-approval',
        component: CdeInstitutionApproval
      },
      {
        path: 'cde-institution-details/:id',
        component: CdeInstitutionDetails
      }
    ]
  }
];
