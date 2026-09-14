# -*- coding: utf-8 -*-
"""
Gera o script SQL de UPDATE do codigo_interno (REF PARANA) dos produtos
Parana Colors, a partir da mesma planilha usada na importacao original.

O script gerado casa cada linha da planilha com o produto ja existente no
banco pela chave natural (nome + unidade + tamanho_embalagem + marca), que e
exatamente a chave usada pelo importar_catalogo_parana.sql.

Uso:
    python tools/gerar_update_ref_parana.py <planilha.xlsx> <saida.sql>
"""
from __future__ import annotations

import collections
import re
import sys
from pathlib import Path

import openpyxl

# Coluna "UN" da planilha -> (unidade, tamanho_embalagem) usados no cadastro.
# Mesmo desmembramento aplicado na importacao original.
UNIDADES = {
    "GL": ("GL", None),
    "LT": ("LT", None),
    "BA": ("BA", None),
    "LA18L": ("LA", "18L"),
    "BD18L": ("BD", "18L"),
    "BD16L": ("BD", "16L"),
    "BA17kg": ("BA", "17 KG"),
    "BA25kg": ("BA", "25 KG"),
}

COL_REF, COL_DESCRICAO, COL_UN = 3, 4, 5


def escapar(valor: str) -> str:
    return valor.replace("'", "''")


def literal(valor: str | None) -> str:
    return "NULL" if valor is None else f"'{escapar(valor)}'"


def ler_planilha(caminho: Path) -> list[dict]:
    planilha = openpyxl.load_workbook(caminho, data_only=True).worksheets[0]
    itens: list[dict] = []
    for numero_linha, linha in enumerate(planilha.iter_rows(min_row=2, values_only=True), start=2):
        descricao = linha[COL_DESCRICAO]
        if descricao in (None, ""):
            continue  # linha de totais no rodape da planilha

        un_bruta = str(linha[COL_UN]).strip()
        if un_bruta not in UNIDADES:
            raise SystemExit(f"Linha {numero_linha}: unidade desconhecida '{un_bruta}'")

        unidade, tamanho = UNIDADES[un_bruta]
        ref = linha[COL_REF]
        if ref in (None, ""):
            raise SystemExit(f"Linha {numero_linha}: REF PARANA vazia")

        itens.append(
            {
                "linha": numero_linha,
                "descricao": str(descricao).strip(),
                "unidade": unidade,
                "tamanho": tamanho,
                "ref": str(int(ref)) if isinstance(ref, float) and ref.is_integer() else str(ref).strip(),
            }
        )
    return itens


def ler_chaves_do_script_original(caminho: Path) -> set[tuple[str, str, str | None]]:
    """Extrai (descricao, unidade, tamanho) das linhas VALUES da importacao original."""
    if not caminho.exists():
        return set()

    padrao = re.compile(
        r"^\('(?P<categoria>[^']*)', '(?P<ncm>[^']*)', '(?P<descricao>(?:[^']|'')*)', "
        r"'(?P<unidade>[^']*)', (?P<tamanho>NULL|'[^']*')",
    )
    chaves = set()
    for linha in caminho.read_text(encoding="utf-8", errors="replace").splitlines():
        casamento = padrao.match(linha.strip())
        if not casamento:
            continue
        tamanho = casamento.group("tamanho")
        chaves.add(
            (
                casamento.group("descricao").replace("''", "'"),
                casamento.group("unidade"),
                None if tamanho == "NULL" else tamanho.strip("'"),
            )
        )
    return chaves


def desambiguar_refs(itens: list[dict]) -> list[dict]:
    """Da um codigo proprio a cada produto que a planilha trouxe com REF repetida.

    codigo_interno tem indice UNIQUE: dois produtos nao podem ficar com a mesma
    REF. Cada ocorrencia repetida recebe um sufixo sequencial na ordem da
    planilha (501010172 -> 5010101721 no primeiro, 5010101722 no segundo), o que
    mantem a REF do fornecedor legivel e ainda separa os produtos.

    Devolve a lista dos itens que foram renomeados, para o relatorio.
    """
    repetidas = collections.Counter(item["ref"] for item in itens)
    sequencia: collections.Counter[str] = collections.Counter()
    ajustados: list[dict] = []

    for item in itens:
        item["ref_planilha"] = item["ref"]
        if repetidas[item["ref_planilha"]] == 1:
            continue

        sequencia[item["ref_planilha"]] += 1
        item["ref"] = f"{item['ref_planilha']}{sequencia[item['ref_planilha']]}"
        ajustados.append(item)

    # O sufixo nao pode esbarrar numa REF que a planilha ja usa em outro produto.
    colisoes = {i["ref"] for i in ajustados} & {i["ref_planilha"] for i in itens}
    if colisoes:
        raise SystemExit(f"Sufixo gerou codigo que ja existe na planilha: {sorted(colisoes)}")

    return ajustados


