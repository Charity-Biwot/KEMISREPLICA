import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import {
  DuplicateMatch,
  InstitutionOnboardingService
} from '../services/institution-onboarding.service';

interface Option {
  value: number;
  label: string;
}

@Component({
  selector: 'app-institution-onboarding',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './institution-onboarding.html',
  styleUrls: ['./institution-onboarding.scss']
})
export class InstitutionOnboarding {


  departments: Option[] = [
    { value: 1, label: 'Basic education' },
    { value: 2, label: 'TVET' },
    { value: 3, label: 'Higher Education' }
  ];

  institutionTypes: string[] = ['Boys school', 'Girls school', 'Mixed'];

  applicationTypes: Option[] = [
    { value: 1, label: 'Regular' },
    { value: 2, label: 'Integrated' },
    { value: 3, label: 'Special school' },
    { value: 4, label: 'Regular with special unit' }
  ];

  levels: Option[] = [
    { value: 1, label: 'Pre-primary (pre-1)' },
    { value: 2, label: 'Primary (pri-2)' },
    { value: 3, label: 'Pre-primary and primary (pre&pri-3)' },
    { value: 4, label: 'Junior school (JS-4)' },
    { value: 5, label: 'Primary and junior school (pri&JS-5)' },
    { value: 6, label: 'Senior school (SS-6)' },
    { value: 7, label: 'PTE (PTE-7)' }
  ];

  institutionLevelNames: string[] = ['Pre-primary', 'Primary', 'Junior school', 'Senior school'];

  accommodationTypes: Option[] = [
    { value: 1, label: 'Day' },
    { value: 2, label: 'Boarding' },
    { value: 3, label: 'Hybrid' }
  ];


  step = 0;

  form = {
    stateDepartment: 0,
    institutionType: '',
    email: '',
    mobileNumber: '',
    name: '',
    postalCode: '',
    county: '',
    subCounty: '',
    constituency: '',
    postalLocation: '',
    institutionLevelName: '',
    latitude: null as number | null,
    longitude: null as number | null,
    schoolCategories: '',
    accommodationType: 0,
    hasCertificate: null as boolean | null,
    certificateNumber: '',
    applicationType: 0,
    level: 0,
    uic: '',
    nameAsOnCertificate: ''
  };

  certificateFile: File | null = null;
  certificateUploaded = false;

  applicationId: number | null = null;
  resumeCode = '';

  applicationFee = 0;
  paymentReference = '';

  errors: Record<string, string> = {};
  serverMessages: string[] = [];
  duplicates: DuplicateMatch[] = [];
  acknowledged = false;

  saving = false;
  submitting = false;
  paying = false;

  showResume = false;
  resumeId: number | null = null;
  resumeInput = '';
  resumeMessage = '';

  constructor(
    private service: InstitutionOnboardingService,
    private cdr: ChangeDetectorRef
  ) {}


  async next(): Promise<void> {
    if (!this.validateStep(this.step)) {
      return;
    }

    if (this.step === 2) {
      const saved = await this.saveDraft();
      if (!saved) {
        return;
      }
    }

    this.step++;
    this.scrollToTop();
    this.cdr.detectChanges();   // CHANGED: refresh the screen after the step changes
  }

  back(): void {
    if (this.step > 0 && this.step < 4) {
      this.step--;
      this.errors = {};
      this.serverMessages = [];
      this.scrollToTop();
    }
  }

  goTo(target: number): void {
    this.step = target;
    this.errors = {};
    this.serverMessages = [];
    this.scrollToTop();
  }


