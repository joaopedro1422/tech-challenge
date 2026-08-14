using Desafio.Api.Api.Contratos;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext db, PlanoServico planoServico)
{
    private const string CodigoViolacaoDeUnicidade = "23505";

    public async Task<Beneficiario> CriarAsync(BeneficiarioRequest dados, CancellationToken cancellationToken)
    {
        var beneficiario = new Beneficiario();
        beneficiario.DefinirDados(dados.NomeCompleto, dados.Cpf, dados.DataNascimento, dados.PlanoId);

        await VerificaCpfExistente(beneficiario.Cpf, cancellationToken);
        await VerificaPlanoExistente(beneficiario.PlanoId, cancellationToken);

        db.Beneficiarios.Add(beneficiario);
        await SalvarAsync(cancellationToken);
       
        return beneficiario;
    }
    public async Task<Beneficiario> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Beneficiarios.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
               ?? throw new NaoEncontradoException("Beneficiario não encontrado para este ID");
    }

    private async Task VerificaCpfExistente(string cpf, CancellationToken cancellationToken)
    {
        var registro = await db.Beneficiarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.Cpf == cpf)
            .FirstOrDefaultAsync(cancellationToken);
        if(registro != null)
        {
            throw new ConflitoException("Já existe um Beneficiário com o CPF informado"); 
        }
    }

    private async Task VerificaPlanoExistente(Guid planoId, CancellationToken cancellationToken)
    {
        try
        {
            await planoServico.ObterAsync(planoId, cancellationToken);
        }
        catch (NaoEncontradoException)
        {
            throw new NaoProcessavelException("O plano informado não existe ou foi excluído"
        );
        }
    }
    
    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeUnicidade(excecao))
        {
            throw new ConflitoException("Já existe um Beneficiário com o CPF informado");
        }
    }
    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao) =>
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == CodigoViolacaoDeUnicidade;

}

