import { FormsModule } from '@angular/forms';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

import {
  QasoService,
  QasoInspectionDetail,
  SubmitQasoReportRequest
} from '../../services/qaso';

@Component({
  selector: 'app-qaso-institution-details',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './qaso-institution-details.html',
  styleUrl: './qaso-institution-details.scss'
})
export class QasoInstitutionDetails implements OnInit {

  institutionId = 0;
  institution: QasoInspectionDetail | null = null;

  loading = false;
  saving = false;

  errorMessage = '';
  successMessage = '';

  certificateAuthenticityVerified: boolean | null = null;
  certificateIssuerVerified: boolean | null = null;
  certificateNotExpired: boolean | null = null;
  certificateComplete: boolean | null = null;
  certificateNotes = '';

  scheduledAt = '';
  scheduleNotes = '';

  selectedFile: File | null = null;

  reportSummary = '';
  reportFindings = '';
  recommendation: 'Approve' | 'Reject' = 'Approve';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private qasoService: QasoService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.institutionId = Number(this.route.snapshot.paramMap.get('id'));

    if (!this.institutionId || Number.isNaN(this.institutionId)) {
      this.errorMessage = 'Invalid institution ID.';
      return;
    }

    this.loadInstitution();
  }

  // The API may return enum values as numbers or text.
  private normalizeCertificateStatus(value: unknown): string {
    switch (String(value)) {
      case '0':
      case 'NotReviewed':
        return 'NotReviewed';
      case '1':
      case 'Valid':
        return 'Valid';
      case '2':
      case 'Invalid':
        return 'Invalid';
      case '3':
      case 'Missing':
        return 'Missing';
      default:
        return 'NotReviewed';
    }
  }

  private normalizePhysicalStatus(value: unknown): string {
    switch (String(value)) {
      case '0':
      case 'NotRequired':
        return 'NotRequired';
      case '1':
      case 'Required':
        return 'Required';
      case '2':
      case 'Scheduled':
        return 'Scheduled';
      case '3':
      case 'Completed':
        return 'Completed';
      default:
        return 'Required';
    }
  }

  get certificateStatus(): string {
    return this.normalizeCertificateStatus(
      (this.institution as any)?.certificateStatus
    );
  }

  get physicalInspectionStatus(): string {
    return this.normalizePhysicalStatus(
      (this.institution as any)?.physicalInspectionStatus
    );
  }

  get certificateReviewPending(): boolean {
    return this.certificateStatus === 'NotReviewed';
  }

  get isCertificateMissing(): boolean {
    const item = this.institution as any;

    return !!item && (
      item.hasCertificate === false ||
      item.certificateFileAvailable === false
    );
  }

  get showPhysicalInspection(): boolean {
    return (
      (this.certificateStatus === 'Missing' ||
        this.certificateStatus === 'Invalid') &&
      this.physicalInspectionStatus !== 'NotRequired'
    );
  }

  get evidenceFiles(): any[] {
    const item = this.institution as any;
    return item?.evidenceFiles ?? item?.evidence ?? [];
  }

  get hasSubmittedReport(): boolean {
    return !!(this.institution as any)?.hasSubmittedReport;
  }

  get scheduledDateDisplay(): string {
    const value = (this.institution as any)?.scheduledAt;
    if (!value) return 'Not available';

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString();
  }

  loadInstitution(): void {
    this.loading = true;
    this.errorMessage = '';

    this.qasoService.getInstitutionDetails(this.institutionId).subscribe({
      next: (data) => {
        this.institution = data;

        this.certificateAuthenticityVerified =
          data.certificateAuthenticityVerified ?? null;

        this.certificateIssuerVerified =
          data.certificateIssuerVerified ?? null;

        this.certificateNotExpired =
          data.certificateNotExpired ?? null;

        this.certificateComplete =
          data.certificateComplete ?? null;

        this.certificateNotes = data.certificateReviewNotes ?? '';
        this.scheduleNotes = data.scheduleNotes ?? '';

        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Unable to load QASO institution details:', error);

        this.errorMessage = error.status === 401
          ? 'Your session has expired. Please log in again.'
          : error.status === 403
            ? 'You are not authorized to view this institution.'
            : 'Unable to load institution details. Please try again.';

        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  reviewCertificate(): void {
    if (!this.institution || this.saving || this.hasSubmittedReport) return;

    const missingCertificate = this.isCertificateMissing;

    if (!missingCertificate) {
      if (
        this.certificateAuthenticityVerified === null ||
        this.certificateIssuerVerified === null ||
        this.certificateNotExpired === null ||
        this.certificateComplete === null
      ) {
        this.errorMessage =
          'Please complete all four certificate verification checks.';
        return;
      }

      const allChecksPassed =
        this.certificateAuthenticityVerified &&
        this.certificateIssuerVerified &&
        this.certificateNotExpired &&
        this.certificateComplete;

      if (!allChecksPassed && !this.certificateNotes.trim()) {
        this.errorMessage =
          'Please explain why the certificate failed verification.';
        return;
      }
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const request = missingCertificate
      ? {
          authenticityVerified: null,
          issuerVerified: null,
          notExpired: null,
          complete: null,
          notes: this.certificateNotes.trim() ||
            'Certificate file is missing or unavailable.'
        }
      : {
          authenticityVerified: this.certificateAuthenticityVerified,
          issuerVerified: this.certificateIssuerVerified,
          notExpired: this.certificateNotExpired,
          complete: this.certificateComplete,
          notes: this.certificateNotes.trim() || null
        };

    this.qasoService.reviewCertificate(
      this.institutionId,
      request as any
    ).subscribe({
      next: (data) => {
        this.institution = data;
        this.saving = false;

        this.successMessage = missingCertificate
          ? 'Missing certificate recorded. A physical inspection is required.'
          : this.certificateStatus === 'Valid'
            ? 'Certificate verified successfully. You can now recommend the institution to SCDE.'
            : 'Certificate review saved. A physical inspection is required.';

        this.cdr.detectChanges();
      },
      error: (error) => {
        this.handleError(error, 'Unable to save certificate verification.');
      }
    });
  }

  scheduleInspection(): void {
    if (!this.institution || this.saving || !this.scheduledAt) {
      this.errorMessage = 'Please select a physical inspection date and time.';
      return;
    }

    if (new Date(this.scheduledAt).getTime() <= Date.now()) {
      this.errorMessage = 'The inspection date and time must be in the future.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.qasoService.scheduleInspection(this.institutionId, {
      scheduledAt: new Date(this.scheduledAt).toISOString(),
      notes: this.scheduleNotes.trim() || null
    }).subscribe({
      next: (data) => {
        this.institution = data;
        this.saving = false;
        this.successMessage = 'Physical inspection scheduled successfully.';
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.handleError(error, 'Unable to schedule physical inspection.');
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  uploadEvidence(): void {
    if (!this.selectedFile || this.saving || this.hasSubmittedReport) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.qasoService.uploadEvidence(
      this.institutionId,
      this.selectedFile
    ).subscribe({
      next: (data) => {
        this.institution = data;
        this.selectedFile = null;
        this.saving = false;
        this.successMessage = 'Inspection evidence uploaded successfully.';
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.handleError(error, 'Unable to upload inspection evidence.');
      }
    });
  }

  completeInspection(): void {
    if (!this.institution || this.saving || this.hasSubmittedReport) return;

    if (this.evidenceFiles.length === 0) {
      this.errorMessage =
        'Upload at least one inspection evidence file before completing the inspection.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.qasoService.completePhysicalInspection(
      this.institutionId
    ).subscribe({
      next: (data) => {
        this.institution = data;
        this.saving = false;
        this.successMessage = 'Physical inspection marked as completed.';
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.handleError(error, 'Unable to complete physical inspection.');
      }
    });
  }

  submitReport(): void {
    if (!this.institution || this.saving || this.hasSubmittedReport) return;

    const fastTrack = this.certificateStatus === 'Valid';

    if (!fastTrack) {
      if (this.physicalInspectionStatus !== 'Completed') {
        this.errorMessage =
          'Complete the physical inspection before submitting your report.';
        return;
      }

      if (this.evidenceFiles.length === 0) {
        this.errorMessage =
          'At least one inspection evidence file is required.';
        return;
      }

      if (!this.reportSummary.trim()) {
        this.errorMessage = 'Please enter the inspection report summary.';
        return;
      }
    }

    const summary = this.reportSummary.trim() ||
      'Certificate verified and found valid. Recommended for fast-track processing.';

    const request: SubmitQasoReportRequest = {
      institutionId: this.institutionId,
      summary,
      findings: this.reportFindings.trim(),
      recommendation: this.recommendation
    };

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.qasoService.submitReport(
      this.institutionId,
      request
    ).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage =
          'Recommendation submitted successfully to SCDE.';
        this.loadInstitution();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.handleError(error, 'Unable to submit the report.');
      }
    });
  }

  private handleError(error: any, fallback: string): void {
    console.error(error);

    this.saving = false;
    this.errorMessage =
      error.error?.message ||
      error.error?.title ||
      fallback;

    this.cdr.detectChanges();
  }

  logout(): void {
    this.router.navigate(['/login']);
  }

  goBack(): void {
    this.router.navigate(['/qaso-dashboard']);
  }
}