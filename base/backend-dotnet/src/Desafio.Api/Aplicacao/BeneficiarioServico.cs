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
        var beneficiario = new Beneficiario(dados.NomeCompleto, dados.Cpf, dados.DataNascimento, dados.PlanoId);

        await VerificaCpfExistente(beneficiario.Cpf, cancellationToken);
        await VerificaPlanoExistente(beneficiario.PlanoId, cancellationToken);

        db.Beneficiarios.Add(beneficiario);
        await SalvarAsync(cancellationToken);
       
        return beneficiario;
    }
    public async Task<Beneficiario> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Beneficiarios.Include(b => b.Plano) 
                    .FirstOrDefaultAsync(b => b.Id == id && !b.ExcluidoEm.HasValue, cancellationToken)
                    ?? throw new NaoEncontradoException("Beneficiario não encontrado para este ID");
    }

    public async Task<ListaPaginada<BeneficiarioResponse>> ListarAsync(
        int pagina, 
        int tamanho, 
        StatusBeneficiario? status, 
        Guid? planoId, 
        CancellationToken cancellationToken)
    {
        var query = db.Beneficiarios.AsNoTracking().Where(b => !b.ExcluidoEm.HasValue);

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }
        if (planoId.HasValue)
        {
            query = query.Where(b => b.PlanoId == planoId.Value);
        }
        

        // obter a quantidade total de registros validos para o filtro
        var total = await query.CountAsync(cancellationToken);

        var dados = await query
            .Include(b => b.Plano) 
            .OrderBy(b => b.DataCadastro) 
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken);
        var dadosRetorno = dados.Select(BeneficiarioResponse.De).ToList();

        return new ListaPaginada<BeneficiarioResponse>(dadosRetorno, pagina, tamanho, total);
    }

    public async Task<Beneficiario> AtualizaBeneficiario(Guid id, BeneficiarioAtualizacaoRequest dados, CancellationToken cancellationToken)
    {
        var beneficiario = await ObterPorIdAsync(id, cancellationToken);

        if (!Enum.TryParse<StatusBeneficiario>(dados.Status, ignoreCase: true, out var novoStatus))
        {
            throw new ValidacaoException("Status informado é inválido.");
        }

        if (beneficiario.Status == StatusBeneficiario.INATIVO)
        {
            bool alterouDadosCadastrais = 
                beneficiario.NomeCompleto != dados.NomeCompleto ||
                (dados.DataNascimento.HasValue && beneficiario.DataNascimento != dados.DataNascimento.Value) ||
                (dados.PlanoId.HasValue && beneficiario.PlanoId != dados.PlanoId.Value);

            if (alterouDadosCadastrais)
            {
                throw new ConflitoException("Beneficiários inativos não podem ter seus dados cadastrais alterados.");
            }
        }
        if (dados.PlanoId.HasValue && dados.PlanoId.Value != beneficiario.PlanoId)
        {
            await VerificaPlanoExistente(dados.PlanoId.Value, cancellationToken);
            beneficiario.PlanoId = dados.PlanoId.Value;
        }

        if (!string.IsNullOrWhiteSpace(dados.NomeCompleto))
        {
            beneficiario.NomeCompleto = dados.NomeCompleto;
        }
        beneficiario.Status = novoStatus;

        if (dados.DataNascimento.HasValue)
        {
            beneficiario.DataNascimento = dados.DataNascimento.Value;
        }

        await SalvarAsync(cancellationToken);
        return beneficiario;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await ObterPorIdAsync(id, cancellationToken);

        beneficiario.Excluir();
        await SalvarAsync(cancellationToken);
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
            var plano  = await planoServico.ObterAsync(planoId, cancellationToken);
            if (plano.ExcluidoEm.HasValue)
            {
                throw new NaoProcessavelException("O plano informado não existe ou foi excluído");
            }
        }
        catch (NaoEncontradoException)
        {
            throw new NaoProcessavelException("O plano informado não existe ou foi excluído");
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

