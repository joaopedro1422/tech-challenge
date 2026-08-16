import { Component, inject, signal } from '@angular/core';
import { BeneficiarioServico } from '../../beneficiario-servico';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms'; 
import {MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { PlanoServico } from '../../../planos/plano-servico';
import { Plano } from '../../../planos/plano';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-modal-cadastro',
  imports: [MatDialogModule, ReactiveFormsModule],
  templateUrl: './modal-cadastro.html',
  styleUrl: './modal-cadastro.css',
})
export class ModalCadastro {
  private readonly fb = inject(FormBuilder);
  private readonly servico = inject(BeneficiarioServico);
  private readonly dialogRef = inject(MatDialogRef<ModalCadastro>);
  private readonly planoService = inject(PlanoServico);
  carregando = signal(false);
  planos = signal<Plano[]>([]);
  erro = signal<string | null>(null);
  form = this.fb.group({
    nomeCompleto: ['', [Validators.required, Validators.minLength(3)]],
    cpf: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
    dataNascimento: ['', [Validators.required]],
    planoId: ['', [Validators.required]]
  });
  ngOnInit(): void {
    this.carregarPlanos();
  }
  protected carregarPlanos(): void {
    this.erro.set(null);

    this.planoService.listar()
      .subscribe({
        next: (planos) => {
          this.planos.set(planos);
        },
        error: (err) => {
          this.erro.set(err.mensagem);
        }
      });
  }
  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.carregando.set(true);
    this.servico.criar(this.form.value as any).subscribe({
      next: () => {
        this.carregando.set(false);
        this.dialogRef.close(true); 
      },
      error: (resposta: HttpErrorResponse) => {
        this.carregando.set(false);
        const mensagemApi = resposta.error?.mensagem || 'Erro ao processar a requisição.';
        this.erro.set(mensagemApi);
      }
    });
  }

  fechar(): void {
    this.dialogRef.close(false);
  }
}
