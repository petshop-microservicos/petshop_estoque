using Estoque.DTOs;
using Estoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController(ProdutoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProdutoResponse>>> Listar(
        [FromQuery] string? busca, [FromQuery] bool somenteDisponiveis, CancellationToken ct) =>
        Ok(await service.ListarAsync(busca, somenteDisponiveis, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ProdutoResponse>> Criar(ProdutoRequest req, CancellationToken ct)
    {
        var criado = await service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = criado.Id }, criado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProdutoResponse>> Atualizar(int id, ProdutoRequest req, CancellationToken ct) =>
        Ok(await service.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }
}
