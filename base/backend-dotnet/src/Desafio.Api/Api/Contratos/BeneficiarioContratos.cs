using System.Text.Json.Serialization;
using Desafio.Api.Dominio;

namespace Desafio.Api.Api.Contratos;

public sealed record BeneficiarioRequest(string? NomeCompleto,
                                        string? Cpf,
                                        DateOnly? DataNascimento,
                                        Guid? PlanoId);
                                        
public sealed record BeneficiarioAtualizacaoRequest(string? NomeCompleto,
                                        DateOnly? DataNascimento,
                                        Guid? PlanoId,
                                        string? Status);


public sealed record BeneficiarioResponse(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    string DataNascimento,
    string Status,
    Guid PlanoId,
    string NomePlano
)
{
    public static BeneficiarioResponse De(Beneficiario beneficiario) =>
        new(
            beneficiario.Id,
            beneficiario.NomeCompleto,
            beneficiario.Cpf,
            beneficiario.DataNascimento.ToString("yyyy-MM-dd"),
            beneficiario.Status.ToString(),
            beneficiario.PlanoId,
            beneficiario.Plano.Nome
        );
}

public record ListaPaginada<T>(IEnumerable<T> Dados, int Pagina, int Tamanho, int Total);