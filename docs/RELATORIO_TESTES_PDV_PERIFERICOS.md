# Relatório de Testes — PDV (Pagamentos) e Periféricos

**Data:** 10/06/2026  
**Versão:** 1.2.0  
**Ambiente:** Windows 11 | .NET 10 | PostgreSQL 18 | `imperial_colors`

---

## Resumo Executivo

| Categoria | Total | Aprovados | Falhas |
|-----------|-------|-----------|--------|
| Testes automatizados | 19 | 19 | 0 |
| Build da solução | 1 | 1 | 0 |
| Migration `AddFormaPagamentoVenda` | 1 | 1 | 0 |

---

## Arquivos Alterados / Criados

### Domain
- `src/ImperialColors.Domain/Enums/FormaPagamento.cs` — enum (Dinheiro, Débito, Crédito, Pix, Boleto)
- `src/ImperialColors.Domain/Entities/Venda.cs` — campos de pagamento

### Application
- `src/ImperialColors.Application/Helpers/PagamentoHelper.cs` — cálculo, validação e descrições
- `src/ImperialColors.Application/DTOs/VendaDto.cs` — DTOs atualizados
- `src/ImperialColors.Application/Services/VendaService.cs` — persistência de pagamento
- `src/ImperialColors.Application/Interfaces/IPrinterService.cs`
- `src/ImperialColors.Application/Interfaces/ILocalConfigService.cs`

### Infrastructure
- `src/ImperialColors.Infrastructure/Data/Mappings/VendaMapping.cs`
- `src/ImperialColors.Infrastructure/Services/PrinterService.cs` — listagem Windows
- `src/ImperialColors.Infrastructure/Services/LocalConfigService.cs` — `localsettings.json`
- `src/ImperialColors.Infrastructure/Migrations/20260610134955_AddFormaPagamentoVenda.cs`

### UI
- `src/ImperialColors.UI/Views/FechamentoVendaView.xaml(.cs)` — modal de fechamento
- `src/ImperialColors.UI/Views/PerifericosView.xaml(.cs)` — impressoras e scanner
- `src/ImperialColors.UI/Views/PDVView.xaml.cs` — fluxo de fechamento
- `src/ImperialColors.UI/Views/CupomView.xaml(.cs)` — exibição e impressão
- `src/ImperialColors.UI/Views/ConfiguracoesView.xaml(.cs)` — aba Periféricos
- `src/ImperialColors.UI/Helpers/CupomPrintHelper.cs` — impressão na impressora configurada
- `src/ImperialColors.UI/Services/RelatorioService.cs` — PDF com pagamento

### Testes
- `tests/ImperialColors.Application.Tests/PagamentoHelperTests.cs`
- `tests/ImperialColors.Application.Tests/VendaPagamentoIntegrationTests.cs`
- `tests/ImperialColors.Application.Tests/PrinterServiceTests.cs`

---

## Testes Automatizados

```bash
dotnet test tests/ImperialColors.Application.Tests
```

### PagamentoHelper (4 testes)
| Teste | Resultado |
|-------|-----------|
| Calcular Dinheiro com troco | ✅ |
| Calcular Pix (valor automático) | ✅ |
| Calcular Crédito 3x | ✅ |
| Validar dinheiro insuficiente | ✅ |
| Descrição crédito parcelado | ✅ |

### Venda — Integração PostgreSQL (5 testes)
| Modalidade | forma_pagamento | parcelas | valor_pago | troco | Resultado |
|------------|-----------------|----------|------------|-------|-----------|
| Dinheiro | 1 | 1 | total+50 | 50 | ✅ |
| Cartão Crédito 4x | 3 | 4 | total | 0 | ✅ |
| Pix | 4 | 1 | total | 0 | ✅ |
| Cartão Débito | 2 | 1 | total | 0 | ✅ |
| Boleto | 5 | 1 | total | 0 | ✅ |

### Periféricos (2 testes)
| Teste | Resultado |
|-------|-----------|
| Listar impressoras sem exceção | ✅ |
| Impressora inexistente retorna false | ✅ |

### Autenticação (7 testes — regressão)
| Teste | Resultado |
|-------|-----------|
| Suite AuthService + integração admin | ✅ (7/7) |

---

## Validação Manual Recomendada

1. **PDV → Finalizar Venda** — testar cada forma de pagamento no modal
2. **Cupom** — conferir forma de pagamento, troco (dinheiro) e parcelas (crédito)
3. **Configurações → Periféricos** — selecionar impressora, salvar, imprimir cupom
4. **Scanner** — bipar código no campo de teste (feedback verde)

---

## Colunas adicionadas em `vendas`

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| `forma_pagamento` | integer | Enum FormaPagamento |
| `quantidade_parcelas` | integer | Default 1 |
| `valor_pago` | numeric(10,2) | Valor recebido |
| `troco` | numeric(10,2) | Troco (dinheiro) |

---

## Configuração local de impressora

Arquivo: `localsettings.json` (pasta de saída do executável)

```json
{
  "ImpressoraSelecionada": "Nome da Impressora no Windows"
}
```

---

## Comandos úteis

```bash
dotnet build
dotnet ef database update --project src/ImperialColors.Infrastructure --startup-project src/ImperialColors.UI
dotnet test tests/ImperialColors.Application.Tests
dotnet run --project src/ImperialColors.UI
```
