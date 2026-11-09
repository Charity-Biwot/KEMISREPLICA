import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CdeInstitutionDetails } from './cde-institution-details';

describe('CdeInstitutionDetails', () => {
  let component: CdeInstitutionDetails;
  let fixture: ComponentFixture<CdeInstitutionDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CdeInstitutionDetails],
    }).compileComponents();

    fixture = TestBed.createComponent(CdeInstitutionDetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
