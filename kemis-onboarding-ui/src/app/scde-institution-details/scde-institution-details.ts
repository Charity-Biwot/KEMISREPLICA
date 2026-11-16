
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
  AssignRequest,
  SubmitReportRequest,
  ReviewQasoReportRequest,
  Report,
  Comment
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

  // QASO assignment loading
  loading = true;
  loadingQasos = false;
  assigning = false;

  // QASO report review
  qasoReviewComment = '';
  submittingQasoReview = false;

  // SCDE recommendation form
  scdeFindings = '';
  scdeSummary = '';
  scdeRecommendation: 'Approve' | 'Reject' | 'NeedsCorrection' | '' = '';
  submittingRecommendation = false;

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

  // GET THE SUBMITTED QASO INSPECTION REPORT
  get qasoReport(): Report | null {

    return this.institution?.reports?.find(
      report =>
        report.type?.toLowerCase() === 'qasoinspection'
    ) ?? null;
  }

  // CHECK WHETHER QASO HAS SUBMITTED AN INSPECTION REPORT
  get qasoReportSubmitted(): boolean {
    return this.qasoReport !== null;
  }

  // GET COMMENTS ASSOCIATED WITH THE QASO REPORT
  get qasoReviewComments(): Comment[] {

    const report = this.qasoReport;

    if (!report || !this.institution?.comments) {
      return [];
    }

    return this.institution.comments.filter(
      comment => comment.reportId === report.id
    );
  }

  // SUBMIT SCDE REVIEW COMMENT AGAINST THE QASO REPORT
  reviewQasoReport(): void {

    if (!this.institution) {
      this.errorMessage = 'Institution details are not available.';
      return;
    }

    const report = this.qasoReport;

    if (!report) {
      this.errorMessage =
        'There is no submitted QASO inspection report to review.';
      return;
    }

    if (!this.qasoReviewComment.trim()) {
      this.errorMessage =
        'Please enter your review comment before submitting.';
      return;
    }

    const request: ReviewQasoReportRequest = {
      reportId: report.id,
      text: this.qasoReviewComment.trim()
    };

    this.submittingQasoReview = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.scdeService
      .reviewQasoReport(this.institution.id, request)
      .subscribe({

        next: (data: InstitutionDetail) => {

          this.institution = data;

          this.submittingQasoReview = false;
          this.qasoReviewComment = '';

          this.successMessage =
            'Your review comment has been submitted successfully.';

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QASO report review failed:',
            error.status,
            error
          );

          this.submittingQasoReview = false;

          if (error.status === 401) {
            this.errorMessage =
              'Your session has expired. Please log in again.';
          } else if (error.status === 403) {
            this.errorMessage =
              'You are not authorized to review this QASO report.';
          } else if (error.status === 409) {
            this.errorMessage =
              error.error?.message ||
              'The QASO report is not currently available for review.';
          } else {
            this.errorMessage =
              error.error?.message ||
              'Unable to submit your review comment. Please try again.';
          }

          this.cdr.detectChanges();
        }
      });
  }

  // CHECK WHETHER SCDE HAS ALREADY SUBMITTED A RECOMMENDATION
  get scdeRecommendationSubmitted(): boolean {

    return this.institution?.reports?.some(
      report =>
        report.type?.toLowerCase() === 'scderecommendation'
    ) ?? false;
  }

  // CHECK WHETHER SCDE CAN SUBMIT A RECOMMENDATION
  get canSubmitRecommendation(): boolean {

    return !!this.institution &&
      this.qasoReportSubmitted &&
      !this.scdeRecommendationSubmitted;
  }

  // SUBMIT SCDE FINDINGS AND RECOMMENDATION TO CDE
  submitRecommendation(): void {

    if (!this.institution) {
      this.errorMessage = 'Institution details are not available.';
      return;
    }

    if (!this.qasoReportSubmitted) {
      this.errorMessage =
        'The QASO inspection report must be submitted before you can make a recommendation.';
      return;
    }

    if (this.scdeRecommendationSubmitted) {
      this.errorMessage =
        'An SCDE recommendation has already been submitted.';
      return;
    }

    if (!this.scdeSummary.trim()) {
      this.errorMessage = 'Please enter your report summary.';
      return;
    }

    if (!this.scdeFindings.trim()) {
      this.errorMessage = 'Please enter your findings.';
      return;
    }

    if (!this.scdeRecommendation) {
      this.errorMessage = 'Please select a recommendation.';
      return;
    }

    const request: SubmitReportRequest = {
      institutionId: this.institution.id,
      summary: this.scdeSummary.trim(),
      findings: this.scdeFindings.trim(),
      recommendation: this.scdeRecommendation
    };

    this.submittingRecommendation = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.scdeService
      .submitRecommendation(this.institution.id, request)
      .subscribe({

        next: (report) => {

          console.log('SCDE recommendation submitted:', report);

          this.submittingRecommendation = false;

          this.successMessage =
            'Your recommendation has been submitted successfully to the CDE.';

          this.scdeSummary = '';
          this.scdeFindings = '';
          this.scdeRecommendation = '';

          this.loadInstitution(this.institution!.id);

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'SCDE recommendation submission failed:',
            error.status,
            error
          );

          this.submittingRecommendation = false;

          if (error.status === 401) {
            this.errorMessage =
              'Your session has expired. Please log in again.';
          } else if (error.status === 403) {
            this.errorMessage =
              'You are not authorized to submit this recommendation.';
          } else {
            this.errorMessage =
              error.error?.message ||
              'Failed to submit your recommendation. Please try again.';
          }

          this.cdr.detectChanges();
        }
      });
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
