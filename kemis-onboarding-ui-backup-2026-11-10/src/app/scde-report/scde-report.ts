
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-scde-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './scde-report.html',
  styleUrl: './scde-report.scss',
})
export class ScdeReport {
  institutionId: number | null = null;
  summary = '';
  recommendation = '';
  message = '';
  isSubmitting = false;

  private readonly apiUrl = 'http://localhost:5137/api/reports';

  constructor(private http: HttpClient) {}

  submitReport(): void {
    if (this.isSubmitting) {
      return;
    }

    this.message = '';

    if (
      this.institutionId === null ||
      this.institutionId < 1 ||
      !this.summary.trim() ||
      !this.recommendation
    ) {
      this.message = 'Please complete all fields with valid details.';
      return;
    }

    const request = {
      institutionId: this.institutionId,
      summary: this.summary.trim(),
      recommendation: this.recommendation,
    };

    this.isSubmitting = true;

    this.http.post(this.apiUrl, request).subscribe({
      next: () => {
        this.message = 'Report submitted successfully.';
        this.institutionId = null;
        this.summary = '';
        this.recommendation = '';
        this.isSubmitting = false;
      },
      error: (error: HttpErrorResponse) => {
        console.error('Report submission error:', error);

        if (error.status === 401) {
          this.message = 'Your session is invalid or expired. Please sign in again.';
        } else if (error.status === 403) {
          this.message = 'Your account does not have permission to submit this report.';
        } else if (error.error?.message) {
          this.message = error.error.message;
        } else if (error.status === 0) {
          this.message = 'Cannot reach the backend. Check that the API is running.';
        } else {
          this.message = `Report submission failed. HTTP status: ${error.status}.`;
        }

        this.isSubmitting = false;
      },
      complete: () => {
        this.isSubmitting = false;
      },
    });
  }
}
