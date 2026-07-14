# Relatório — Erro Oculto de Salvamento de Produto

**Data:** 10/06/2026  
**Módulo:** Cadastro de Produtos / Estoque  
**Status:** Corrigido

---

## 1. Sintoma reportado

Ao salvar um produto, o sistema exibia a mensagem genérica do Entity Framework:

> *"An error occurred while saving the entity changes. See the inner exception for details."*

O cadastro era bloqueado sem indicar a causa real.

---

## 2. Mensagem real do banco (InnerException)

Após capturar explicitamente `DbUpdateException` em `RepositoryBase.SalvarAlteracoesAsync()` e extrair a cadeia de `InnerException` via `DatabaseExceptionHelper`, a mensagem real exposta pelo PostgreSQL era equivalente a:

```
Erro real do banco: 23503: insert or update on table "produtos" violates foreign key constraint "FK_produtos_categorias_CategoriaId"
DETAIL: Key (CategoriaId)=(0) is not present in table "categorias".
```

O mesmo padrão ocorria para `MarcaId = 0` quando a marca fictícia era selecionada.

---

## 3. Causa raiz

| Camada | Problema |
|--------|----------|
| **UI (`ProdutoFormView`)** | ComboBoxes recebiam itens placeholder com `Id = 0` ("Sem categoria" / "Sem marca"). |
| **Envio ao serviço** | `CategoriaId` e `MarcaId` eram enviados como `0` ou `null` convertido implicitamente. |
| **PostgreSQL** | FK exige IDs existentes em `categorias` e `marcas` — `0` não existe. |
| **Tratamento de erro** | A UI lia apenas `ex.InnerException?.Message`, ignorando `DomainException.Message` já enriquecida na Infrastructure. |

---

## 4. Correções aplicadas

### 4.1 Infrastructure — exposição do erro real

- `RepositoryBase`: `catch (DbUpdateException)` → `DomainException` com prefixo `"Erro real do banco: {detalhe}"`.
- `DatabaseExceptionHelper`: percorre toda a cadeia de inner exceptions do Npgsql/PostgreSQL.

### 4.2 Application — validação antes do INSERT

- **`ProdutoValidator`**: nome, categoria/marca > 0, custo/venda > 0, estoque ≥ 0.
- **`ProdutoService.ValidarReferenciasCatalogoAsync`**: confirma que categoria e marca existem no banco antes de persistir.
- **`CategoriaService` / `MarcaService`**: cadastro rápido via Clean Architecture.

### 4.3 UI — cadastro rápido e blindagem

- Removidos placeholders `Id = 0` dos ComboBoxes.
- Botões **+** ao lado de Categoria e Marca abrem `NomeRapidoDialogView`.
- Após salvar categoria/marca, lista é recarregada e o novo item fica selecionado.
- Validação na tela com alerta vermelho + `MessageBox` antes de chamar o serviço.
- `ExceptionMessageHelper` prioriza `DomainException.Message` e mensagens reais do banco.

---

## 5. Fluxo validado (ponta a ponta)

1. Abrir **Mercadorias → Novo Produto**.
2. Clicar **+** em Categoria → informar nome → Salvar.
3. Nova categoria aparece selecionada no ComboBox.
4. Repetir para Marca (se necessário).
5. Preencher nome, preços > 0, estoque ≥ 0 → **Salvar Produto**.
6. Produto persiste sem violação de FK.

---

## 6. Testes automatizados

| Projeto | Testes adicionados/ajustados |
|---------|------------------------------|
| `ImperialColors.Application.Tests` | `ProdutoValidatorTests` (6 cenários) |
| `ImperialColors.UI.Tests` | ComboBoxes sem `Id = 0`; mocks atualizados para `ICategoriaService` / `IMarcaService` |

Executar:

```bash
dotnet test
```

---

## 7. Arquivos principais alterados

```
src/ImperialColors.Infrastructure/Repositories/RepositoryBase.cs
src/ImperialColors.Application/Validation/ProdutoValidator.cs
src/ImperialColors.Application/Services/ProdutoService.cs
src/ImperialColors.Application/Services/CategoriaService.cs
src/ImperialColors.Application/Services/MarcaService.cs
src/ImperialColors.Application/Extensions/ApplicationExtensions.cs
src/ImperialColors.UI/Views/ProdutoFormView.xaml(.cs)
src/ImperialColors.UI/Views/NomeRapidoDialogView.xaml(.cs)
src/ImperialColors.UI/Helpers/ExceptionMessageHelper.cs
```

---

## 8. Conclusão

O erro não era um bug do Entity Framework nem do PostgreSQL em si, e sim **dados inválidos (`CategoriaId`/`MarcaId = 0`)** enviados por placeholders na UI, combinados com **mensagem de erro genérica** que ocultava a FK violation.

Com validação em três camadas (UI → Application → verificação de existência) e exposição explícita de `DbUpdateException`, o cadastro de produtos está protegido contra dados corrompidos e o operador recebe feedback claro e acionável.
