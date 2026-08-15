import { HttpClient } from "@angular/common/http";
import { Beneficiario, BeneficiarioCriacaoRequest } from "./beneficiarioModels";
import { map, Observable } from "rxjs";

export class BeneficiarioService {
    private readonly apiUrl = 'http://localhost:5000/beneficiarios';
    constructor(private http: HttpClient) {}
    listar(): Observable<Beneficiario[]> {
        return this.http.get<any[]>(this.apiUrl).pipe(map(lista => lista.map(item => this.mapearParaFrontend(item))));
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


    private mapearParaFrontend(res: any): Beneficiario {
        return {
        id: res.id,
        nomeCompleto: res.nome_completo,
        cpf: res.cpf,
        dataNascimento: res.data_nascimento,
        planoId: res.plano_id,
        status: res.status,
        dataCadastro: res.data_cadastro
        };
    }
}