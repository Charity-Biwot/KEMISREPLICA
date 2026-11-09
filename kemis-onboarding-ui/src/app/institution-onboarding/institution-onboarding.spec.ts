import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InstitutionOnboarding } from './institution-onboarding';

describe('InstitutionOnboarding', () => {
  let component: InstitutionOnboarding;
  let fixture: ComponentFixture<InstitutionOnboarding>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InstitutionOnboarding],
    }).compileComponents();

    fixture = TestBed.createComponent(InstitutionOnboarding);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
