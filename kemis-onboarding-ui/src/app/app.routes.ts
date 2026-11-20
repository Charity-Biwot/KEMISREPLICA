
import { Routes } from '@angular/router';

import { Login } from './login/login';
import { InstitutionOnboarding } from './institution-onboarding/institution-onboarding';

import { ScdeReport } from './scde-report/scde-report';
import { ScdeDashboard } from './scde-dashboard/scde-dashboard';
import { ScdeInstitutionDetails } from './scde-institution-details/scde-institution-details';

import { CdeDashboard } from './cde-dashboard/cde-dashboard';
import { CdeInstitutionApproval } from './cde-institution-approval/cde-institution-approval';
import { CdeInstitutionDetails } from './cde-institution-details/cde-institution-details';
import { CdeApprovedInstitutions } from './cde-approved-institutions/cde-approved-institutions';
import { CdeRejectedInstitutions } from './cde-rejected-institutions/cde-rejected-institutions';

import { CdeShell } from './cde-shell/cde-shell';

import { QasoDashboardComponent } from './qaso-dashboard/qaso-dashboard.component';
import { QasoInstitutionDetails } from './components/qaso-institution-details/qaso-institution-details';

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

  // Shared shell for all officer dashboards and pages
  {
    path: '',
    component: CdeShell,
    children: [

      // CDE pages
      {
        path: 'cde-dashboard',
        component: CdeDashboard,
        canActivate: [cdeGuard]
      },
      {
        path: 'cde-institution-approval',
        component: CdeInstitutionApproval,
        canActivate: [cdeGuard]
      },
      {
        path: 'cde-institution-details/:id',
        component: CdeInstitutionDetails,
        canActivate: [cdeGuard]
      },
      {
        path: 'cde-approved-institutions',
        component: CdeApprovedInstitutions,
        canActivate: [cdeGuard]
      },
      {
        path: 'cde-rejected-institutions',
        component: CdeRejectedInstitutions,
        canActivate: [cdeGuard]
      },

      // SCDE pages - existing components and paths
      {
        path: 'scde-dashboard',
        component: ScdeDashboard
      },
      {
        path: 'scde-institution-details/:id',
        component: ScdeInstitutionDetails
      },
      {
        path: 'scde-report',
        component: ScdeReport
      },

      // QASO pages - existing components and paths
      {
        path: 'qaso-dashboard',
        component: QasoDashboardComponent
      },
      {
        path: 'qaso-institution-details/:id',
        component: QasoInstitutionDetails
      }
    ]
  }
];
