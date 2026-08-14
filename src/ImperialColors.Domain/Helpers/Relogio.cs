namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Ponto único de verdade para "agora" em tudo que é gravado no banco.
///
/// Todas as colunas de data/hora do sistema são <c>timestamp without time zone</c> (ver
/// <c>AppDbContext.OnModelCreating</c>) — ou seja, guardam um horário sem fuso embutido.
/// Isso só funciona se TODO mundo gravar no MESMO fuso. Antes desta classe existir, metade
/// do sistema usava <c>DateTime.Now</c> (local) e a outra metade <c>DateTime.UtcNow</c>:
/// <c>Venda.DataVenda</c> era local, <c>BaseEntity.CriadoEm</c> era UTC, e as duas iam para
/// colunas do mesmo tipo na mesma linha. Uma venda às 21h30 de 14/08 gravava
/// <c>DataVenda = 14/08 21:30</c> e <c>CriadoEm = 15/08 00:30</c> — qualquer relatório ou
/// tela que lesse <c>CriadoEm</c> mostrava o dia errado nas últimas 3 horas de cada dia.
///
/// O padrão adotado é o horário LOCAL, por três motivos:
/// 1. A legislação fiscal trabalha em horário local — <c>dhEmi</c> da NF-e exige o offset
///    local (-03:00), nunca "Z"/UTC (seção 4.1 do GUIA_INTEGRACAO.md).
/// 2. É o que o operador espera ver na tela: "criado às 21h30" tem que ser 21h30 pra ele.
/// 3. É instalação única, em um único fuso (Curitiba/PR) — não há cenário multi-fuso que
///    justificasse o custo de normalizar tudo em UTC e converter na exibição.
///
/// <b>Nunca use <c>DateTime.UtcNow</c> para gravar no banco.</b> Use <see cref="Agora"/>.
/// </summary>
public static class Relogio
{
    /// <summary>Momento atual no fuso local — use para qualquer campo persistido.</summary>
    public static DateTime Agora => DateTime.Now;

    /// <summary>Data de hoje (meia-noite local) — use para filtros e comparações por dia.</summary>
    public static DateTime Hoje => DateTime.Today;
}
