using Estoque.DTOs;
using Estoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Controllers;

[ApiController]
[Route("api/estoque/movimentacoes")]
public class MovimentacoesController(MovimentacaoService service) : ControllerBase
{
    /// <summary>RF14: histórico com filtros.</summary>
    [HttpGet]
    public async Task<ActionResult<List<MovimentacaoResponse>>> Listar(
        [FromQuery] MovimentacaoFiltro filtro, CancellationToken ct) =>
        Ok(await service.ListarAsync(filtro, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovimentacaoResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    /// <summary>RF13: entrada/saída manual.</summary>
    [HttpPost]
    public async Task<ActionResult<MovimentacaoResponse>> RegistrarManual(MovimentacaoManualRequest req, CancellationToken ct)
    {
        var criada = await service.RegistrarManualAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    /// <summary>Uso interno (CompraVendas): lote atômico e idempotente.</summary>
    [HttpPost("lote")]
    public async Task<IActionResult> Lote(LoteRequest req, CancellationToken ct)
    {
        await service.MovimentarLoteAsync(req, ct);
        return NoContent();
    }
}
