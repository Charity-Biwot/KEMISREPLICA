import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QasoInstitutionDetails } from './qaso-institution-details';

describe('QasoInstitutionDetails', () => {
  let component: QasoInstitutionDetails;
  let fixture: ComponentFixture<QasoInstitutionDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QasoInstitutionDetails],
    }).compileComponents();

    fixture = TestBed.createComponent(QasoInstitutionDetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
