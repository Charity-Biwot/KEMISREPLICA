import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DuplicateMatch {
  institutionId: number;
  name: string;
  uic: string;
  county: string;
  subCounty: string;
  level: string;
  certificateNumber: string | null;
  reason: string;
}

export interface CreateDraftResponse {
  id: number;
  resumeCode: string;
}

@Injectable({ providedIn: 'root' })
export class InstitutionOnboardingService {
  private readonly baseUrl = 'http://localhost:5137/api/institution-onboarding';

  constructor(private http: HttpClient) {}

  create(body: unknown): Observable<CreateDraftResponse> {
    return this.http.post<CreateDraftResponse>(this.baseUrl, body);
  }

  get(id: number, resumeCode: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${id}`, { headers: this.headers(resumeCode) });
  }

  update(id: number, resumeCode: string, body: unknown): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/${id}`, body, { headers: this.headers(resumeCode) });
  }

  uploadCertificate(id: number, resumeCode: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.baseUrl}/${id}/certificate`, formData, {
      headers: this.headers(resumeCode)
    });
  }

  submit(id: number, resumeCode: string, acknowledgeDuplicateWarning: boolean): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}/${id}/submit`,
      { acknowledgeDuplicateWarning },
      { headers: this.headers(resumeCode) }
    );
  }

  pay(id: number, resumeCode: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/payment`, {}, { headers: this.headers(resumeCode) });
  }

  private headers(resumeCode: string): HttpHeaders {
    return new HttpHeaders({ 'X-Resume-Code': resumeCode });
  }
}