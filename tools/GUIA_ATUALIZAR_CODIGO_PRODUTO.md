# Guia — Atualizar o "Código do produto" (REF PARANÁ) no PostgreSQL

Este guia é para rodar o arquivo `atualizar_codigo_produto_parana.sql` na máquina do
cliente, pelo **Prompt de Comando (CMD)** do Windows.

O script troca o código provisório dos produtos da marca **Paraná Colors** (`PC001`,
`PC002`, ...) pela REF PARANÁ da planilha, que é o código que a loja usa no dia a dia.

**Você vai precisar de:** o arquivo `.sql`, a senha do PostgreSQL (campo `DB_PASSWORD`
do arquivo `.env` do sistema) e uns 5 minutos.

---

## Antes de começar

- **Feche o Imperial Colors** em todos os computadores. O script não trava o sistema,
  mas com ele fechado você evita alguém cadastrar produto no meio da atualização.
- O script roda dentro de **uma única transação**: se qualquer linha der erro, o
  PostgreSQL desfaz tudo sozinho e o banco fica exatamente como estava.
- Pode ser rodado **mais de uma vez** sem problema. Na segunda vez ele reconhece o que
  já está certo e não altera nada.

---

## Passo 1 — Copiar o arquivo para a máquina

Copie `atualizar_codigo_produto_parana.sql` para a raiz do disco C:, de forma que o
caminho fique exatamente assim:

```
C:\atualizar_codigo_produto_parana.sql
```

> Usar a raiz do C: evita problemas com acentos e espaços no caminho (a pasta
> `Área de Trabalho`, por exemplo, costuma dar dor de cabeça no CMD).

---

## Passo 2 — Abrir o Prompt de Comando

Pressione **Windows + R**, digite `cmd` e pressione **Enter**.

---

## Passo 3 — Descobrir a versão do PostgreSQL instalada

Os comandos abaixo usam o caminho do PostgreSQL **18**. Confirme a versão instalada
nesta máquina com:

```cmd
dir "C:\Program Files\PostgreSQL"
```

Vai aparecer uma pasta com o número da versão (`18`, `17`, `16`...). **Se não for 18,
troque o número em todos os comandos deste guia.**

---

## Passo 4 — Fazer o backup (não pule esta parte)

```cmd
"C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" -h localhost -U postgres -d imperial_colors -F c -f "C:\backup_antes_update.backup"
```

O terminal vai pedir a senha (`Senha:`). Digite a senha do PostgreSQL e pressione
**Enter** — **os caracteres não aparecem na tela, isso é normal.**

Confirme que o arquivo foi criado e não está vazio:

```cmd
dir "C:\backup_antes_update.backup"
```

Se o tamanho vier como `0` bytes, **pare aqui**: o backup falhou e não é seguro seguir.

---

## Passo 5 — Rodar a atualização

```cmd
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -U postgres -d imperial_colors -f "C:\atualizar_codigo_produto_parana.sql"
```

Digite a senha novamente quando pedir.

---

## Passo 6 — Conferir o resultado

O script imprime um relatório no fim. O resultado **esperado** é este:

```
--- Resumo por situacao ---
 situacao  | itens
-----------+-------
 atualizar |   149

--- Itens que NAO foram atualizados (se a lista vier vazia, deu tudo certo) ---
 ordem | descricao | unidade | tamanho | ref_parana | codigo_atual | situacao
-------+-----------+---------+---------+------------+--------------+----------
(0 linha)
```

Depois vem `COMMIT` e uma amostra dos 20 primeiros códigos já atualizados.

**Como ler o resumo:**

| Situação | O que significa | Ação |
|---|---|---|
| `atualizar` | Código trocado com sucesso | Nada a fazer |
| `ja estava atualizado` | Já estava com a REF certa (normal ao rodar de novo) | Nada a fazer |
| `produto nao encontrado no banco` | O produto da planilha não existe no cadastro | Conferir se a importação rodou |
| `mais de um produto com o mesmo nome/unidade` | Cadastro duplicado no banco | Apagar a duplicata e rodar de novo |
| `REF ja usada por outro produto do banco` | Outro produto (de outra marca) já usa esse código | Decidir manualmente qual fica com o código |

Se a lista de "itens que NÃO foram atualizados" vier **vazia**, deu tudo certo.
Se vier alguma linha, **nada quebrou** — aquele produto apenas continuou com o código
antigo, e você resolve depois pelo próprio sistema (Estoque → editar produto).

---

## Passo 7 — Conferir no sistema

Abra o Imperial Colors, vá em **Estoque** e busque por um produto Paraná Colors. A
coluna **Código** deve mostrar a REF da planilha (ex.: `301010001`) em vez de `PC001`.

---

## Sobre os dois produtos com REF repetida

A planilha traz a **mesma REF `501010172`** para dois produtos diferentes. Como o
sistema não aceita dois produtos com o mesmo código, cada um recebeu um número no
final:

| Produto | Unidade | REF na planilha | Código aplicado |
|---|---|---|---|
| TINTA EMBORRACHADA MARROM BURGUES CORAL | LA / 18L | 501010172 | **5010101721** |
| TINTA EMBORRACHADA PEDRA ALTA | LA / 18L | 501010172 | **5010101722** |

Isso já é aplicado automaticamente. Se depois o fornecedor confirmar qual é a REF
correta de cada um, o fim do arquivo `.sql` tem o comando pronto para trocar —
descomente a linha do produto, substitua `<CODIGO_CORRETO>` pelo código certo e rode
só aquela linha.

---

## Se der erro

### `'psql.exe' não é reconhecido como um comando`
O caminho está errado. Volte ao **Passo 3** e confirme o número da versão.

### `psql: error: connection to server ... failed`
O serviço do PostgreSQL não está rodando. Abra os **Serviços** do Windows
(**Windows + R** → `services.msc`), procure por `postgresql-x64-18` e clique em
**Iniciar**.

### `password authentication failed for user "postgres"`
Senha errada. Ela está no arquivo `.env` do sistema, no campo `DB_PASSWORD`.

### `database "imperial_colors" does not exist`
O banco tem outro nome nesta máquina. Confira o campo `DB_NAME` do `.env` e troque
`-d imperial_colors` pelo nome correto.

### Qualquer outro erro no meio da execução
A transação é desfeita automaticamente — o banco fica como estava antes. Anote a
mensagem de erro e nada mais precisa ser feito.

---

## Restaurar o backup (só se algo der muito errado)

```cmd
"C:\Program Files\PostgreSQL\18\bin\pg_restore.exe" -h localhost -U postgres -d imperial_colors -c "C:\backup_antes_update.backup"
```

---

## Para quem for regerar o script

O `.sql` é gerado a partir da planilha, não editado à mão:

```cmd
python tools\gerar_update_ref_parana.py "Tabela Cadastro_Imperial Colors (1).xlsx" "D:\atualizar_codigo_produto_parana.sql"
```

O gerador confere a planilha contra o `importar_catalogo_parana.sql` e falha se as
duas listas divergirem, então uma planilha desatualizada não passa despercebida.