  validateStep(step: number): boolean {
    const e: Record<string, string> = {};
    const f = this.form;
    const required = (key: string, value: string, message: string) => {
      if (!value || !value.trim()) { e[key] = message; }
    };

    if (step === 1) {
      if (!f.stateDepartment) { e['stateDepartment'] = 'Select the relevant state department.'; }
    }

    if (step === 2) {
      if (!f.institutionType) { e['institutionType'] = 'Select the institution type.'; }

      if (!f.email.trim()) {
        e['email'] = 'Enter the institution email.';
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.trim())) {
        e['email'] = 'Enter a valid email address.';
      }

      if (!f.mobileNumber.trim()) {
        e['mobileNumber'] = 'Enter the institution mobile number.';
      } else if (!/^(?:\+254|0)[17]\d{8}$/.test(f.mobileNumber.trim())) {
        e['mobileNumber'] = 'Enter a valid Kenyan mobile number, such as 0712345678.';
      }

      required('name', f.name, 'Enter the institution name.');
      required('county', f.county, 'Enter the county.');
      required('subCounty', f.subCounty, 'Enter the sub-county.');
      required('constituency', f.constituency, 'Enter the constituency.');

      if (!f.institutionLevelName) { e['institutionLevelName'] = 'Select the institution level.'; }

      const hasLat = f.latitude !== null && f.latitude !== undefined;
      const hasLng = f.longitude !== null && f.longitude !== undefined;
      if (hasLat !== hasLng) {
        e['coordinates'] = 'Enter both latitude and longitude, or leave both empty.';
      } else if (hasLat && (f.latitude! < -90 || f.latitude! > 90)) {
        e['coordinates'] = 'Latitude must be between -90 and 90.';
      } else if (hasLng && (f.longitude! < -180 || f.longitude! > 180)) {
        e['coordinates'] = 'Longitude must be between -180 and 180.';
      }

      if (!f.accommodationType) { e['accommodationType'] = 'Select the accommodation type.'; }

      if (f.hasCertificate === null) {
        e['hasCertificate'] = 'Tell us whether the school has a registration certificate.';
      } else if (f.hasCertificate) {
        required('certificateNumber', f.certificateNumber, 'Enter the registration certificate number.');
        required('nameAsOnCertificate', f.nameAsOnCertificate, 'Enter the name as it appears in the certificate.');
        if (!this.certificateFile && !this.certificateUploaded) {
          e['certificateFile'] = 'Upload the registration certificate.';
        }
      }

      if (!f.applicationType) { e['applicationType'] = 'Select the application type.'; }
      if (!f.level) { e['level'] = 'Select the level.'; }
      required('uic', f.uic, 'Enter the unique institutional code.');
    }

