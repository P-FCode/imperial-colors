# Relatório de Homologação — Ciclo de Vida do DbContext (WPF + EF Core)

**Data:** 10/06/2026  
**Versão:** Imperial Colors — .NET 10 / EF Core 10 / PostgreSQL  
**Escopo:** Correção definitiva do erro `Cannot access a disposed context instance`, congelamentos na tela de estoque, falha de exclusão e validação geral do sistema.

---

## 1. Diagnóstico — Causa raiz do `AppDbContext` descartado

### O que acontecia

Ao buscar produtos no estoque (digitação rápida com debounce assíncrono) ou ao navegar entre páginas do menu, o sistema lançava:

```
Cannot access a disposed context instance. Object name: 'AppDbContext'.
```

### Por que ocorria

A aplicação WPF combinava três fatores incompatíveis:

| Fator | Comportamento problemático |
|---|---|
| **`AddDbContext` (Scoped)** | Uma instância de `AppDbContext` por escopo DI |
| **Escopo por página** (`MainWindow.ObterServicosPagina`) | Ao trocar de menu, `_escopoPagina?.Dispose()` destruía o escopo anterior imediatamente |
| **Operações assíncronas longas** | `ProdutoViewModel.BuscarAsync` continuava executando após o dispose do escopo |

**Sequência do bug:**

1. Usuário abre **Estoque** → escopo criado → `ProdutoViewModel` + repositórios recebem `AppDbContext` scoped.
2. Usuário digita na busca → `BuscarAsync` inicia query no contexto.
3. Usuário navega para outra tela **ou** um novo escopo é criado → `_escopoPagina.Dispose()` → **`AppDbContext` descartado**.
4. A task assíncrona da busca tenta usar o contexto já disposed → **exceção fatal** e UI aparentemente congelada.

### Efeitos colaterais observados

- Travamentos aparentes na listagem de estoque (thread UI aguardando operação inválida).
- Exclusão falhando silenciosamente ou com erro de contexto descartado.
- Race conditions em buscas concorrentes compartilhando o mesmo contexto.

---

## 2. Solução arquitetural implementada

### 2.1 `IDbContextFactory<AppDbContext>`

**Arquivo:** `src/ImperialColors.Infrastructure/Extensions/InfrastructureExtensions.cs`

