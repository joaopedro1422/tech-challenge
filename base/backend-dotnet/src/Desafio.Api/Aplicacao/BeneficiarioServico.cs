using Desafio.Api.Api.Contratos;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext db, PlanoServico planoServico, ILogger<BeneficiarioServico> logger)
{
    private const string CodigoViolacaoDeUnicidade = "23505";

    public async Task<Beneficiario> CriarAsync(BeneficiarioRequest dados, CancellationToken cancellationToken)
    {
        var beneficiario = new Beneficiario(dados.NomeCompleto, dados.Cpf, dados.DataNascimento, dados.PlanoId);

        await VerificaCpfExistente(beneficiario.Cpf, cancellationToken);
        await VerificaPlanoExistente(beneficiario.PlanoId, cancellationToken);

        db.Beneficiarios.Add(beneficiario);
        await SalvarAsync(cancellationToken);
        logger.LogInformation("Beneficiario {BeneficiarioId} criado com sucesso", beneficiario.Id);
        return beneficiario;
    }
    public async Task<Beneficiario> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await db.Beneficiarios.Include(b => b.Plano) 
            .FirstOrDefaultAsync(b => b.Id == id && !b.ExcluidoEm.HasValue, cancellationToken);
        if (beneficiario == null)
        {
            logger.LogWarning("Beneficiario {BeneficiarioId} nao foi encontrado", id);
            throw new NaoEncontradoException("Beneficiario não encontrado para este ID");
        }

        return beneficiario;
    }

    public async Task<ListaPaginada<BeneficiarioResponse>> ListarAtivosAsync(int pagina, int tamanho, StatusBeneficiario? status, Guid? planoId, CancellationToken cancellationToken)
    {
        VerificaParametrosPaginacao(pagina, tamanho);

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
            .OrderByDescending(b => b.DataCadastro) 
            .ThenBy(b => b.Id) 
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken);
        var dadosRetorno = dados.Select(BeneficiarioResponse.De).ToList();

        return new ListaPaginada<BeneficiarioResponse>(dadosRetorno, pagina, tamanho, total);
    }
    
    private void VerificaParametrosPaginacao(int pagina , int tamanho)
    {
        var detalhes = new List<DetalheErro>();
        if (pagina < 1)
        {
            detalhes.Add(new DetalheErro("pagina", "invalido"));
        }
        if(tamanho is < 1 or > 100)
        {
            detalhes.Add(new DetalheErro("tamanho", "invalido"));
        }
        if (detalhes.Count > 0)
        {
            throw new ValidacaoException("Parâmetros de paginação inválidos", detalhes);
        }
    }
    
    public async Task<Beneficiario> AtualizaBeneficiario(Guid id, BeneficiarioAtualizacaoRequest dados, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando atualizacao do beneficiario {BeneficiarioId}", id);
        var beneficiario = await ObterPorIdAsync(id, cancellationToken);
   
        if (!Enum.TryParse<StatusBeneficiario>(dados.Status, ignoreCase: true, out var novoStatus))
        {
            throw new ValidacaoException("O Status informado é invalido");
        }
        
        ValidarAlteracaoDeBeneficiarioInativo(beneficiario, novoStatus, dados);

        if (dados.PlanoId.HasValue && dados.PlanoId.Value != beneficiario.PlanoId)
        {
            await VerificaPlanoExistente(dados.PlanoId.Value, cancellationToken);
        }
        beneficiario.DefinirDados(dados.NomeCompleto!.Trim(), beneficiario.Cpf, dados.DataNascimento, dados.PlanoId, dados.Status);
          
        await SalvarAsync(cancellationToken);
        logger.LogInformation("Beneficiario {BeneficiarioId} atualizado com sucesso", id);
        return beneficiario;
    }

    private void ValidarAlteracaoDeBeneficiarioInativo( Beneficiario dadosAntigos,StatusBeneficiario novoStatus, BeneficiarioAtualizacaoRequest dadosAtuais)
    {
        // Se o beneficiario está INATIVO e o corpo de atualização ainda o mantém INATIVO não pode haver mudanças. Caso o corpo traga status Ativo, não executa nada abaixo
        if (dadosAntigos.Status == StatusBeneficiario.INATIVO && novoStatus == StatusBeneficiario.INATIVO )
        {
            bool alterouDadosCadastrais = 
                dadosAntigos.NomeCompleto != dadosAtuais.NomeCompleto ||
                (dadosAtuais.DataNascimento.HasValue && dadosAntigos.DataNascimento != dadosAtuais.DataNascimento.Value) ||
                (dadosAtuais.PlanoId.HasValue && dadosAntigos.PlanoId != dadosAtuais.PlanoId.Value);

            if (alterouDadosCadastrais )
            {
                throw new ConflitoException("Beneficiários inativos não podem ter seus dados cadastrais alterados.");
            }
        }
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await ObterPorIdAsync(id, cancellationToken);

        beneficiario.Excluir();
        await SalvarAsync(cancellationToken);
        logger.LogInformation("Beneficiario {BeneficiarioId} excluído com sucesso", id);
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
            logger.LogWarning("Conflito de duplicidade detectado para o CPF {cpf}", cpf);
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
            logger.LogWarning("Plano {PlanoId} nao foi encontrado", planoId);
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
            logger.LogError(excecao, "Violacao de constraint de unicidade de CPF ao salvar beneficiário");
            throw new ConflitoException("Já existe um Beneficiário com o CPF informado");
        }
    }
    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao) =>
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == CodigoViolacaoDeUnicidade;

}

