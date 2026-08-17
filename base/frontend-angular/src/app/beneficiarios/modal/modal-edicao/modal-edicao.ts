import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Beneficiario } from '../../beneficiarioModels';
import { BeneficiarioServico } from '../../beneficiario-servico';
import { PlanoServico } from '../../../planos/plano-servico';
import { Plano } from '../../../planos/plano';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
@Component({
  selector: 'app-modal-edicao',
  imports: [
    MatDialogModule, 
    ReactiveFormsModule, 
    MatFormFieldModule,
    MatSelectModule,
    DatePipe
  ],
  templateUrl: './modal-edicao.html',
  styleUrl: './modal-edicao.css',
})
export class ModalEdicao {
  readonly beneficiario = inject<Beneficiario>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ModalEdicao>);
  private readonly planoService = inject(PlanoServico);
  readonly planos = signal<Plano[]>([]);
  readonly carregando = signal<boolean>(false);
  readonly erro = signal<string | null>(null);
  private readonly beneficiarioService = inject(BeneficiarioServico);
  form: FormGroup = this.fb.group({
    nomeCompleto: ['', [Validators.required, Validators.minLength(3)]],
    dataNascimento: ['', [Validators.required]],
    status: ['ATIVO', [Validators.required]],
    planoId: ['', [Validators.required]]
  });
  ngOnInit(): void {
    this.carregarPlanos();
    this.preencherFormulario();
  }
  private preencherFormulario(): void {
    if (this.beneficiario) {
      let dataFormatada = '';
      if (this.beneficiario.dataNascimento) {
        dataFormatada = this.beneficiario.dataNascimento.split('T')[0];
      }
      this.form.patchValue({
        nomeCompleto: this.beneficiario.nomeCompleto,
        dataNascimento: dataFormatada,
        status: this.beneficiario.status || 'ATIVO',
        planoId: this.beneficiario.planoId
      });
    }
  }
  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if(!this.beneficiario.id) return;

    this.carregando.set(true);
    this.erro.set(null);
    const payload = {
      nome_completo: this.form.value.nomeCompleto,
      data_nascimento: this.form.value.dataNascimento,
      plano_id: this.form.value.planoId,
      status: this.form.value.status
    };
    this.beneficiarioService.atualizar(this.beneficiario.id, payload as any).subscribe({
      next: () => {
        this.carregando.set(false);
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.carregando.set(false);
        const msg = err.error?.mensagem || 'Erro ao atualizar beneficiário. Tente novamente.';
        this.erro.set(msg);
      }
    });
  }

  fechar(): void {
    this.dialogRef.close(false);
  }
  private carregarPlanos(): void {
    this.planoService.listar().subscribe({
      next: (listaPlanos) =>{
         this.planos.set(listaPlanos)
      },
      error: (err) =>{
        console.error('Erro ao carregar lista de planos:', err)
      } 
    });
  }
}
