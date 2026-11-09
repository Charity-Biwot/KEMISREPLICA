import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import {
  CdeService,
  CdeAssignRequest,
  CommentRequest,
  DecisionRequest
} from '../services/cde';

import {
  InstitutionDetail,
  UserSummary
} from '../services/scde';

@Component({
  selector: 'app-cde-institution-details',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './cde-institution-details.scss',
  templateUrl: './cde-institution-details.html',
})
export class CdeInstitutionDetails implements OnInit {

  institution: InstitutionDetail | null = null;

  assignees: UserSummary[] = [];

  loading = true;
  saving = false;
  error = '';
  success = '';

  selectedRole: 'SCDE' | 'CQASO' = 'SCDE';
  selectedAssigneeId = '';

  commentText = '';
  decisionReason = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cdeService: CdeService,
    private cdr: ChangeDetectorRef
  ) {}

  async ngOnInit(): Promise<void> {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!Number.isInteger(id) || id <= 0) {
      this.error = 'Invalid institution ID.';
      this.loading = false;
      this.cdr.markForCheck();
      return;
    }

    await this.loadInstitution(id);
    await this.loadAssignees();
  }

  // Load institution details
  async loadInstitution(id: number): Promise<void> {
    this.loading = true;
    this.error = '';
    this.cdr.markForCheck();

    try {
      this.institution = await firstValueFrom(
        this.cdeService.getDetails(id)
      );

    } catch (e: any) {
      console.error('Error loading institution details:', e);

      this.error = e?.status === 401
        ? 'Your session has expired. Please sign in again.'
        : e?.status === 403
          ? 'You do not have permission to view this institution.'
          : 'Unable to load institution details. Please try again.';

    } finally {
      this.loading = false;
      this.cdr.markForCheck();
    }
  }

  // Load available officers
  async loadAssignees(): Promise<void> {
    try {
      this.assignees = await firstValueFrom(
        this.cdeService.getAssignees(this.selectedRole)
      );

    } catch (error) {
      console.error('Error loading officers:', error);
      this.assignees = [];

    } finally {
      this.cdr.markForCheck();
    }
  }

  // Change officer role
  async changeRole(): Promise<void> {
    this.selectedAssigneeId = '';
    await this.loadAssignees();
    this.cdr.markForCheck();
  }

  // Assign an officer
  async assignOfficer(): Promise<void> {
    if (!this.institution || !this.selectedAssigneeId) {
      this.error = 'Please select an officer first.';
      this.cdr.markForCheck();
      return;
    }

    const request: CdeAssignRequest = {
      assigneeUserId: this.selectedAssigneeId,
      role: this.selectedRole
    };

    this.saving = true;
    this.error = '';
    this.success = '';
    this.cdr.markForCheck();

    try {
      this.institution = await firstValueFrom(
        this.cdeService.assign(this.institution.id, request)
      );

      this.success = `${this.selectedRole} assigned successfully.`;
      this.selectedAssigneeId = '';

      await this.loadAssignees();

    } catch (e: any) {
      this.error = e?.error?.message || 'Unable to assign the officer.';

    } finally {
      this.saving = false;
      this.cdr.markForCheck();
    }
  }

  // Add review comment
  async addComment(): Promise<void> {
    if (!this.institution || !this.commentText.trim()) {
      this.error = 'Please enter a comment.';
      this.cdr.markForCheck();
      return;
    }

    const request: CommentRequest = {
      text: this.commentText.trim(),
      reportId: null
    };

    this.saving = true;
    this.error = '';
    this.success = '';
    this.cdr.markForCheck();

    try {
      this.institution = await firstValueFrom(
        this.cdeService.addComment(this.institution.id, request)
      );

      this.commentText = '';
      this.success = 'Comment added successfully.';

    } catch (e: any) {
      this.error = e?.error?.message || 'Unable to add the comment.';

    } finally {
      this.saving = false;
      this.cdr.markForCheck();
    }
  }

  // Approve or reject institution
  async makeDecision(approve: boolean): Promise<void> {
    if (!this.institution) {
      return;
    }

    if (!this.institution.canDecide) {
      this.error = 'This institution is not ready for a CDE decision.';
      this.cdr.markForCheck();
      return;
    }

    // A rejection must have a reason.
    // An approval reason is optional.
    if (!approve && !this.decisionReason.trim()) {
      this.error = 'Please provide a reason for rejecting this institution.';
      this.cdr.markForCheck();
      return;
    }

    const action = approve ? 'approve' : 'reject';

    const confirmed = window.confirm(
      `Are you sure you want to ${action} this institution?`
    );

    if (!confirmed) {
      return;
    }

    const request: DecisionRequest = {
      approve,
      reason: this.decisionReason.trim() || null
    };

    this.saving = true;
    this.error = '';
    this.success = '';
    this.cdr.markForCheck();

    try {
      this.institution = await firstValueFrom(
        this.cdeService.decide(this.institution.id, request)
      );

      this.decisionReason = '';

      this.success = approve
        ? 'Institution approved successfully.'
        : 'Institution rejected successfully.';

    } catch (e: any) {
      console.error('Error submitting CDE decision:', e);

      this.error =
        e?.error?.message || 'Unable to submit the decision.';

    } finally {
      this.saving = false;
      this.cdr.markForCheck();
    }
  }

  // Return to CDE dashboard
  goBack(): void {
    this.router.navigate(['/cde-dashboard']);
  }
}