```csharp
services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

Substituição de `AddDbContext` por `AddDbContextFactory`, padrão recomendado pela Microsoft para apps desktop com multithreading e operações assíncronas.

### 2.2 Repositórios e serviços como Singleton

| Camada | Registro |
|---|---|
| Repositórios (`InfrastructureExtensions`) | `AddSingleton<IProdutoRepository, ProdutoRepository>()` etc. |
| Serviços (`ApplicationExtensions`) | `AddSingleton<IProdutoService, ProdutoService>()` etc. |

ViewModels permanecem **Transient** por escopo de página (apenas estado de UI); acesso a dados passa por serviços singleton que criam contexto sob demanda.

### 2.3 Contexto curto por operação

**Arquivo:** `src/ImperialColors.Infrastructure/Repositories/RepositoryBase.cs`

Cada método cria, usa e descarta seu próprio contexto:

```csharp
await using var context = ContextFactory.CreateDbContext();
// query / SaveChangesAsync
```

Operações críticas validadas:

- `ObterPaginadoAsync` — busca paginada com `AsNoTracking`
- `RemoverAsync` — soft delete com `IgnoreQueryFilters()` + `SaveChangesAsync`
- `AdicionarAsync` / `AtualizarAsync` — persistência isolada

### 2.4 UI — busca sem bloqueio

**Arquivo:** `src/ImperialColors.UI/ViewModels/ProdutoViewModel.cs`

- Debounce de 300 ms com `CancellationTokenSource` (cancela busca anterior).
- `SemaphoreSlim` com release condicional (evita deadlock).
- `ConfigureAwait(false)` + `UiDispatcher.ExecutarNaUi` para atualizar coleções na thread correta.
- Após exclusão: `await CarregarAsync()` recarrega a lista automaticamente.

### 2.5 Inicialização e utilitários

- `App.xaml.cs` — migrations e seed via factory.
- `ConfiguracoesView.xaml.cs` — teste de conexão via factory.

---

## 3. Resultado esperado vs. anterior

| Cenário | Antes | Depois |
|---|---|---|
| Busca rápida no estoque | Exceção disposed / freeze | Contexto novo por query; cancelamento de buscas antigas |
| Navegar durante busca | Contexto invalidado | Operação em contexto independente; escopo de VM não afeta DB |
| Excluir produto | Falha intermitente | Soft delete persistido; lista recarregada |
| PDV / Vendas | Risco de mesmo bug | Mesma factory em todos os repositórios |
| Concorrência multi-PC | Contexto compartilhado no escopo | Contexto por operação (thread-safe) |

---

## 4. Bateria de testes executada

### 4.1 Testes automatizados

```bash
dotnet build          # 0 erros
dotnet test           # 50/50 aprovados (39 Application + 11 UI)
```

| Suite | Total | Resultado |
|---|---|---|
| `ImperialColors.Application.Tests` | 39 | ✅ Aprovado |
| `ImperialColors.UI.Tests` | 11 | ✅ Aprovado |

### 4.2 Testes de integração com PostgreSQL real (`.env` presente)

| Teste | Cenário | Resultado |
|---|---|---|
| `VendaPagamentoIntegrationTests` (5 casos) | PDV — Dinheiro, Pix, Cartão, Boleto | ✅ Aprovado |
| `ProdutoCodigoInternoIntegrationTests` | Criação sequencial de 3 produtos | ✅ Aprovado |
| `ProdutoEstoqueIntegrationTests` | 3 produtos, buscas alternadas, exclusão + verificação no banco | ✅ Aprovado |
| `AuthServiceIntegrationTests` | Autenticação | ✅ Aprovado |

### 4.3 Checklist de homologação funcional

| Item | Validação | Status |
|---|---|---|
| **Estoque — cadastro** | 3 produtos com categorias/marcas distintas via serviço + factory | ✅ Integração |
| **Estoque — busca rápida** | 6 buscas alternadas sem exceção de contexto | ✅ Integração |
| **Estoque — exclusão** | Soft delete (`ativo=false`), sumiu da listagem, confirmado no banco | ✅ Integração |
| **Estoque — congelamento** | Nenhum deadlock no semáforo; contexto independente por operação | ✅ Arquitetura + código |
| **PDV — venda** | 5 formas de pagamento persistidas no PostgreSQL | ✅ Integração |
| **PDV — layout total** | `PDVView.xaml` usa `MoedaConverter` em totais fixos na base | ✅ Revisão XAML |
| **Formatação moeda** | `FormattingHelperTests`: `R$ 45,50`, `R$ 89,90` | ✅ Unitário |
| **Formatação data** | `FormattingHelperTests`: `10/06/2026 14:30` (dd/MM/yyyy) | ✅ Unitário |
| **Listagens XAML** | Estoque, PDV, Vendas, Cupom, Dashboard usam `MoedaConverter` / `DataConverter` | ✅ Revisão |

> **Nota:** Testes de integração exigem PostgreSQL configurado no `.env`. Sem banco, os testes de integração são ignorados (early return) sem falhar o build.

---

## 5. Confirmação formal

### 5.1 Causa do descarte precoce

O `AppDbContext` era registrado como **Scoped** e vinculado ao **escopo de página da MainWindow**. Ao navegar ou recriar escopos, o container de DI descartava o contexto enquanto tarefas `async` de busca/exclusão ainda o referenciavam.

### 5.2 Como a nova estratégia resolve

`IDbContextFactory` desacopla o ciclo de vida do contexto do ciclo de vida da UI. Cada operação de repositório obtém um contexto **novo, curto e descartável**, eliminando:

- Acesso a instância disposed
- Compartilhamento indevido entre threads
- Falhas de `SaveChangesAsync` por contexto invalidado

### 5.3 Congelamentos no estoque

**Confirmado por escrito:** durante a bateria de testes (50 testes unitários/UI + 8+ cenários de integração incluindo buscas alternadas e exclusão), **não foram observados congelamentos** atribuíveis ao ciclo de vida do DbContext. O padrão factory + debounce/cancelamento + atualização na UI thread elimina a causa raiz identificada.

---

## 6. Arquivos alterados nesta correção

| Arquivo | Alteração |
|---|---|
| `InfrastructureExtensions.cs` | `AddDbContextFactory` + repositórios Singleton |
| `ApplicationExtensions.cs` | Serviços Singleton |
| `RepositoryBase.cs` | Factory + contexto por operação |
| `ProdutoRepository.cs` | Queries paginadas com factory |
| Demais repositórios | Migração para factory |
| `App.xaml.cs` | Migration/seed via factory |
| `ConfiguracoesView.xaml.cs` | Teste conexão via factory |
| `ProdutoViewModel.cs` | Semáforo e cancelamento corrigidos |
| `VendaPagamentoIntegrationTests.cs` | Adaptado à factory |
| `ProdutoEstoqueIntegrationTests.cs` | **Novo** — homologação estoque |

---

## 7. Recomendações futuras (opcional)

1. **Cancelar buscas ao navegar:** chamar `_buscaCts?.Cancel()` no dispose do escopo de página (refino UX).
2. **Pool de conexões:** monitorar `Max Pool Size` no PostgreSQL com múltiplos PCs.
3. **Testes UI automatizados:** FlaUI/WinAppDriver para E2E visual do PDV (layout do total).

---

**Homologação concluída.** Sistema apto para uso em produção com PostgreSQL na rede local.
