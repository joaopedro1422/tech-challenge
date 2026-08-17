import { HttpClient, HttpParams } from "@angular/common/http";
import { Beneficiario, BeneficiarioAtualizacaoRequest, BeneficiarioCriacaoRequest, BeneficiarioFiltro, ListaPaginadaBeneficiarios } from "./beneficiarioModels";
import { map, Observable } from "rxjs";
import { Injectable } from "@angular/core";

@Injectable({
  providedIn: 'root'
})
export class BeneficiarioServico {
    private readonly apiUrl = 'http://localhost:9999/beneficiarios';
    constructor(private http: HttpClient) {}
    listar(filtro?: BeneficiarioFiltro): Observable<ListaPaginadaBeneficiarios> {
        let params = new HttpParams();

        const pagina = filtro?.pagina ?? 1;
        const tamanho = filtro?.tamanho ?? 20;

        params = params.set('pagina', pagina.toString());
        params = params.set('tamanho', tamanho.toString());

        if (filtro?.status) {
        params = params.set('status', filtro.status);
        }

        if (filtro?.planoId) {
        params = params.set('plano_id', filtro.planoId);
        }

        return this.http.get<any>(this.apiUrl, { params }).pipe(
        map(res => ({
            dados: res.dados ? res.dados.map((item: any) => this.mapearParaFrontend(item)) : [],
            pagina: res.pagina,
            tamanho: res.tamanho,
            total: res.total
        }))
        );
    }

    atualizar(id: string, dados: BeneficiarioAtualizacaoRequest): Observable<Beneficiario>{
        return this.http.put<any>(`${this.apiUrl}/${id}`, dados).pipe(map(res => this.mapearParaFrontend(res)));
    }

    obterPorId(id: string): Observable<Beneficiario> {
        return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
        map(res => this.mapearParaFrontend(res))
        );
    }

    criar(beneficiario: BeneficiarioCriacaoRequest): Observable<Beneficiario> {
        const payload = {
        nome_completo: beneficiario.nomeCompleto,
        cpf: beneficiario.cpf,
        data_nascimento: beneficiario.dataNascimento,
        plano_id: beneficiario.planoId
        };
        return this.http.post<any>(this.apiUrl, payload).pipe(map(res => this.mapearParaFrontend(res)));
    }
    deletar(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }


    private mapearParaFrontend(res: any): Beneficiario {
        return {
        id: res.id,
        nomeCompleto: res.nome_completo,
        cpf: res.cpf,
        nomePlano: res.nome_plano,
        dataNascimento: res.data_nascimento,
        planoId: res.plano_id,
        status: res.status,
        dataCadastro: res.data_cadastro
        };
    }
}