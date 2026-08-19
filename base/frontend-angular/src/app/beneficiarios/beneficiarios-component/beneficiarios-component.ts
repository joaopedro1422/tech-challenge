import { Component, HostListener, inject, signal } from '@angular/core';
import {BeneficiarioServico } from '../beneficiario-servico';
import { Beneficiario, BeneficiarioFiltro, StatusBeneficiario } from '../beneficiarioModels';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { mensagemDeErro } from '../../nucleo/api';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ModalCadastro } from '../modal/modal-cadastro/modal-cadastro';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmDialogComponent } from '../modal/ConfirmacaoExclusaoComponent';
import { ModalEdicao } from '../modal/modal-edicao/modal-edicao';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { Plano } from '../../planos/plano';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { PlanoServico } from '../../planos/plano-servico';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

@Component({
  selector: 'app-beneficiarios-component',
  imports: [MatDialogModule ,MatFormFieldModule,
    MatSelectModule, ReactiveFormsModule, MatPaginatorModule],
  templateUrl: './beneficiarios-component.html',
  styleUrl: './beneficiarios-component.css',
})
export class BeneficiariosComponent {
  private readonly servico = inject(BeneficiarioServico);
  private readonly planoServico = inject(PlanoServico);
  menuAbertoId = signal<string | null>(null);
  beneficiarios = signal<Beneficiario[]>([]);
  planos = signal<Plano[]>([]);
  statusFilterControl = new FormControl('');
  planoFilterControl = new FormControl('');
  private readonly dialog = inject(MatDialog);
  paginaAtual = signal<number>(1);
  tamanhoPagina = signal<number>(10);
  totalRegistros = signal<number>(0);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);
  private readonly snackBar = inject(MatSnackBar);
  constructor() {
    this.carregarPlanos();
    this.carregarBeneficiarios();
    this.statusFilterControl.valueChanges.subscribe(() => {
      this.paginaAtual.set(1);
    });

    this.planoFilterControl.valueChanges.subscribe(() => {
      this.paginaAtual.set(1);
    });
  }
  aoMudarPagina(event: PageEvent): void {
    this.paginaAtual.set(event.pageIndex + 1);
    this.tamanhoPagina.set(event.pageSize);
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

  private carregarPlanos(): void {
    this.planoServico.listar().subscribe({
      next: (dados) => this.planos.set(dados),
      error: (err) => console.error('Erro ao carregar planos para o select:', err)
    });
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
  editarBeneficiario(beneficiario: Beneficiario): void{
    const dialogRef = this.dialog.open(ModalEdicao, {
      width: '450px',
      data: beneficiario 
    });
    dialogRef.afterClosed().subscribe((sucesso: boolean) => {
      if (sucesso) {
        this.snackBar.open('Beneficiário atualizado com sucesso!', 'Fechar', {
          duration: 3500,
          horizontalPosition: 'center',
          verticalPosition: 'bottom',
          panelClass: ['toast-sucesso'] 
        });
        
        this.carregarBeneficiarios();
      }
    });
  }
  excluirBeneficiario(beneficiario: Beneficiario): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        titulo: 'Excluir Beneficiário',
        mensagem: beneficiario.nomeCompleto,
        textoConfirmar: 'Sim, excluir',
        textoCancelar: 'Cancelar'
      }
    });

    dialogRef.afterClosed().subscribe((confirmado: boolean) => {
      if (confirmado) {
        if (this.beneficiarios().length === 1 && this.paginaAtual() > 1) {
          this.paginaAtual.update(p => p - 1);
        }
        this.executarExclusao(beneficiario.id!);
      }
    });
  }
  private executarExclusao(id:string): void {
    this.servico.deletar(id).subscribe({
      next: () => {
        this.snackBar.open('Beneficiário excluído com sucesso!', 'Fechar', {
          duration: 3000,
          horizontalPosition: 'center',
          verticalPosition: 'bottom',
          panelClass: ['toast-sucesso'] 
        });
        this.carregarBeneficiarios(); 
      },
      error: () => {
        this.snackBar.open('Erro ao excluir o beneficiário.', 'Fechar', {
          duration: 3000,
          horizontalPosition: 'center',
          verticalPosition: 'bottom'
        });
      }
    });
  }

  formatarCpf(cpf: string): string {
    if (!cpf) return '';
    const apenasNumeros = cpf.replace(/\D/g, '');
    
    if (apenasNumeros.length !== 11) {
      return cpf;
    }

    return apenasNumeros.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
  }

  formatarData(dataIso: string): string {
    if (!dataIso) return '';
    
    const dataLimpa = dataIso.split('T')[0];
    const partes = dataLimpa.split('-');

    if (partes.length !== 3) {
      return dataIso; 
    }

    const [ano, mes, dia] = partes;
    return `${dia}/${mes}/${ano}`;
  }

  excluir(id: string): void {
    this.menuAbertoId.set(null);
    console.log('Excluir ID:', id);
  }
  protected carregarBeneficiarios(): void {
    this.carregando.set(true);
    this.erro.set(null);
    const statusVal = this.statusFilterControl.value;
    const planoVal = this.planoFilterControl.value;
    const filtro: BeneficiarioFiltro = {
      pagina: this.paginaAtual(),
      tamanho: this.tamanhoPagina(),
      status: statusVal ? statusVal : null,
      planoId: planoVal ? planoVal : null
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
  recarregar(): void {

    this.paginaAtual.set(1);
    this.carregarBeneficiarios();
  }
}
