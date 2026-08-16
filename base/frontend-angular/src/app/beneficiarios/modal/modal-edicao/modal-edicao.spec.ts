import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ModalEdicao } from './modal-edicao';

describe('ModalEdicao', () => {
  let component: ModalEdicao;
  let fixture: ComponentFixture<ModalEdicao>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModalEdicao]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ModalEdicao);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
