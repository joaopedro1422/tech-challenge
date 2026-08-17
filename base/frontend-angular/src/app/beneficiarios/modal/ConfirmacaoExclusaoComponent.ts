import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

export interface ConfirmDialogData {
  titulo?: string;
  mensagem: string;
  textoConfirmar?: string;
  textoCancelar?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule],
  template: `
    <h2 mat-dialog-title class="dialog-title">
      <i class="fa-solid fa-triangle-exclamation icone-alerta"></i>
      {{ data.titulo || 'Confirmar Exclusão' }}
    </h2>

    <mat-dialog-content>
      <p class="dialog-mensagem">Tem certeza que deseja excluir <strong>{{ data.mensagem }}</strong>? Esta ação não poderá ser desfeita.</p>
    </mat-dialog-content>

    <mat-dialog-actions align="end" class="dialog-acoes">
      <button type="button" class="btn-cancelar" (click)="cancelar()">
        {{ data.textoCancelar || 'Cancelar' }}
      </button>
      <button type="button" class="btn-excluir" (click)="confirmar()">
        {{ data.textoConfirmar || 'Excluir' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-title {
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 1.25rem;
      color: #1f2937;
      margin: 0;
    }
    .icone-alerta {
      color: #dc2626; /* Vermelho alerta */
      font-size: 1.3rem;
    }
    .dialog-mensagem {
      color: #4b5563;
      font-size: 0.95rem;
      margin: 12px 0 0 0;
    }
    .dialog-acoes {
      gap: 8px;
      padding-top: 16px;
    }
    .btn-cancelar {
      padding: 8px 16px;
      border: 1px solid #d1d5db;
      background-color: #ffffff;
      color: #374151;
      border-radius: 6px;
      font-weight: 500;
      cursor: pointer;
      transition: background-color 0.2s;
    }
    .btn-cancelar:hover {
      background-color: #f3f4f6;
    }
    .btn-excluir {
      padding: 8px 16px;
      border: none;
      background-color: #dc2626; /* Vermelho perigo */
      color: #ffffff;
      border-radius: 6px;
      font-weight: 500;
      cursor: pointer;
      transition: background-color 0.2s;
    }
    .btn-excluir:hover {
      background-color: #b91c1c;
    }
  `]
})
export class ConfirmDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
  readonly data: ConfirmDialogData = inject(MAT_DIALOG_DATA);

  confirmar(): void {
    this.dialogRef.close(true);
  }

  cancelar(): void {
    this.dialogRef.close(false);
  }
}