using Desafio.Api.Api.Contratos;
using Desafio.Api.Aplicacao;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Api.Controllers;

[ApiController]
[Route("beneficiarios")]
[Produces("application/json")]
public class BeneficiariosController(BeneficiarioServico beneficiarioServico) : ControllerBase
{

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PlanoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await beneficiarioServico.ObterPorIdAsync(id, cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }
    
    [HttpPost]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] BeneficiarioRequest beneficiarioRequest, CancellationToken cancellationToken)
    {
        var beneficiario = await beneficiarioServico.CriarAsync(beneficiarioRequest, cancellationToken);

        return CreatedAtAction(nameof(Obter), new { id = beneficiario.Id }, BeneficiarioResponse.De(beneficiario));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<PlanoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] BeneficiarioAtualizacaoRequest dados,
        CancellationToken cancellationToken)
    {
        var beneficiario = await beneficiarioServico.AtualizaBeneficiario(id, dados, cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListaPaginada<BeneficiarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        [FromQuery] StatusBeneficiario? status = null,
        [FromQuery(Name = "plano_id")] Guid? planoId = null,
        CancellationToken cancellationToken = default)
    {

        var resultado = await beneficiarioServico.ListarAtivosAsync(pagina, tamanho, status, planoId, cancellationToken);

        return Ok(resultado);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        await beneficiarioServico.ExcluirAsync(id, cancellationToken);

        return NoContent();
    }
}
