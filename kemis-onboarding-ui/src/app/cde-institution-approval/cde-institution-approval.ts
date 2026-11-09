import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import {
  CdeService,
  CdeAssignRequest
} from '../services/cde';

import {
  PendingInstitution,
  UserSummary
} from '../services/scde';

type ApprovalRow = PendingInstitution & { status?: string };
type OfficerRole = 'SCDE' | 'CQASO';

@Component({
  selector: 'app-cde-institution-approval',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cde-institution-approval.html',
  styleUrl: './cde-institution-approval.scss'
})
export class CdeInstitutionApproval implements OnInit {

  institutions: ApprovalRow[] = [];

  scdeOfficers: UserSummary[] = [];
  cqasoOfficers: UserSummary[] = [];

  searchTerm = '';
  selectedSubCounty = 'All';
  selectedStatus = 'Pending';
  onboardingType = 'New Onboarding';

  loading = false;
  error = '';
  successMessage = '';

  // Assign popup state
  popupOpen = false;
  popupRole: OfficerRole = 'SCDE';
  popupInstitution: ApprovalRow | null = null;
  popupOfficerId = '';
  popupError = '';
  saving = false;

  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private cdeService: CdeService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPendingInstitutions();
    this.loadOfficers();
  }

  loadPendingInstitutions(): void {
    this.loading = true;
    this.error = '';

    this.cdeService.getPending().subscribe({
      next: (data) => {
        // TEMPORARY: shows what the API really returns. Delete this line when done.
        console.log('institutions response:', data);

        this.institutions = Array.isArray(data) ? (data as ApprovalRow[]) : [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Unable to load pending institutions:', error);

        this.institutions = [];
        this.loading = false;

        if (error.status === 401) {
          this.error = 'Your session has expired. Please sign in again.';
        } else if (error.status === 403) {
          this.error = 'Your account does not have CDE access.';
        } else {
          this.error = 'Unable to load institution applications.';
        }

        this.cdr.detectChanges();
      }
    });
  }

  loadOfficers(): void {
    this.cdeService.getAssignees('SCDE').subscribe({
      next: (data) => {
        this.scdeOfficers = Array.isArray(data) ? data : [];
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Unable to load SCDE officers:', error);
      }
    });

    this.cdeService.getAssignees('CQASO').subscribe({
      next: (data) => {
        this.cqasoOfficers = Array.isArray(data) ? data : [];
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Unable to load CQASO officers:', error);
      }
    });
  }

  get subCounties(): string[] {
    const values = this.institutions
      .map(institution => institution.subCounty)
      .filter((value): value is string => !!value);

    return ['All', ...Array.from(new Set(values)).sort()];
  }

  statusOf(institution: ApprovalRow): string {
    const row = institution as unknown as Record<string, unknown>;

    // 1. A text status under any of the usual field names
    const candidates = [
      row['status'],
      row['approvalStatus'],
      row['decisionStatus'],
      row['applicationStatus'],
      row['state']
    ];

    const text = candidates.find(
      value => typeof value === 'string' && value.trim() !== ''
    ) as string | undefined;

    if (text) {
      const raw = text.trim().toLowerCase();

      if (raw.startsWith('approv')) {
        return 'Approved';
      }

      if (raw.startsWith('reject') || raw.startsWith('declin')) {
        return 'Rejected';
      }

      return 'Pending';
    }

    // 2. True/false flags
    if (row['isApproved'] === true) {
      return 'Approved';
    }

    if (row['isRejected'] === true) {
      return 'Rejected';
    }

    // 3. No status information at all: treat as pending
    return 'Pending';
  }

  get filteredInstitutions(): ApprovalRow[] {
    const search = this.searchTerm.trim().toLowerCase();

    return this.institutions.filter(institution => {
      const matchesSearch =
        !search ||
        institution.name.toLowerCase().includes(search) ||
        institution.uic.toLowerCase().includes(search);

      const matchesSubCounty =
        this.selectedSubCounty === 'All' ||
        institution.subCounty === this.selectedSubCounty;

      const matchesStatus =
        this.statusOf(institution) === this.selectedStatus;

      return matchesSearch && matchesSubCounty && matchesStatus;
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

  // ---------- Success toast ----------

  showSuccess(message: string): void {
    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
    }

    // Clear first so the toast (and its progress line) restarts
    this.successMessage = '';

    setTimeout(() => {
      this.successMessage = message;
      this.cdr.detectChanges();

      this.toastTimer = setTimeout(() => {
        this.successMessage = '';
        this.cdr.detectChanges();
      }, 4000);
    }, 0);
  }

  dismissToast(): void {
    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
    }

    this.successMessage = '';
  }

  // ---------- Assign popup ----------

  get popupOfficers(): UserSummary[] {
    return this.popupRole === 'SCDE' ? this.scdeOfficers : this.cqasoOfficers;
  }

  openAssign(institution: ApprovalRow, role: OfficerRole): void {
    this.popupInstitution = institution;
    this.popupRole = role;
    this.popupOfficerId = '';
    this.popupError = '';
    this.error = '';
    this.dismissToast();
    this.popupOpen = true;
  }

  closePopup(): void {
    if (this.saving) {
      return;
    }

    this.popupOpen = false;
    this.popupInstitution = null;
    this.popupOfficerId = '';
    this.popupError = '';
  }

  submitAssign(): void {
    if (!this.popupInstitution || this.saving) {
      return;
    }

    if (!this.popupOfficerId) {
      this.popupError = `Please select a ${this.popupRole} officer.`;
      return;
    }

    const institution = this.popupInstitution;
    const role = this.popupRole;

    const request: CdeAssignRequest = {
      assigneeUserId: this.popupOfficerId,
      role
    };

    this.saving = true;
    this.popupError = '';

    this.cdeService.assign(institution.id, request).pipe(
      finalize(() => {
        this.saving = false;
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: () => {
        this.saving = false;
        this.closePopup();
        this.showSuccess('Assigned Successfully!');
        this.loadPendingInstitutions();
      },
      error: (error) => {
        console.error(`Unable to assign ${role}:`, error);
        this.popupError =
          error?.error?.message ?? `Unable to assign ${role}. Please try again.`;
        this.cdr.detectChanges();
      }
    });
  }

  viewDetails(id: number): void {
    this.router.navigate(['/cde-institution-details', id]);
  }

  trackById(index: number, institution: ApprovalRow): number {
    return institution.id;
  }
}