
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

import {
  CdeService,
  CdeInstitutionList
} from '../services/cde';

@Component({
  selector: 'app-cde-rejected-institutions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cde-rejected-institutions.html',
  styleUrl: './cde-rejected-institutions.scss'
})
export class CdeRejectedInstitutions implements OnInit {

  institutions: CdeInstitutionList[] = [];

  loading = false;
  error = '';

  constructor(
    private cdeService: CdeService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadRejectedInstitutions();
  }

  loadRejectedInstitutions(): void {
    this.loading = true;
    this.error = '';

    this.cdeService.getRejected().subscribe({
      next: (data) => {
        this.institutions = Array.isArray(data) ? data : [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Unable to load rejected institutions:', error);

        this.institutions = [];
        this.loading = false;

        if (error.status === 401) {
          this.error = 'Your session has expired. Please sign in again.';
        } else if (error.status === 403) {
          this.error = 'Your account does not have CDE access.';
        } else {
          this.error = 'Unable to load rejected institutions.';
        }

        this.cdr.detectChanges();
      }
    });
  }

  formatDate(date: string | null): string {
    if (!date) {
      return '—';
    }

    const parsedDate = new Date(date);

    if (isNaN(parsedDate.getTime())) {
      return '—';
    }

    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    }).format(parsedDate);
  }

  viewDetails(id: number): void {
    this.router.navigate(['/cde-institution-details', id]);
  }

  goBack(): void {
    this.router.navigate(['/cde-dashboard']);
  }

  trackById(index: number, institution: CdeInstitutionList): number {
    return institution.id;
  }
}
