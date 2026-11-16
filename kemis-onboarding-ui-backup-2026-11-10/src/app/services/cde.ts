import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  InstitutionDetail,
  PendingInstitution,
  UserSummary
} from './scde';

export interface AppNotification {
  id: string;
  recipientUserId: string;
  institutionId: number;
  type: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface DashboardSummary {
  approved: number;
  rejected: number;
  pending: number;
}

export interface CdeAssignRequest {
  assigneeUserId: string;
  role: 'SCDE' | 'CQASO';
}

export interface CommentRequest {
  text: string;
  reportId: string | null;
}

export interface DecisionRequest {
  approve: boolean;
  reason: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class CdeService {
  private readonly apiUrl = 'http://localhost:5137/api';

  constructor(private http: HttpClient) {}

  // Get pending institutions
  getPending(): Observable<PendingInstitution[]> {
    return this.http.get<PendingInstitution[]>(
      `${this.apiUrl}/cde/institutions/pending`
    );
  }

  // Get institution details
  getDetails(id: number): Observable<InstitutionDetail> {
    return this.http.get<InstitutionDetail>(
      `${this.apiUrl}/cde/institutions/${id}`
    );
  }

  // Get notifications
  getNotifications(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(
      `${this.apiUrl}/notifications`
    );
  }

  // Mark notification as read
  markRead(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/notifications/${id}/read`,
      {}
    );
  }

  // Get CDE dashboard statistics
  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(
      `${this.apiUrl}/cde/dashboard`
    );
  }

  // Get available officers
  getAssignees(role: 'SCDE' | 'CQASO'): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(
      `${this.apiUrl}/cde/assignees`,
      { params: { role } }
    );
  }

  // Assign SCDE or CQASO
  assign(
    id: number,
    request: CdeAssignRequest
  ): Observable<InstitutionDetail> {
    return this.http.post<InstitutionDetail>(
      `${this.apiUrl}/cde/institutions/${id}/assign`,
      request
    );
  }

  // Add CDE review comment
  addComment(
    id: number,
    request: CommentRequest
  ): Observable<InstitutionDetail> {
    return this.http.post<InstitutionDetail>(
      `${this.apiUrl}/cde/institutions/${id}/comments`,
      request
    );
  }

  // CDE final approval or rejection
  decide(
    id: number,
    request: DecisionRequest
  ): Observable<InstitutionDetail> {
    return this.http.post<InstitutionDetail>(
      `${this.apiUrl}/cde/institutions/${id}/decision`,
      request
    );
  }
}