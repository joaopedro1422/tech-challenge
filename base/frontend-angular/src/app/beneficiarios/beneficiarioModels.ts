export type StatusBeneficiario = 'ATIVO' | 'INATIVO';

export interface Beneficiario {
  id?: string;
  nomeCompleto: string;
  cpf: string;
  dataNascimento: string; 
  planoId: string;
  status: StatusBeneficiario;
  dataCadastro?: string;
}

export interface BeneficiarioCriacaoRequest{
  nomeCompleto: string;
  cpf: string;
  dataNascimento: string;
  planoId: string;
}

export interface BeneficiarioAtualizacaoRequest{
  nomeCompleto: string;
  status: StatusBeneficiario;
  dataNascimento: string;
  planoId: string;
}