import { TestBed } from '@angular/core/testing';

import { PennantService } from './pennant.service';

describe('Pennant', () => {
  let service: PennantService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PennantService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
