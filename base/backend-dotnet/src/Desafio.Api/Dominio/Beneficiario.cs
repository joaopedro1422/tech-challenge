using System.Text.RegularExpressions;
using Desafio.Api.Api.Contratos;

namespace Desafio.Api.Dominio;

public enum StatusBeneficiario
{
    ATIVO,
    INATIVO
}

public class Beneficiario
{
    public Beneficiario()
    {
        Id = Guid.NewGuid();
        Status = StatusBeneficiario.ATIVO;
        DataCadastro = DateTime.UtcNow;
    }
    public Guid Id { get; set; }

    public string NomeCompleto { get; set; } = null!;

    public string Cpf { get; set; } = null!;

    public DateOnly DataNascimento { get; set; }

    public StatusBeneficiario Status { get; set; }

    public Guid PlanoId { get; set; }

    public Plano? Plano { get; set; }

    public DateTime DataCadastro { get; set; }

    public void DefinirDados(string? nomeCompleto, string? cpf, DateOnly? dataNascimento, Guid? planoId)
    {
        nomeCompleto = nomeCompleto?.Trim() ?? string.Empty;
        cpf = cpf?.Trim() ?? string.Empty;

        var detalhes = new List<DetalheErro>();

        if (nomeCompleto.Length == 0)
        {
            detalhes.Add(new DetalheErro("nome_completo", "obrigatorio"));
        }
        else if (nomeCompleto.Length is < 3 or > 120)
        {
            detalhes.Add(new DetalheErro("nome_completo", "tamanho_invalido"));
        }

        if (cpf.Length == 0)
        {
            detalhes.Add(new DetalheErro("cpf", "obrigatorio"));
        }
        else if (!ValidarCpf(cpf))
        {
            detalhes.Add(new DetalheErro("cpf", "invalido"));
        }
        if (!dataNascimento.HasValue)
        {
            detalhes.Add(new DetalheErro("data_nascimento", "obrigatorio"));
        }
        else if (dataNascimento.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            detalhes.Add(new DetalheErro("data_nascimento", "invalido"));
        }
        if (!planoId.HasValue || planoId.Value == Guid.Empty)
        {
            detalhes.Add(new DetalheErro("plano_id", "obrigatorio"));
        }

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException("Dados do beneficiário inválidos", detalhes);
        }
    }
    //Algoritmo oficial do ministério da Fazenda
    private static bool ValidarCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            return false;

        if (cpf.Distinct().Count() == 1)
            return false;

        int[] tempCpf = cpf.Select(c => c - '0').ToArray();

        int soma1 = 0;
        for (int i = 0; i < 9; i++)
            soma1 += tempCpf[i] * (10 - i);
        
        int resto1 = soma1 % 11;
        int dv1 = resto1 < 2 ? 0 : 11 - resto1;

        if (tempCpf[9] != dv1)
            return false;

        int soma2 = 0;
        for (int i = 0; i < 10; i++)
            soma2 += tempCpf[i] * (11 - i);
        int resto2 = soma2 % 11;
        int dv2 = resto2 < 2 ? 0 : 11 - resto2;
        return tempCpf[10] == dv2;
    }
  
}
