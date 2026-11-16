import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CdeDashboard } from './cde-dashboard';

describe('CdeDashboard', () => {
  let component: CdeDashboard;
  let fixture: ComponentFixture<CdeDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CdeDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(CdeDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