def validar(
    itens: list[dict],
    chaves_originais: set[tuple[str, str, str | None]],
    ajustados: list[dict],
) -> list[str]:
    avisos: list[str] = []

    chaves_planilha = collections.Counter((i["descricao"], i["unidade"], i["tamanho"]) for i in itens)
    duplicadas = [chave for chave, total in chaves_planilha.items() if total > 1]
    if duplicadas:
        raise SystemExit(f"Chave natural duplicada na planilha: {duplicadas}")

    if chaves_originais:
        somente_planilha = set(chaves_planilha) - chaves_originais
        somente_script = chaves_originais - set(chaves_planilha)
        if somente_planilha or somente_script:
            raise SystemExit(
                "Divergencia entre a planilha e a importacao original.\n"
                f"  So na planilha: {sorted(somente_planilha)}\n"
                f"  So no script:   {sorted(somente_script)}"
            )
        avisos.append(f"{len(chaves_originais)} chaves conferidas contra o importar_catalogo_parana.sql")

    for item in ajustados:
        avisos.append(
            f"REF {item['ref_planilha']} vinha repetida na planilha; "
            f"'{item['descricao']}' ({item['unidade']}) recebeu o codigo {item['ref']}"
        )

    ainda_repetidas = [ref for ref, total in collections.Counter(i["ref"] for i in itens).items() if total > 1]
    if ainda_repetidas:
        raise SystemExit(f"REF ainda repetida depois do ajuste: {sorted(ainda_repetidas)}")

    return avisos


def gerar_bloco_ajustes(ajustados: list[dict]) -> str:
    """Registra no fim do SQL quais codigos ganharam sufixo e como desfazer isso."""
    if not ajustados:
        return ""

    linhas = [
        "-- ============================================================================",
        "-- APENDICE (informativo): codigos que ganharam sufixo",
        "-- ============================================================================",
        "-- A planilha trouxe a MESMA REF PARANA para os produtos abaixo. Como o sistema",
        "-- nao aceita dois produtos com o mesmo codigo, cada um recebeu um sufixo",
        "-- sequencial. Eles JA FORAM aplicados pelo UPDATE acima — esta secao existe so",
        "-- para registro.",
        "--",
        "-- Se o fornecedor confirmar a REF real de algum deles, troque <CODIGO_CORRETO>",
        "-- pelo codigo certo e rode apenas a linha correspondente.",
        "--",
    ]
    for item in ajustados:
        tamanho = item["tamanho"] or "sem tamanho"
        linhas += [
            f"-- {item['descricao']} ({item['unidade']} / {tamanho})",
            f"--   planilha: {item['ref_planilha']}   ->   aplicado: {item['ref']}",
            "-- UPDATE produtos p SET codigo_interno = '<CODIGO_CORRETO>', atualizado_em = LOCALTIMESTAMP",
            "--   FROM marcas m",
            "--  WHERE m.id = p.marca_id AND m.nome = 'Parana colors' AND p.ativo = true",
            f"--    AND p.nome = {literal(item['descricao'])}",
            f"--    AND p.unidade = {literal(item['unidade'])}",
            f"--    AND p.tamanho_embalagem IS NOT DISTINCT FROM {literal(item['tamanho'])};",
            "--",
        ]
    return "\n".join(linhas) + "\n"


