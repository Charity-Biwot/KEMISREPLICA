import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

import {
  QasoService,
  QasoDashboardItem
} from '../services/qaso';

@Component({
  selector: 'app-qaso-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './qaso-dashboard.component.html',
  styleUrl: './qaso-dashboard.component.scss'
})
export class QasoDashboardComponent implements OnInit {

  institutions: QasoDashboardItem[] = [];

  loading = false;
  errorMessage = '';

  constructor(
    private qasoService: QasoService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadInstitutions();
  }

  loadInstitutions(): void {
    console.log('1. load started');
    this.loading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.qasoService.getAssignedInstitutions().subscribe({
      next: (data: QasoDashboardItem[]) => {
        console.log('2. next fired', data);
        this.institutions = data ?? [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('2. error fired', error);

        if (error.status === 401) {
          this.errorMessage = 'Your session has expired. Please log in again.';
        } else if (error.status === 403) {
          this.errorMessage = 'You are not authorized to access the QASO dashboard.';
        } else {
          this.errorMessage = 'Unable to load assigned institutions. Please try again.';
        }

        this.loading = false;
        this.cdr.detectChanges();
      },
      complete: () => console.log('3. complete')
    });
  }

  get pendingCertificateReviews(): number {
    return this.institutions.filter(
      institution => institution.certificateStatus === 'NotReviewed'
    ).length;
  }

  get physicalInspectionsRequired(): number {
    return this.institutions.filter(
      institution =>
        institution.physicalInspectionStatus === 'Required' ||
        institution.physicalInspectionStatus === 'Scheduled'
    ).length;
  }

  get submittedReports(): number {
    return this.institutions.filter(
      institution => institution.hasSubmittedReport
    ).length;
  }

  viewInstitution(institution: QasoDashboardItem): void {
    this.router.navigate([
      '/qaso-institution-details',
      institution.institutionId
    ]);
  }

  getCertificateStatus(status: string): string {
    switch (status) {
      case 'Valid':
        return 'Valid';
      case 'Invalid':
        return 'Invalid';
      case 'Missing':
        return 'Missing';
      default:
        return 'Not Reviewed';
    }
  }

  getInspectionStatus(status: string): string {
    switch (status) {
      case 'NotRequired':
        return 'Not Required';
      case 'Required':
        return 'Required';
      case 'Scheduled':
        return 'Scheduled';
      case 'Completed':
        return 'Completed';
      default:
        return status || 'Pending';
    }
  }

  logout(): void {
    this.router.navigate(['/login']);
  }
}