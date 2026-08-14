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

    [HttpGet]
    [ProducesResponseType(typeof(ListaPaginada<BeneficiarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        [FromQuery] StatusBeneficiario? status = null,
        [FromQuery] Guid? planoId = null,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1 || tamanho < 1 || tamanho > 100)
        {
            throw new ValidacaoException("Página deve ser >= 1 e tamanho deve estar entre 1 e 100.");
        }

        var resultado = await beneficiarioServico.ListarAsync(pagina, tamanho, status, planoId, cancellationToken);

        return Ok(resultado);
    }
}
