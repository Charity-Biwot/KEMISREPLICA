import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

import { CdeService, DashboardSummary } from '../services/cde';
import { PendingInstitution } from '../services/scde';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-cde-dashboard',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './cde-dashboard.scss',
  templateUrl: './cde-dashboard.html',
})
export class CdeDashboard implements OnInit {
  pending: PendingInstitution[] = [];
  summary: DashboardSummary | null = null;

  loading = true;
  error = '';
  county = '';

  isUserMenuOpen = false;

  constructor(
    private cdeService: CdeService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    if (typeof window === 'undefined') {
      this.loading = false;
      return;
    }

    this.loadUserDetails();
    this.loadDashboard();
  }

  loadUserDetails(): void {
    if (typeof localStorage === 'undefined') return;

    const storedUser = localStorage.getItem('kemis_user');
    if (!storedUser) return;

    try {
      const user = JSON.parse(storedUser);
      this.county = user.county || '';
    } catch (error) {
      console.error('Unable to read user details:', error);
    }
  }

  loadDashboard(): void {
    if (typeof window === 'undefined') return;

    this.loading = true;
    this.error = '';
    this.pending = [];

    this.cdeService.getDashboard().subscribe({
      next: data => {
        this.summary = data;
        console.log('Dashboard statistics received:', data);
        this.cdr.markForCheck();
      },
      error: error => {
        console.error('Error loading dashboard statistics:', error);
        this.summary = null;
        this.cdr.markForCheck();
      }
    });

    this.cdeService.getPending().subscribe({
      next: data => {
        console.log('Pending institutions received:', data);
        this.pending = Array.isArray(data) ? data : [];
        console.log('Pending institutions count:', this.pending.length);
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: error => {
        console.error('Error loading pending institutions:', error);
        this.pending = [];
        this.loading = false;

        if (error.status === 401) {
          this.error = 'Your session has expired. Please sign in again.';
        } else if (error.status === 403) {
          this.error = 'Your account does not have CDE access.';
        } else {
          this.error = 'Unable to load pending applications.';
        }

        this.cdr.markForCheck();
      }
    });
  }

  get approvedText(): string {
    return this.summary ? String(this.summary.approved) : '—';
  }

  get rejectedText(): string {
    return this.summary ? String(this.summary.rejected) : '—';
  }

  get pendingCount(): number {
    return this.pending.length;
  }

  barWidth(value: number | null | undefined): string {
    const approved = this.summary?.approved ?? 0;
    const rejected = this.summary?.rejected ?? 0;
    const pending = this.summary?.pending ?? this.pending.length;
    const total = approved + rejected + pending;

    if (!total || value == null || value <= 0) {
      return '0%';
    }

    const percentage = (value / total) * 100;
    return `${Math.min(percentage, 100)}%`;
  }

  statusText(institution: PendingInstitution): string {
    if (institution.canDecide) {
      return 'Ready for CDE Decision';
    }

    if (institution.scdeAssigned && institution.cqasoAssigned) {
      return 'Officers Assigned';
    }

    if (institution.scdeAssigned) {
      return 'SCDE Assigned';
    }

    if (institution.cqasoAssigned) {
      return 'CQASO Assigned';
    }

    return 'Pending Review';
  }

  open(id: number): void {
    this.router.navigate(['/cde-institution-details', id]);
  }

  openPendingApplications(): void {
    this.router.navigate(['/cde-institution-approval']);
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  // Closes the caret dropdown when the user clicks anywhere else on the page
  @HostListener('document:click')
  closeUserMenu(): void {
    this.isUserMenuOpen = false;
  }

  goToDashboard(): void {
    this.isUserMenuOpen = false;
    this.router.navigate(['/cde-dashboard']);
  }

  changePassword(): void {
    this.isUserMenuOpen = false;
    this.router.navigate(['/change-password']);
  }

  logout(): void {
    this.isUserMenuOpen = false;

    if (typeof window === 'undefined') return;

    const confirmed = window.confirm('Do you want to log out?');

    if (confirmed) {
      this.authService.logout();
      this.router.navigate(['/login']);
    }
  }

  signInAgain(): void {
    this.router.navigate(['/login']);
  }
}