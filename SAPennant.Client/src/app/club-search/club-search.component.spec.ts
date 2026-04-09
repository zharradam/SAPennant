import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClubSearchComponent } from './club-search.component';

describe('ClubSearchComponent', () => {
  let component: ClubSearchComponent;
  let fixture: ComponentFixture<ClubSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ClubSearchComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ClubSearchComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
