
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PendingInstitution {
  id: number;
  name: string;
  uic: string;
  county: string;
  subCounty: string;
  approvalStartedAt: string | null;
  scdeAssigned: boolean;
  cqasoAssigned: boolean;
  canDecide: boolean;
}

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  role: string;
  county: string | null;
}

export interface AssignRequest {
  assigneeUserId: string;
  role: 'QASO';
}

export interface Assignment {
  id: string;
  assignedToUserId: string;
  assignedToName: string;
  role: string;
  status: string;
  assignedAt: string;
}

export interface Report {
  id: string;
  type: string;
  authorName: string;
  summary: string;
  recommendation: string;
  submittedAt: string;
}

export interface Comment {
  id: string;
  reportId: string | null;
  authorName: string;
  text: string;
  createdAt: string;
}

export interface InstitutionDetail {
  id: number;
  name: string;
  nameAsOnCertificate: string | null;
  uic: string;
  stateDepartment: string;
  institutionType: string;
  applicationType: string;
  institutionLevel: string;
  email: string;
  mobileNumber: string;
  county: string;
  subCounty: string;
  constituency: string;
  latitude: number | null;
  longitude: number | null;
  hasCertificate: boolean;
  certificateNumber: string | null;
  status: string;
  approvalStartedAt: string | null;
  decisionReason: string | null;
  decidedAt: string | null;
  assignments: Assignment[];
  reports: Report[];
  comments: Comment[];
  canDecide: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ScdeService {
  private readonly apiUrl = 'http://localhost:5137/api/scde';

  constructor(private http: HttpClient) {}

  getInstitutions(): Observable<PendingInstitution[]> {
    return this.http.get<PendingInstitution[]>(
      `${this.apiUrl}/institutions`
    );
  }

  getInstitutionDetails(id: number): Observable<InstitutionDetail> {
    return this.http.get<InstitutionDetail>(
      `${this.apiUrl}/institutions/${id}`
    );
  }

  getQasos(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(
      `${this.apiUrl}/qasos`
    );
  }

  assignQaso(
    institutionId: number,
    request: AssignRequest
  ): Observable<InstitutionDetail> {
    return this.http.post<InstitutionDetail>(
      `${this.apiUrl}/institutions/${institutionId}/assign-qaso`,
      request
    );
  }
}
