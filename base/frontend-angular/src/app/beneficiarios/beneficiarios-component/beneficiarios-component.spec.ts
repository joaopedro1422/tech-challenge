import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BeneficiariosComponent } from './beneficiarios-component';

describe('BeneficiariosComponent', () => {
  let component: BeneficiariosComponent;
  let fixture: ComponentFixture<BeneficiariosComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BeneficiariosComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BeneficiariosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
