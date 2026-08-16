import { Component, HostListener, inject, signal } from '@angular/core';
import {BeneficiarioServico } from '../beneficiario-servico';
import { Beneficiario, StatusBeneficiario } from '../beneficiarioModels';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { mensagemDeErro } from '../../nucleo/api';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ModalCadastro } from '../modal/modal-cadastro/modal-cadastro';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-beneficiarios-component',
  imports: [MatDialogModule],
  templateUrl: './beneficiarios-component.html',
  styleUrl: './beneficiarios-component.css',
})
export class BeneficiariosComponent {
  private readonly servico = inject(BeneficiarioServico);
  menuAbertoId = signal<string | null>(null);
  beneficiarios = signal<Beneficiario[]>([]);
  private readonly dialog = inject(MatDialog);
  totalRegistros = signal<number>(0);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);
  private readonly snackBar = inject(MatSnackBar);
  constructor() {
    this.carregarBeneficiarios();
  }
  toggleMenu(id: string, event: Event): void {
    event.stopPropagation(); 
    this.menuAbertoId.set(this.menuAbertoId() === id ? null : id);
  }
  @HostListener('document:click')
  fecharMenu(): void {
    this.menuAbertoId.set(null);
  }

  editar(beneficiario: Beneficiario): void {
    this.menuAbertoId.set(null);
    console.log('Editar:', beneficiario);
  }
  abrirModalCadastro(){
    const dialogRef = this.dialog.open(ModalCadastro, {
      width: '450px',
      disableClose: false 
    });

    dialogRef.afterClosed().subscribe((resultado) => {
      if (resultado === true) {
        this.snackBar.open('Beneficiário adicionado com sucesso.', 'Fechar', {
          duration: 3500, 
          horizontalPosition: 'center',
          verticalPosition: 'bottom',
          panelClass: ['toast-sucesso'] 
        });
        this.carregarBeneficiarios(); 
      }
    });
  }
  excluir(id: string): void {
    this.menuAbertoId.set(null);
    console.log('Excluir ID:', id);
  }
  protected carregarBeneficiarios(): void {
    this.carregando.set(true);
    this.erro.set(null);

    const filtro = {
      pagina: 1,
      tamanho: 10,
      status: 'ATIVO' as StatusBeneficiario
    };

    this.servico.listar(filtro).subscribe({
      next: (resultado) => {
        this.beneficiarios.set(resultado.dados);
        this.totalRegistros.set(resultado.total);
        this.carregando.set(false);
      },
      error: (err) => {
        console.error('Erro ao buscar beneficiários:', err);
        this.erro.set('Falha ao carregar a lista de beneficiários.');
        this.carregando.set(false);
      }
    });
  }
}
