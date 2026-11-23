# Estoque API

Microsserviço responsável por **produtos (com preço de venda), saldo em estoque e histórico de movimentações**. Recebe entradas e saídas manuais e lotes automáticos vindos do serviço **CompraVendas** (compra confirmada = entrada; venda = saída).

## Requisitos atendidos

| ID | Requisito | Onde |
|----|-----------|------|
| RF12 | Gerenciar produtos (incluindo preço de venda) | `/api/produtos` |
| RF13 | Registrar entradas e saídas manuais | `POST /api/estoque/movimentacoes` |
| RF14 | Consultar quantidades disponíveis e histórico | `GET /api/produtos`, `GET /api/estoque/movimentacoes` |

## Regras de negócio

| ID | Regra | Implementação |
|----|-------|---------------|
| RN16 | A quantidade em estoque nunca pode ficar negativa | Saída usa `UPDATE ... WHERE QuantidadeAtual >= qtd` (sem corrida entre requisições) + `CHECK (QuantidadeAtual >= 0)` no banco |
| RN17 | A origem de uma movimentação é Manual, Compra ou Venda | Enum `OrigemMovimentacao`; endpoint manual força `Manual`; lote aceita só `Compra`/`Venda` |

## Stack

C# · .NET 10 · ASP.NET Core · Entity Framework Core · SQLite · HTTP/REST · OpenAPI/Swagger

## Estrutura

```
Estoque/
├── Controllers/    # Endpoints REST
├── Services/       # Regras de negócio (ProdutoService, MovimentacaoService)
├── DTOs/           # Contratos de entrada e saída
├── Entities/       # Produto, MovimentacaoEstoque, enums
├── Data/           # DbContext e mapeamento EF Core
├── Infra/          # Exceções e tratador global (ProblemDetails)
└── Program.cs
```

## Executando

```bash
dotnet restore
dotnet run
```

Sobe em `http://localhost:5002` (porta esperada pelo CompraVendas). Swagger em `/swagger`. O banco `estoque.db` é criado na primeira execução.

## Endpoints

### Produtos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/produtos?busca=&somenteDisponiveis=` | Lista com quantidade atual |
| GET | `/api/produtos/{id}` | Detalha `{ id, nome, descricao, preco, quantidadeAtual }` |
| POST | `/api/produtos` | Cria com quantidade 0 |
| PUT | `/api/produtos/{id}` | Atualiza nome, descrição e preço (**não** altera quantidade) |
| DELETE | `/api/produtos/{id}` | Exclui (409 se houver histórico) |

A quantidade só muda por movimentação. Para dar saldo inicial a um produto novo, registre uma entrada manual: assim o histórico fica completo.

### Movimentações

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/estoque/movimentacoes?produtoId=&tipo=&origem=&de=&ate=` | Histórico (mais recentes primeiro) |
| GET | `/api/estoque/movimentacoes/{id}` | Detalha |
| POST | `/api/estoque/movimentacoes` | Entrada/saída manual |
| POST | `/api/estoque/movimentacoes/lote` | Lote atômico e idempotente (uso interno do CompraVendas) |

Manual:

```json
{ "produtoId": 3, "tipo": "Entrada", "quantidade": 10, "observacao": "Saldo inicial" }
```

Lote:

```json
{
  "tipo": "Saida",
  "origem": "Venda",
  "origemId": 12,
  "itens": [ { "produtoId": 3, "quantidade": 2 } ]
}
```

Cada movimentação guarda `saldoApos`, o saldo do produto logo depois dela.

## Lote: atomicidade e idempotência

- **Atômico:** todos os itens são aplicados na mesma transação. Se um produto não existir (404) ou não tiver saldo (409), nada é alterado.
- **Idempotente:** a chave é `(tipo, origem, origemId)`. Reenviar o mesmo lote retorna 204 sem alterar o estoque de novo. Um índice único `(tipo, origem, origemId, produtoId)` cobre requisições simultâneas.
- **Estorno:** o CompraVendas desfaz uma movimentação enviando o tipo oposto com a mesma origem e origemId. A chave é diferente da original, então não colide.

## Erros (`ProblemDetails`)

| Status | Situação |
|--------|----------|
| 400 | Validação de DTO (quantidade < 1, enum inválido, etc.) |
| 404 | Produto ou movimentação inexistente |
| 409 | Estoque insuficiente (RN16) ou exclusão bloqueada |
| 422 | Regra de negócio (ex.: lote com origem Manual) |

## Decisões e limitações

- Exclusão de produto é física, bloqueada quando há histórico (estratégia de exclusão ainda em definição).
- Não há estoque mínimo, lotes/validade nem unidades de medida (fora do escopo dos requisitos).
- `EnsureCreated()` deve ser substituído por migrations quando o schema estabilizar.
