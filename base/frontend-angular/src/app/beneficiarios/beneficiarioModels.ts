export type StatusBeneficiario = 'ATIVO' | 'INATIVO';

export interface Beneficiario {
  id?: string;
  nomeCompleto: string;
  cpf: string;
  dataNascimento: string; 
  planoId: string;
  nomePlano : string,
  status: StatusBeneficiario;
  dataCadastro?: string;
}

export interface BeneficiarioCriacaoRequest{
  nomeCompleto: string;
  cpf: string;
  dataNascimento: string;
  planoId: string;
}

export interface ListaPaginadaBeneficiarios{
  dados: Beneficiario[];
  pagina: number;
  tamanho: number;
  total: number;
}

export interface BeneficiarioAtualizacaoRequest{
  nomeCompleto: string;
  status: StatusBeneficiario;
  dataNascimento: string;
  planoId: string;
}
export interface BeneficiarioFiltro {
  pagina?: number;
  tamanho?: number;
  status?: StatusBeneficiario;
  planoId?: string;
}