    this.errors = e;
    return Object.keys(e).length === 0;
  }


  onCertificateSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    delete this.errors['certificateFile'];

    if (!file) {
      this.certificateFile = null;
      return;
    }

    const allowed = ['.pdf', '.jpg', '.jpeg', '.png'];
    const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();

    if (!allowed.includes(extension)) {
      this.errors['certificateFile'] = 'Upload a PDF, JPG or PNG file.';
      this.certificateFile = null;
      input.value = '';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.errors['certificateFile'] = 'The file must be 5 MB or smaller.';
      this.certificateFile = null;
      input.value = '';
      return;
    }

    this.certificateFile = file;
  }

  onCertificateAnswerChange(): void {
    delete this.errors['hasCertificate'];
    if (!this.form.hasCertificate) {
      this.form.certificateNumber = '';
      this.form.nameAsOnCertificate = '';
      this.certificateFile = null;
      this.certificateUploaded = false;
    }
  }

  useMyLocation(): void {
    if (!navigator.geolocation) {
      this.errors['coordinates'] = 'Location is not available in this browser. Enter the coordinates manually.';
      return;
    }

    navigator.geolocation.getCurrentPosition(
      position => {
        this.form.latitude = Number(position.coords.latitude.toFixed(6));
        this.form.longitude = Number(position.coords.longitude.toFixed(6));
        delete this.errors['coordinates'];
        this.cdr.detectChanges();
      },
      () => {
        this.errors['coordinates'] = 'We could not get your location. Allow location access or enter the coordinates manually.';
        this.cdr.detectChanges();
      }
    );
  }


  private buildPayload(): any {
    const f = this.form;
    const text = (value: string) => (value && value.trim() ? value.trim() : null);

    return {
      stateDepartment: f.stateDepartment,
      institutionType: f.institutionType,
      applicationType: f.applicationType,
      name: f.name.trim(),
      uic: f.uic.trim(),
      level: f.level,
      institutionLevelName: f.institutionLevelName,
      accommodationType: f.accommodationType,
      schoolCategories: text(f.schoolCategories),
      county: f.county.trim(),
      subCounty: f.subCounty.trim(),
      constituency: f.constituency.trim(),
      postalCode: text(f.postalCode),
      postalLocation: text(f.postalLocation),
      latitude: f.latitude,
      longitude: f.longitude,
      email: f.email.trim(),
      mobileNumber: f.mobileNumber.trim(),
      hasCertificate: f.hasCertificate === true,
      certificateNumber: f.hasCertificate ? text(f.certificateNumber) : null,
      nameAsOnCertificate: f.hasCertificate ? text(f.nameAsOnCertificate) : null
    };
  }

  private async saveDraft(): Promise<boolean> {
    this.saving = true;
    this.serverMessages = [];
    this.cdr.detectChanges();

    try {
      const body = this.buildPayload();

      if (this.applicationId === null) {
        const created = await firstValueFrom(this.service.create(body));
        this.applicationId = created.id;
        this.resumeCode = created.resumeCode;
      } else {
        await firstValueFrom(this.service.update(this.applicationId, this.resumeCode, body));
      }

      if (this.form.hasCertificate && this.certificateFile) {
        await firstValueFrom(
          this.service.uploadCertificate(this.applicationId, this.resumeCode, this.certificateFile)
        );
        this.certificateUploaded = true;
        this.certificateFile = null;
      }

      return true;
    } catch (error) {
      this.showServerError(error);
      return false;
    } finally {
      this.saving = false;
      this.cdr.detectChanges();
    }
  }

  async resume(): Promise<void> {
    this.resumeMessage = '';

    if (!this.resumeId || !this.resumeInput.trim()) {
      this.resumeMessage = 'Enter your application number and resume code.';
      return;
    }

    try {
      const v = await firstValueFrom(this.service.get(this.resumeId, this.resumeInput.trim()));

      this.applicationId = v.id;
      this.resumeCode = this.resumeInput.trim();
      this.certificateUploaded = !!v.certificateUploaded;
      this.applicationFee = v.applicationFee ?? 0;
      this.paymentReference = v.paymentReference ?? '';

      this.form = {
        stateDepartment: v.stateDepartment,
        institutionType: v.institutionType ?? '',
        email: v.email ?? '',
        mobileNumber: v.mobileNumber ?? '',
        name: v.name ?? '',
        postalCode: v.postalCode ?? '',
        county: v.county ?? '',
        subCounty: v.subCounty ?? '',
        constituency: v.constituency ?? '',
        postalLocation: v.postalLocation ?? '',
        institutionLevelName: v.institutionLevelName ?? '',
        latitude: v.latitude ?? null,
        longitude: v.longitude ?? null,
        schoolCategories: v.schoolCategories ?? '',
        accommodationType: v.accommodationType,
        hasCertificate: v.hasCertificate,
        certificateNumber: v.certificateNumber ?? '',
        applicationType: v.applicationType,
        level: v.level,
        uic: v.uic ?? '',
        nameAsOnCertificate: v.nameAsOnCertificate ?? ''
      };

      this.showResume = false;
      this.step = v.status === 3 ? 5 : v.status === 2 ? 4 : 3;
      this.cdr.detectChanges();
    } catch {
      this.resumeMessage = 'We could not find an application with that number and code.';
      this.cdr.detectChanges();
    }
  }

  async submit(): Promise<void> {
    if (this.applicationId === null) {
      return;
    }

    this.submitting = true;
    this.serverMessages = [];
    this.cdr.detectChanges();

    try {
      const result = await firstValueFrom(
        this.service.submit(this.applicationId, this.resumeCode, this.acknowledged)
      );
      this.duplicates = [];
      this.applicationFee = result.applicationFee ?? 0;
      this.step = 4; // payment window
      this.scrollToTop();
    } catch (error) {
      const err = error as HttpErrorResponse;
      if (err.status === 409 && Array.isArray(err.error?.details)) {
        // Possible duplicates: show them and ask the applicant to confirm.
        this.duplicates = err.error.details;
        this.acknowledged = false;
      } else {
        this.showServerError(error);
      }
    } finally {
      this.submitting = false;
      this.cdr.detectChanges();
    }
  }

  async pay(): Promise<void> {
    if (this.applicationId === null) {
      return;
    }

    this.paying = true;
    this.serverMessages = [];
    this.cdr.detectChanges();

    try {
      const result = await firstValueFrom(this.service.pay(this.applicationId, this.resumeCode));
      this.paymentReference = result.paymentReference ?? '';
      this.applicationFee = result.applicationFee ?? 0;
      this.step = 5; // confirmation
      this.scrollToTop();
    } catch (error) {
      this.showServerError(error);
    } finally {
      this.paying = false;
      this.cdr.detectChanges();
    }
  }


  label(options: Option[], value: number): string {
    return options.find(o => o.value === value)?.label ?? '';
  }

  private showServerError(error: unknown): void {
    const err = error as HttpErrorResponse;

    if (err.status === 0) {
      this.serverMessages = ['We could not reach the server. Check your connection and try again.'];
      return;
    }

    if (err.error?.errors) {
      this.serverMessages = Object.values(err.error.errors as Record<string, string[]>)
        .reduce((all, list) => all.concat(list), [] as string[]);
      return;
    }

    this.serverMessages = [err.error?.message || 'Something went wrong. Try again.'];
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0 });
  }
}