def gerar_sql(itens: list[dict], avisos: list[str], ajustados: list[dict]) -> str:
    valores = ",\n".join(
        "({ordem}, {descricao}, {unidade}, {tamanho}, {ref})".format(
            ordem=indice,
            descricao=literal(item["descricao"]),
            unidade=literal(item["unidade"]),
            tamanho=literal(item["tamanho"]),
            ref=literal(item["ref"]),
        )
        for indice, item in enumerate(itens, start=1)
    )

    cabecalho_avisos = "\n".join(f"--   * {aviso}" for aviso in avisos) or "--   * nenhum"

    return TEMPLATE.format(
        total=len(itens),
        valores=valores,
        avisos=cabecalho_avisos,
        ajustes=gerar_bloco_ajustes(ajustados),
    )


TEMPLATE = r"""-- ============================================================================
-- Atualizacao do "Codigo do produto" (REF PARANA) — Imperial Colors
-- ============================================================================
-- Complementa o importar_catalogo_parana.sql: la os produtos entraram com
-- codigo interno provisorio (PC001, PC002...) porque a coluna REF PARANA tinha
-- sido descartada. Este script troca esse codigo provisorio pela REF PARANA de
-- verdade, que e o "Codigo do produto" que o cliente usa no dia a dia.
--
-- ARQUIVO GERADO AUTOMATICAMENTE a partir de
-- "Tabela Cadastro_Imperial Colors (1).xlsx" ({total} itens).
-- Nao edite a mao: ajuste tools/gerar_update_ref_parana.py e gere de novo.
--
-- Observacoes da geracao:
{avisos}
--
-- COMO ESTE SCRIPT ENCONTRA CADA PRODUTO
--   Nao usa o codigo PC### (que pode ter comecado em outro numero). Usa a
--   chave natural do cadastro: nome + unidade + tamanho da embalagem, sempre
--   dentro da marca "Parana colors". E a mesma chave da importacao original.
--
-- REFs REPETIDAS NA PLANILHA
--   Quando a planilha traz a mesma REF para produtos diferentes, cada um recebe
--   um sufixo sequencial (ex.: 501010172 -> 5010101721 e 5010101722), porque o
--   codigo do produto e unico no sistema. O apendice no fim deste arquivo lista
--   exatamente quais foram e como trocar por outro codigo depois, se o
--   fornecedor confirmar a REF correta.
--
-- SEGURANCA
--   - Roda inteiro dentro de UMA transacao: qualquer erro desfaz TUDO.
--   - So altera produtos da marca "Parana colors" e que estejam ativos.
--   - Nunca sobrescreve um codigo que ja esteja em uso por outro produto
--     (a coluna codigo_interno tem indice UNIQUE); esses casos sao pulados e
--     aparecem no relatorio final, para decisao manual.
--   - Pode ser rodado mais de uma vez: o que ja estiver certo e ignorado.
--   - Nenhuma venda, movimentacao de estoque ou tributacao e afetada: todas
--     apontam para o id do produto, nao para o codigo interno.
--
-- COMO RODAR (maquina do cliente, so com Postgres instalado)
--   1. Copie este arquivo para a maquina, por exemplo C:\atualizar_ref.sql
--   2. Abra o Prompt de Comando (cmd) e rode (ajuste usuario/porta/banco se o
--      .env do cliente for diferente do padrao):
--
--        "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -U postgres -d imperial_colors -f "C:\atualizar_ref.sql"
--
--   3. Informe a senha do Postgres (campo DB_PASSWORD do .env).
--   4. Confira o relatorio impresso no fim. Se alguma linha aparecer como
--      pendente, nada foi quebrado — aquele produto so ficou com o codigo
--      antigo.
--
-- IMPORTANTE: faca um backup antes. Pelo proprio sistema (Configuracoes >
-- Backup) ou por linha de comando:
--   "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" -h localhost -U postgres -d imperial_colors -F c -f "C:\backup_antes_update.backup"
-- ============================================================================

\set ON_ERROR_STOP on
SET client_encoding = 'UTF8';

BEGIN;

-- 1) Planilha carregada numa tabela temporaria (some sozinha no COMMIT) ------
CREATE TEMP TABLE _ref_parana (
    ordem          integer,
    descricao      varchar(200),
    unidade        varchar(10),
    tamanho        varchar(30),
    ref_parana     varchar(50)
) ON COMMIT DROP;

INSERT INTO _ref_parana (ordem, descricao, unidade, tamanho, ref_parana) VALUES
{valores};

-- 2) Monta o plano de atualizacao, classificando cada linha ------------------
--    Aqui nada e alterado ainda: so se decide o que da para fazer.
CREATE TEMP TABLE _plano AS
WITH marca_parana AS (
    SELECT id FROM marcas WHERE nome = 'Parana colors'
),
casamento AS (
    SELECT
        r.ordem,
        r.descricao,
        r.unidade,
        r.tamanho,
        r.ref_parana,
        (SELECT count(*)
           FROM produtos p
          WHERE p.ativo = true
            AND p.marca_id = (SELECT id FROM marca_parana)
            AND p.nome = r.descricao
            AND p.unidade = r.unidade
            AND p.tamanho_embalagem IS NOT DISTINCT FROM r.tamanho) AS produtos_encontrados,
        (SELECT min(p.id)
           FROM produtos p
          WHERE p.ativo = true
            AND p.marca_id = (SELECT id FROM marca_parana)
            AND p.nome = r.descricao
            AND p.unidade = r.unidade
            AND p.tamanho_embalagem IS NOT DISTINCT FROM r.tamanho) AS produto_id,
        (SELECT count(*) FROM _ref_parana d WHERE d.ref_parana = r.ref_parana) AS vezes_na_planilha
    FROM _ref_parana r
)
SELECT
    c.*,
    p.codigo_interno AS codigo_atual,
    CASE
        WHEN c.produtos_encontrados = 0 THEN 'produto nao encontrado no banco'
        WHEN c.produtos_encontrados > 1 THEN 'mais de um produto com o mesmo nome/unidade'
        WHEN p.codigo_interno = c.ref_parana THEN 'ja estava atualizado'
        -- Rede de seguranca: o gerador ja separa REFs repetidas com sufixo, entao
        -- isto so dispara se alguem editar o bloco VALUES a mao.
        WHEN c.vezes_na_planilha > 1 THEN 'REF repetida no bloco VALUES (corrija o arquivo)'
        WHEN EXISTS (
            SELECT 1 FROM produtos outro
             WHERE outro.codigo_interno = c.ref_parana
               AND outro.id <> c.produto_id
        ) THEN 'REF ja usada por outro produto do banco'
        ELSE 'atualizar'
    END AS situacao
FROM casamento c
LEFT JOIN produtos p ON p.id = c.produto_id;

-- 3) Aplica somente o que esta liberado --------------------------------------
UPDATE produtos p
SET codigo_interno = plano.ref_parana,
    atualizado_em  = LOCALTIMESTAMP
FROM _plano plano
WHERE p.id = plano.produto_id
  AND plano.situacao = 'atualizar';

-- 4) Relatorio -----------------------------------------------------------------
\echo ''
\echo '--- Resumo por situacao ---'
SELECT situacao, count(*) AS itens
FROM _plano
GROUP BY situacao
ORDER BY itens DESC;

\echo ''
\echo '--- Itens que NAO foram atualizados (se a lista vier vazia, deu tudo certo) ---'
SELECT ordem, descricao, unidade, tamanho, ref_parana, codigo_atual, situacao
FROM _plano
WHERE situacao NOT IN ('atualizar', 'ja estava atualizado')
ORDER BY ordem;

COMMIT;

\echo ''
\echo '--- Conferencia final: codigos da marca Parana colors no banco ---'
SELECT p.codigo_interno, p.nome, p.unidade, p.tamanho_embalagem
FROM produtos p
JOIN marcas m ON m.id = p.marca_id
WHERE m.nome = 'Parana colors' AND p.ativo = true
ORDER BY p.nome, p.unidade
LIMIT 20;

\echo ''
\echo '--- Atualizacao concluida ---'

{ajustes}"""


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)

    planilha = Path(sys.argv[1])
    saida = Path(sys.argv[2])

    itens = ler_planilha(planilha)
    chaves_originais = ler_chaves_do_script_original(Path("d:/importar_catalogo_parana.sql"))
    ajustados = desambiguar_refs(itens)
    avisos = validar(itens, chaves_originais, ajustados)

    saida.write_text(gerar_sql(itens, avisos, ajustados), encoding="utf-8")

    print(f"{len(itens)} itens gravados em {saida}")
    for aviso in avisos:
        print(f"  aviso: {aviso}")


if __name__ == "__main__":
    main()
