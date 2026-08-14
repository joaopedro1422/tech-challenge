using System.Text.Json.Serialization;
using Desafio.Api.Dominio;

namespace Desafio.Api.Api.Contratos;

public sealed record BeneficiarioRequest(string? NomeCompleto,
                                        string? Cpf,
                                        DateOnly? DataNascimento,
                                        Guid? PlanoId,
                                        string? Status);

public sealed record BeneficiarioResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("nome_completo")] string NomeCompleto,
    [property: JsonPropertyName("cpf")] string Cpf,
    [property: JsonPropertyName("data_nascimento")] string DataNascimento,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("plano_id")] Guid PlanoId,
    [property: JsonPropertyName("nome_plano")] string NomePlano
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