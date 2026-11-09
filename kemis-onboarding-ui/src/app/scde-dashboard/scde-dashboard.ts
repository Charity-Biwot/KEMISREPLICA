
import {
  Component,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import {
  ScdeService,
  PendingInstitution,
  InstitutionDetail,
  UserSummary
} from '../services/scde';

import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-scde-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './scde-dashboard.scss',
  templateUrl: './scde-dashboard.html',
})
export class ScdeDashboard implements OnInit {

  institutions: PendingInstitution[] = [];
  selectedInstitution: InstitutionDetail | null = null;
  qasos: UserSummary[] = [];

  selectedQasoId = '';

  loading = false;
  loadingDetails = false;
  assigning = false;

  message = '';
  errorMessage = '';

  userName = 'SCDE User';
  county = '';

  constructor(
    private scdeService: ScdeService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {

    if (typeof window === 'undefined') {
      return;
    }

    const storedUser = localStorage.getItem('kemis_user');

    if (storedUser) {
      try {
        const user = JSON.parse(storedUser);

        this.userName = user.fullName || 'SCDE User';
        this.county = user.county || '';

      } catch (error) {
        console.error(
          'Unable to read logged-in user details:',
          error
        );
      }
    }

    this.loadInstitutions();
  }

  loadInstitutions(): void {

    this.loading = true;
    this.errorMessage = '';
    this.message = '';

    this.cdr.detectChanges();

    this.scdeService.getInstitutions().subscribe({

      next: (data) => {

        console.log('SCDE institutions received:', data);
        console.log('Number of institutions:', data.length);

        this.institutions = data;
        this.loading = false;

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error(
          'Error loading SCDE institutions:',
          error
        );

        this.errorMessage = error.status === 403
          ? 'Your account does not have SCDE access.'
          : error.status === 401
            ? 'Your session has expired. Please sign in again.'
            : 'Unable to load assigned institutions. Please try again.';

        this.loading = false;

        this.cdr.detectChanges();
      }

    });
  }

  viewDetails(institution: PendingInstitution): void {

    this.loadingDetails = true;
    this.errorMessage = '';
    this.message = '';

    this.selectedInstitution = null;
    this.selectedQasoId = '';
    this.qasos = [];

    this.cdr.detectChanges();

    this.scdeService
      .getInstitutionDetails(institution.id)
      .subscribe({

        next: (data) => {

          this.selectedInstitution = data;
          this.loadingDetails = false;

          this.loadQasos();

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'Error loading institution details:',
            error
          );

          this.errorMessage =
            'Unable to load institution details.';

          this.loadingDetails = false;

          this.cdr.detectChanges();
        }

      });
  }

  loadQasos(): void {

    this.scdeService.getQasos().subscribe({

      next: (data) => {

        this.qasos = data;

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error('Error loading QASOs:', error);

        this.errorMessage =
          'Unable to load available QASOs.';

        this.cdr.detectChanges();
      }

    });
  }

  assignQaso(): void {

    if (!this.selectedInstitution || !this.selectedQasoId) {

      this.errorMessage = 'Please select a QASO.';

      this.cdr.detectChanges();

      return;
    }

    this.assigning = true;
    this.errorMessage = '';
    this.message = '';

    this.cdr.detectChanges();

    this.scdeService.assignQaso(
      this.selectedInstitution.id,
      {
        assigneeUserId: this.selectedQasoId,
        role: 'QASO'
      }
    ).subscribe({

      next: (data) => {

        this.selectedInstitution = data;
        this.selectedQasoId = '';
        this.assigning = false;

        this.message = 'QASO assigned successfully.';

        this.loadInstitutions();

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error('Error assigning QASO:', error);

        this.errorMessage =
          error.error?.message || 'Unable to assign QASO.';

        this.assigning = false;

        this.cdr.detectChanges();
      }

    });
  }

  closeDetails(): void {

    this.selectedInstitution = null;
    this.selectedQasoId = '';

    this.message = '';
    this.errorMessage = '';

    this.cdr.detectChanges();
  }

  logout(): void {

    this.authService.logout();

    this.router.navigate(['/login']);
  }

}
