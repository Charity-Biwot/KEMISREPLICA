import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScdeDashboard } from './scde-dashboard';

describe('ScdeDashboard', () => {
  let component: ScdeDashboard;
  let fixture: ComponentFixture<ScdeDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScdeDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(ScdeDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
