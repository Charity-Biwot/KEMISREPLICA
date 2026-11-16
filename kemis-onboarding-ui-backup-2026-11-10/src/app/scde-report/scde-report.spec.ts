import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScdeReport } from './scde-report';

describe('ScdeReport', () => {
  let component: ScdeReport;
  let fixture: ComponentFixture<ScdeReport>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScdeReport],
    }).compileComponents();

    fixture = TestBed.createComponent(ScdeReport);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
