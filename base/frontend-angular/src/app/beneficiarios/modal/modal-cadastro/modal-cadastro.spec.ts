import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ModalCadastro } from './modal-cadastro';

describe('ModalCadastro', () => {
  let component: ModalCadastro;
  let fixture: ComponentFixture<ModalCadastro>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModalCadastro]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ModalCadastro);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
