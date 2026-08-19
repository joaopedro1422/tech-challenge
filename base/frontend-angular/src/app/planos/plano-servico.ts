import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { API_BASE } from '../nucleo/api';
import { Plano } from './plano';

/**
 * Todo acesso à API passa por um serviço. Componente não chama HttpClient direto.
 */
@Injectable({ providedIn: 'root' })
export class PlanoServico {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);
  private planos$?: Observable<Plano[]>;
  listar(forceRefresh = false): Observable<Plano[]> {
    if (forceRefresh) {
      this.limparCache();
    }
    
    if (!this.planos$) {
      this.planos$ = this.http.get<Plano[]>(`${this.base}/planos`).pipe(
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }
    return this.planos$;
  }
  limparCache(): void {
    this.planos$ = undefined;
  }
}
