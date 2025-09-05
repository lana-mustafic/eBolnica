import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DoctorProfileEditComponent } from './doctor-profile-edit.component';

describe('DoctorProfileEditComponent', () => {
  let component: DoctorProfileEditComponent;
  let fixture: ComponentFixture<DoctorProfileEditComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DoctorProfileEditComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DoctorProfileEditComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
