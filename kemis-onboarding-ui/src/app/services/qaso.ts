
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// =========================================================
// QASO DASHBOARD
// =========================================================

export interface QasoDashboardItem {
  assignmentId: string;
  institutionId: number;
  institutionName: string;
  uic: string;
  county: string;
  subCounty: string;
  hasCertificate: boolean;
  certificateNumber: string | null;
  assignedAt: string;
  certificateStatus: string;
  physicalInspectionStatus: string;
  scheduledAt: string | null;
  hasSubmittedReport: boolean;
}

// =========================================================
// INSPECTION EVIDENCE
// =========================================================

export interface InspectionEvidence {
  id: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
}

// =========================================================
// QASO INSTITUTION DETAILS
// =========================================================

export interface QasoInspectionDetail {
  // Assignment and institution identification
  assignmentId: string;
  institutionId: number;
  institutionName: string;
  uic: string;

  // Basic institution contact information
  email: string;
  mobileNumber: string;

  // Institution classification
  institutionType: string;
  institutionLevel: string;
  stateDepartment: string;
  applicationType: string;
  accommodationType: string;
  schoolCategories: string | null;

  // Certificate registration information
  nameAsOnCertificate: string | null;
  hasCertificate: boolean;
  certificateNumber: string | null;
  certificateFileAvailable: boolean;

  // Institution location information
  county: string;
  subCounty: string;
  constituency: string;
  postalCode: string | null;
  postalLocation: string | null;
  latitude: number | null;
  longitude: number | null;

  // Registration dates
  createdAt: string;
  submittedAt: string | null;

  // Certificate verification
  certificateStatus: string;
  certificateAuthenticityVerified: boolean | null;
  certificateIssuerVerified: boolean | null;
  certificateNotExpired: boolean | null;
  certificateComplete: boolean | null;
  certificateReviewNotes: string | null;
  certificateReviewedAt: string | null;

  // Physical inspection
  physicalInspectionStatus: string;
  scheduledAt: string | null;
  scheduleNotes: string | null;

  // Inspection report and evidence
  hasSubmittedReport: boolean;
  evidenceFiles: InspectionEvidence[];
}

// =========================================================
// REQUEST INTERFACES
// =========================================================

export interface ReviewCertificateRequest {
  authenticityVerified: boolean | null;
  issuerVerified: boolean | null;
  notExpired: boolean | null;
  complete: boolean | null;
  notes: string | null;
}

export interface ScheduleInspectionRequest {
  scheduledAt: string;
  notes: string | null;
}

export interface SubmitQasoReportRequest {
  institutionId: number;
  summary: string;
  findings: string;
  recommendation: 'Approve' | 'Reject';
}

// =========================================================
// REPORT RESPONSE
// =========================================================

export interface QasoReport {
  id: string;
  type: string;
  authorName: string;
  summary: string;
  findings: string | null;
  recommendation: string;
  submittedAt: string;
}

// =========================================================
// QASO SERVICE
// =========================================================

@Injectable({
  providedIn: 'root'
})
export class QasoService {

  private readonly apiUrl = 'http://localhost:5137/api/qaso';

  constructor(private http: HttpClient) {}

  // -------------------------------------------------------
  // GET ASSIGNED INSTITUTIONS
  // -------------------------------------------------------

  getAssignedInstitutions(): Observable<QasoDashboardItem[]> {
    return this.http.get<QasoDashboardItem[]>(
      `${this.apiUrl}/institutions`
    );
  }

  // -------------------------------------------------------
  // GET INSTITUTION DETAILS
  // -------------------------------------------------------

  getInstitutionDetails(
    institutionId: number
  ): Observable<QasoInspectionDetail> {
    return this.http.get<QasoInspectionDetail>(
      `${this.apiUrl}/institutions/${institutionId}`
    );
  }

  // -------------------------------------------------------
  // REVIEW CERTIFICATE
  // -------------------------------------------------------

  reviewCertificate(
    institutionId: number,
    request: ReviewCertificateRequest
  ): Observable<QasoInspectionDetail> {
    return this.http.post<QasoInspectionDetail>(
      `${this.apiUrl}/institutions/${institutionId}/certificate-review`,
      request
    );
  }

  // -------------------------------------------------------
  // SCHEDULE PHYSICAL INSPECTION
  // -------------------------------------------------------

  scheduleInspection(
    institutionId: number,
    request: ScheduleInspectionRequest
  ): Observable<QasoInspectionDetail> {
    return this.http.post<QasoInspectionDetail>(
      `${this.apiUrl}/institutions/${institutionId}/schedule-inspection`,
      request
    );
  }

  // -------------------------------------------------------
  // COMPLETE PHYSICAL INSPECTION
  // -------------------------------------------------------

  completePhysicalInspection(
    institutionId: number
  ): Observable<QasoInspectionDetail> {
    return this.http.post<QasoInspectionDetail>(
      `${this.apiUrl}/institutions/${institutionId}/complete-inspection`,
      {}
    );
  }

  // -------------------------------------------------------
  // UPLOAD INSPECTION EVIDENCE
  // -------------------------------------------------------

  uploadEvidence(
    institutionId: number,
    file: File
  ): Observable<QasoInspectionDetail> {

    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<QasoInspectionDetail>(
      `${this.apiUrl}/institutions/${institutionId}/evidence`,
      formData
    );
  }

  // -------------------------------------------------------
  // SUBMIT INSPECTION REPORT TO SCDE
  // -------------------------------------------------------

  submitReport(
    institutionId: number,
    request: SubmitQasoReportRequest
  ): Observable<QasoReport> {
    return this.http.post<QasoReport>(
      `${this.apiUrl}/institutions/${institutionId}/reports`,
      request
    );
  }
}
