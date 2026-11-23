using Microsoft.AspNetCore.Diagnostics;

namespace Estoque.Exceptions;

public class TratadorDeExcecoes(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var status = ex switch
        {
            NaoEncontradoException => StatusCodes.Status404NotFound,
            ConflitoException => StatusCodes.Status409Conflict,
            NegocioException => StatusCodes.Status422UnprocessableEntity,
            _ => 0
        };
        if (status == 0) return false;

        ctx.Response.StatusCode = status;
        return await problems.TryWriteAsync(new()
        {
            HttpContext = ctx,
            Exception = ex,
            ProblemDetails = { Status = status, Detail = ex.Message }
        });
    }
}