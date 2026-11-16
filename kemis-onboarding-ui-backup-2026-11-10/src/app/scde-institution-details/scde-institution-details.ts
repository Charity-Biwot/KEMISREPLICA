
import {
  Component,
  OnInit,
  Inject,
  PLATFORM_ID,
  ChangeDetectorRef
} from '@angular/core';

import {
  CommonModule,
  isPlatformBrowser
} from '@angular/common';

import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import {
  ScdeService,
  InstitutionDetail,
  UserSummary,
  AssignRequest
} from '../services/scde';

@Component({
  selector: 'app-scde-institution-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './scde-institution-details.html',
  styleUrl: './scde-institution-details.scss'
})
export class ScdeInstitutionDetails implements OnInit {

  institution: InstitutionDetail | null = null;
  qasos: UserSummary[] = [];

  selectedQasoId = '';

  loading = true;
  loadingQasos = false;
  assigning = false;

  errorMessage = '';
  successMessage = '';

  private isBrowser: boolean;
  private initialized = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private scdeService: ScdeService,
    private cdr: ChangeDetectorRef,
    @Inject(PLATFORM_ID) platformId: object
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  ngOnInit(): void {

    if (!this.isBrowser || this.initialized) {
      return;
    }

    this.initialized = true;

    const idParam = this.route.snapshot.paramMap.get('id');
    const id = Number(idParam);

    console.log('SCDE Institution Details initialized.');
    console.log('Institution ID:', id);

    const token = localStorage.getItem('kemis_token');

    console.log('SCDE token available:', !!token);

    if (!idParam || !Number.isInteger(id) || id <= 0) {
      this.errorMessage = 'Invalid institution ID.';
      this.loading = false;
      this.cdr.detectChanges();
      return;
    }

    if (!token) {
      this.errorMessage =
        'Your login session could not be found. Please log in again.';

      this.loading = false;
      this.cdr.detectChanges();
      return;
    }

    this.loadInstitution(id);
    this.loadQasos();
  }

  // LOAD INSTITUTION DETAILS
  loadInstitution(id: number): void {

    this.loading = true;
    this.errorMessage = '';

    this.scdeService.getInstitutionDetails(id).subscribe({

      next: (data: InstitutionDetail) => {

        console.log('Institution API response:', data);

        this.institution = data;

        this.loading = false;

        console.log('Institution name:', this.institution.name);
        console.log('Institution received:', !!this.institution);

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error(
          'Institution details request failed:',
          error.status,
          error
        );

        this.loading = false;

        if (error.status === 401) {
          this.errorMessage =
            'Your session is not authorized to view this institution.';
        } else if (error.status === 403) {
          this.errorMessage =
            'You do not have permission to view this institution.';
        } else if (error.status === 404) {
          this.errorMessage =
            'The requested institution could not be found.';
        } else {
          this.errorMessage =
            error.error?.message ||
            'Unable to load institution details.';
        }

        this.cdr.detectChanges();
      }
    });
  }

  // LOAD AVAILABLE QASO OFFICERS
  loadQasos(): void {

    this.loadingQasos = true;

    this.scdeService.getQasos().subscribe({

      next: (data: UserSummary[]) => {

        console.log('QASO officers received:', data);

        this.qasos = data || [];
        this.loadingQasos = false;

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error(
          'QASO loading failed:',
          error.status,
          error
        );

        this.loadingQasos = false;

        if (!this.errorMessage) {
          this.errorMessage =
            error.error?.message ||
            'Unable to load available QASO officers.';
        }

        this.cdr.detectChanges();
      }
    });
  }

  // ASSIGN QASO TO INSTITUTION
  assignQaso(): void {

    if (!this.institution || !this.selectedQasoId) {
      this.errorMessage = 'Please select a QASO officer.';
      return;
    }

    const request: AssignRequest = {
      assigneeUserId: this.selectedQasoId,
      role: 'QASO'
    };

    this.assigning = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.scdeService
      .assignQaso(this.institution.id, request)
      .subscribe({

        next: (data: InstitutionDetail) => {

          this.institution = data;

          this.assigning = false;
          this.selectedQasoId = '';

          this.successMessage =
            'QASO successfully assigned to this institution.';

          console.log('Updated institution:', data);

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QASO assignment failed:',
            error.status,
            error
          );

          this.assigning = false;

          this.errorMessage =
            error.error?.message ||
            'Failed to assign QASO. Please try again.';

          this.cdr.detectChanges();
        }
      });
  }

  // CHECK WHETHER A QASO IS ALREADY ASSIGNED
  get assignedQaso(): boolean {

    return this.institution?.assignments?.some(
      assignment =>
        assignment.role?.toUpperCase() === 'QASO' &&
        assignment.status?.toUpperCase() !== 'CANCELLED'
    ) ?? false;
  }

  // RETURN TO SCDE DASHBOARD
  goBack(): void {
    this.router.navigate(['/scde-dashboard']);
  }

  // LOGOUT
  logout(): void {
    this.router.navigate(['/login']);
  }
}
