# Relatório — Exclusão, Travamentos e Performance do Estoque

**Data:** 10/06/2026  
**Módulos:** Estoque / Produtos / Infrastructure  
**Status:** Concluído

---

## 1. Exclusão de produtos — diagnóstico e correção

### Estratégia adotada: **Soft Delete** (`ativo = false`)

O sistema já utilizava exclusão lógica (`RepositoryBase.RemoverAsync` → `Ativo = false` + `SaveChangesAsync`). A linha **permanece no PostgreSQL** com `ativo = false` — isso é intencional para preservar histórico de vendas e movimentações.

### Problemas encontrados

| Problema | Impacto |
|---------|---------|
| Filtro `Ativo` manual em cada repositório | Consultas inconsistentes; risco de produto “inativo” aparecer em algum fluxo |
| `RemoverAsync` buscava entidade **com** filtro ativo | Falha silenciosa se entidade já estivesse inativa |
| Usuário via linha no pgAdmin | Esperava hard delete, mas o dado estava correto como soft delete |

### Correções

1. **Global Query Filter** em `AppDbContext` via `SoftDeleteQueryFilterExtensions` — todas as entidades `BaseEntity` recebem automaticamente `WHERE ativo = true`.
2. **`RemoverAsync` reforçado** — usa `IgnoreQueryFilters()` para localizar o registro, define `Ativo = false`, persiste com `SaveChangesAsync()`.
3. **`ProdutoService.RemoverAsync`** — valida pós-exclusão; se o produto ainda aparecer nas consultas normais, lança erro explícito.

### Validação no pgAdmin

```sql
SELECT id, nome, codigo_interno, ativo, atualizado_em
FROM produtos
WHERE id = <id_excluido>;
-- Esperado: ativo = false, atualizado_em preenchido
```

---

## 2. Fim dos congelamentos na tela de Estoque

### Causa raiz

| Causa | Detalhe |
|-------|---------|
| **Atualização de UI fora da UI Thread** | Após `await` na busca, `ObservableCollection` era alterada na thread pool → comportamento instável/freezing no WPF |
| **Busca sem controle de concorrência** | Cada tecla disparava nova consulta; múltiplas queries simultâneas ao PostgreSQL |
| **Carregamento completo na memória** | `ObterTodosAsync()` trazia todos os produtos — lento e pesado conforme a base cresce |

### Correções em `ProdutoViewModel` e `EstoqueView`

- `SemaphoreSlim` serializa buscas (debounce 300 ms mantido)
- Atualizações de `Produtos`, `TotalProdutos` e `Carregando` via `UiDispatcher.ExecutarNaUi`
- **Paginação** de 50 itens por página (`ObterPaginadoAsync`)
- Overlay “Carregando...” sem bloquear a janela principal
- Botão **⟳ Atualizar** para recarregar manualmente
- Varredura confirmada: **zero** uso de `.Wait()`, `.Result` ou `.GetAwaiter().GetResult()` no projeto

---

## 3. Otimização para alta volumetria

### Índices — Migration `20260610154254_AddPerformanceIndexesAndSoftDelete`

| Tabela | Índice novo | Coluna | Finalidade |
|--------|-------------|--------|------------|
| `produtos` | `IX_produtos_nome` | `nome` | Busca por nome (ILIKE) |
| `produtos` | `IX_produtos_codigo_interno` | `codigo_interno` | Já existia (UNIQUE) |
| `produtos` | `IX_produtos_codigo_barras` | `codigo_barras` | Já existia |
| `vendas` | `IX_vendas_data_venda` | `data_venda` | Relatórios por período |
| `movimentacoes_estoque` | `IX_movimentacoes_estoque_criado_em` | `criado_em` | Histórico temporal |
| `movimentacoes_estoque` | `IX_movimentacoes_estoque_produto_id` | `produto_id` | Já existia (FK) |

### Paginação

```csharp
.Skip((pagina - 1) * itensPorPagina).Take(itensPorPagina)
```

Implementado em `ProdutoRepository.ObterPaginadoAsync` → `ProdutoService.ObterPaginadoAsync` → `ProdutoViewModel`.

### AsNoTracking

Aplicado em consultas de leitura em:

- `ProdutoRepository` (listagem, busca, paginação)
- `RepositoryBase.ObterTodosAsync` / `BuscarAsync`
- `VendaRepository` (relatórios, histórico, totais)

---

## 4. Arquivos alterados

```
src/ImperialColors.Infrastructure/Data/AppDbContext.cs
src/ImperialColors.Infrastructure/Data/SoftDeleteQueryFilterExtensions.cs
src/ImperialColors.Infrastructure/Repositories/RepositoryBase.cs
src/ImperialColors.Infrastructure/Repositories/ProdutoRepository.cs
src/ImperialColors.Infrastructure/Repositories/VendaRepository.cs
src/ImperialColors.Infrastructure/Data/Mappings/ProdutoMapping.cs
src/ImperialColors.Infrastructure/Data/Mappings/VendaMapping.cs
src/ImperialColors.Infrastructure/Data/Mappings/MovimentacaoEstoqueMapping.cs
src/ImperialColors.Infrastructure/Migrations/20260610154254_AddPerformanceIndexesAndSoftDelete.cs
src/ImperialColors.Application/DTOs/PaginacaoResultadoDto.cs
src/ImperialColors.Application/Services/ProdutoService.cs
src/ImperialColors.Application/Interfaces/IProdutoService.cs
src/ImperialColors.Domain/Interfaces/IProdutoRepository.cs
src/ImperialColors.UI/ViewModels/ProdutoViewModel.cs
src/ImperialColors.UI/Views/EstoqueView.xaml
src/ImperialColors.UI/Converters/Converters.cs
tests/ImperialColors.Application.Tests/ProdutoPaginacaoTests.cs
```

---

## 5. Como validar

1. Execute a migration: `dotnet ef database update` (ou reinicie o app — auto-migrate no startup)
2. Exclua um produto → confira `ativo = false` no pgAdmin
3. Reabra Estoque → produto não aparece
4. Clique repetidamente em **⟳ Atualizar** e digite na busca → tela permanece responsiva
5. Navegue **◀ Anterior / Próxima ▶** com bases grandes

---

## 6. Testes automatizados

```bash
dotnet test
```

Inclui `ProdutoPaginacaoTests` e suite completa (49+ testes).
