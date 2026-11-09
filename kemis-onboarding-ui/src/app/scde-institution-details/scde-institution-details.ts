
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

import { ScdeService } from '../services/scde';

@Component({
  selector: 'app-scde-institution-details',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="details-page">
      <header class="page-header">
        <h2>Institution Details</h2>
        <button (click)="goBack()">Back to Dashboard</button>
      </header>

      <div *ngIf="loading">Loading institution details...</div>

      <div *ngIf="errorMessage" class="error">
        {{ errorMessage }}
      </div>

      <section *ngIf="!loading && institution" class="details-card">
        <h3>{{ institution.name }}</h3>

        <p><strong>UIC:</strong> {{ institution.uic }}</p>
        <p><strong>County:</strong> {{ institution.county }}</p>
        <p><strong>Sub County:</strong> {{ institution.subCounty }}</p>
        <p><strong>Status:</strong> {{ institution.status }}</p>

        <hr />

        <h3>QASO Assignment</h3>
        <p>QASO assignment functionality will be connected here.</p>
      </section>
    </div>
  `
})
export class ScdeInstitutionDetails implements OnInit {

  institution: any = null;
  loading = true;
  errorMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private scdeService: ScdeService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id || isNaN(id)) {
      this.errorMessage = 'Invalid institution ID.';
      this.loading = false;
      return;
    }

    this.scdeService.getInstitutionDetails(id).subscribe({
      next: (data) => {
        this.institution = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Failed to load institution details:', error);
        this.errorMessage = 'Unable to load institution details.';
        this.loading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/scde-dashboard']);
  }
}
