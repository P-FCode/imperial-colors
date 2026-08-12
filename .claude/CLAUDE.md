# CLAUDE.md - Guia do Projeto Imperial Colors

Este arquivo contém diretrizes essenciais, convenções de código, regras de negócio e comandos operacionais do projeto para auxiliar agentes de IA durante o desenvolvimento.

---

## 🏗️ Padrões Arquiteturais

- **`IDbContextFactory<AppDbContext>`**: Em vez de injetar `AppDbContext` diretamente em instâncias scoped/singleton de vida longa, utilize `IDbContextFactory<AppDbContext>`. Cada repositório ou operação abre e descarta seu próprio contexto de vida curta (`using var context = dbContextFactory.CreateDbContext()`), evitando problemas de concorrência e rastreamento (change tracking) no Entity Framework Core.
- **Soft-Delete**: A inativação de registros é feita via propriedade `Ativo` com Query Filter Global configurado no `AppDbContext`.
- **Concorrência entre PDVs**: Utilização de **Advisory Lock do PostgreSQL** para controle estrito de concorrência e integridade nas operações simultâneas entre múltiplos PDVs.

---

## 🧪 Testes de Integração

- **Variável de Ambiente**: Para executar os testes de integração, a variável `RUN_INTEGRATION_TESTS=true` deve estar definida (necessita da instância do PostgreSQL ativa conforme as credenciais configuradas no `.env`).
- **Isolamento e Flakiness**: Testes que manipulam a configuração fiscal global **devem** utilizar o atributo de coleção:
  ```csharp
  [Collection(ConfiguracaoFiscalGlobalCollection.Nome)]
  ```
  Isso previne a execução paralela e evita falhas intermitentes (*flaky tests*).

---

## 🔄 Fluxo de Migrations (EF Core)

Para adicionar novas migrations ao banco de dados, utilize sempre a seguinte estrutura de comando na raiz da solução:

```powershell
dotnet ef migrations add <NomeDaMigration> --project src/ImperialColors.Infrastructure --startup-project src/ImperialColors.UI --context AppDbContext
```

---

## 🌐 Integração Externa: API-NF Fiscal

Os contratos e especificações da API de Nota Fiscal do cliente estão localizados fora deste repositório nos seguintes caminhos locais:

- **Explicação detalhada dos campos da nota fiscal e regras de preenchimento**:
  `C:\Users\Windows\Desktop\Projetos\PFCode\API-NF\GUIA_INTEGRACAO.md`
- **Documentação de rotas, endpoints e contratos da API**:
  `C:\Users\Windows\Desktop\Projetos\PFCode\API-NF\README.md`

---

## 📜 Regras de Negócio e Domínio Fiscal

- **CRT (Código de Regime Tributário)**: Possui 4 valores válidos:
  - `1`: Simples Nacional
  - `2`: Simples Nacional - excesso de sublimite de receita bruta
  - `3`: Regime Normal
  - `4`: Simples Nacional - Microempreendedor Individual (MEI)
- **CST × CSOSN**: São mutuamente exclusivos dependendo do CRT da empresa:
  - CRT `1` e `4`: Utilizam **CSOSN**.
  - CRT `2` e `3`: Utilizam **CST**.
- **CFOP**: Respeita rigorosamente a origem e o destino da operação pelo primeiro dígito do código:
  - `5.xxx`: Operações internas (dentro do mesmo estado).
  - `6.xxx`: Operações interestaduais (para outros estados).
  - `7.xxx`: Operações com o exterior.

---

## 🏷️ Convenções de Nomenclatura e Arquitetura

- **Idioma**: Nomes de classes, métodos, propriedades, variáveis e documentos devem ser em **Português (PT-BR)**.
- **Fluxo de Dados em Camadas**: Siga estritamente a cadeia de responsabilidades:
  ```
  DTO ──> Validator (FluentValidation) ──> Service (Application) ──> UI (Views/ViewModels)
  ```
