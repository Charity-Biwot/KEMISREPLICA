import { TestBed } from '@angular/core/testing';
import { Scde } from './scde';

describe('Scde', () => {
  let service: Scde;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Scde);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
