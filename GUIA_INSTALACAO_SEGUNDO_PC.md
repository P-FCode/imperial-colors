# Guia de Instalação — Segundo PC (e demais)

Passo a passo completo para colocar o Imperial Colors em um **segundo computador** da loja,
compartilhando o mesmo banco de dados do primeiro.

Escrito para ser seguido de cima para baixo, no dia da instalação, sem precisar consultar
outro documento. Cada passo diz **o que fazer**, **como conferir que deu certo** e **o que
significa se der errado**.

> **Quanto tempo leva:** cerca de 40 minutos, sendo que a maior parte é no PC que já está
> funcionando (o servidor). No PC novo em si são uns 10 minutos.

---

## Índice

1. [Entenda o que você vai montar](#1-entenda-o-que-você-vai-montar)
2. [Antes de começar — o que anotar](#2-antes-de-começar--o-que-anotar)
3. [Parte 1 — Preparar o PC servidor](#parte-1--preparar-o-pc-servidor)
4. [Parte 2 — Instalar no PC novo](#parte-2--instalar-no-pc-novo)
5. [Parte 3 — Conferência final](#parte-3--conferência-final)
6. [Quando algo dá errado](#quando-algo-dá-errado)
7. [Depois da instalação — o que manter](#depois-da-instalação--o-que-manter)

---

## 1. Entenda o que você vai montar

O Imperial Colors não guarda os dados dentro do programa. Os dados ficam num **banco de
dados PostgreSQL**, que roda em **um** computador da loja. Todos os outros computadores
abrem o mesmo programa e se conectam **pela rede** a esse banco.

```
┌──────────────────────────────┐              ┌──────────────────────────────┐
│  PC 1 — SERVIDOR             │              │  PC 2 — NOVO                 │
│                              │   rede da    │                              │
│  PostgreSQL (o banco)        │ ◄─ loja ──►  │  ImperialColors.exe          │
│  ImperialColors.exe          │   (cabo ou   │  (SEM PostgreSQL)            │
│                              │    Wi-Fi)    │                              │
└──────────────────────────────┘              └──────────────────────────────┘
```

Consequências práticas disso, que valem para o resto do guia:

- **O PostgreSQL é instalado uma vez só**, no PC 1. No PC 2 você instala **apenas** o
  Imperial Colors.
- **Estoque, vendas e cadastros são compartilhados na hora.** Vendeu no PC 2, o estoque cai
  no PC 1 no mesmo instante.
- **Se o PC 1 desligar, o PC 2 para.** Não é opinião, é como funciona: o banco está lá.
- **Impressora é de cada máquina.** Cada PDV configura a sua.

---

## 2. Antes de começar — o que anotar

Anote estas cinco informações **antes** de mexer em qualquer coisa. Sem elas você trava no
meio do processo.

| # | O que anotar | Onde conseguir |
|---|---|---|
| 1 | **IPv4 do PC servidor** | `ipconfig` no PC 1 — explicado no [Passo 1.1](#11-descobrir-o-ip-do-servidor) |
| 2 | **Senha do usuário `postgres`** | Foi definida na instalação do PostgreSQL no PC 1. Também está no arquivo `.env` do PC 1, em `DB_PASSWORD` |
| 3 | **Versão do sistema no PC 1** | No PC 1: **Configurações → Sobre o Sistema** |
| 4 | **Versão do PostgreSQL** | Nome da pasta em `C:\Program Files\PostgreSQL\` (ex.: `16`) |
| 5 | **Usuário e senha do admin** | Para conseguir entrar no sistema depois de instalar |

Leve também:

- O **pendrive** (ou acesso à internet) com o pacote `ImperialColors-win-x64.zip`
- Acesso de **administrador do Windows** nas duas máquinas
- Acesso ao **roteador da loja** (senha de administrador) — se não tiver, o Passo 1.2 já traz
  o caminho alternativo

---

## Parte 1 — Preparar o PC servidor

> **Não pule esta parte**, mesmo que o PC 1 já esteja funcionando há meses. Quando o sistema
> é instalado numa máquina só, o PostgreSQL fica configurado para aceitar conexões **apenas
> do próprio computador**. Ele funciona perfeitamente no PC 1 e recusa o PC 2. Os passos 1.3
> a 1.5 são exatamente o que muda isso.

### 1.1 Descobrir o IP do servidor

No **PC 1**, abra o PowerShell e digite:

```powershell
ipconfig
```

Procure o adaptador que está realmente em uso:

- **Ethernet** / **Adaptador de Rede Ethernet** → se o PC está ligado por cabo
- **Wi-Fi** / **Adaptador de Rede sem Fio** → se está sem fio

Dentro dele, anote a linha **Endereço IPv4**:

```
Adaptador de Rede sem Fio Wi-Fi:
   Endereço IPv4. . . . . . . . . . . . . . . . : 192.168.1.100
   Máscara de Sub-rede . . . . . . . . . . . . . : 255.255.255.0
   Gateway Padrão. . . . . . . . . . . . . . . . : 192.168.1.1
```

Neste exemplo o IP do servidor é **`192.168.1.100`**. Anote também o **Gateway Padrão**
(`192.168.1.1`) — você vai precisar dele se escolher fixar o IP manualmente.

**O que NÃO serve como IP do servidor:**

| Valor | Por quê |
|---|---|
| `127.0.0.1` ou `localhost` | Significa "este mesmo computador". No PC 2 apontaria para o PC 2. |
| `169.254.x.x` | O DHCP não respondeu. A rede está com problema — resolva antes de continuar. |
| `fe80::...` (IPv6) | Use sempre o IPv4, no formato `192.168.x.x` ou `10.0.x.x`. |
| IP que aparece em "qual é meu IP" no navegador | É o IP público do roteador, não serve dentro da loja. |

### 1.2 Fixar o IP do servidor

Este passo é o que evita o sistema "parar sozinho" daqui a algumas semanas.

**O problema:** o roteador distribui os IPs automaticamente (DHCP) e pode entregar um
endereço diferente ao PC 1 depois de um reinício ou de uma queda de energia. Quando isso
acontece, o PC 2 continua procurando o IP antigo e não encontra mais o banco. O erro que
aparece na tela fala de banco de dados, e ninguém desconfia que foi a rede.

**As duas soluções abaixo resolvem.** Nenhuma é gambiarra: as duas são usadas em instalação
profissional, e cada uma falha num cenário diferente. O que decide é uma pergunta prática:

> **Você tem acesso estável ao roteador da loja — a senha de administrador, e a certeza de
> que ninguém vai resetá-lo?**

| Sua situação | Escolha |
|---|---|
| Roteador próprio do cliente, você tem a senha de admin | **Opção 1 — reserva de DHCP** |
| Roteador da operadora (Vivo, Claro, Oi) sem acesso de admin | **Opção 2 — IP manual** |
| A operadora costuma dar factory reset em visita técnica | **Opção 2 — IP manual** |
| A rede pode mudar de faixa no futuro (troca de roteador/provedor) | **Opção 1 — reserva de DHCP** |

Em resumo: a **reserva** é mais resistente ao que acontece do lado do computador (reinstalação
do Windows, troca de placa de rede, "redefinição de rede") e se reajusta sozinha se a faixa
da rede mudar. O **IP manual** é mais resistente ao que acontece do lado do roteador (reset
de fábrica, troca de aparelho, perda da senha de admin) e não depende de ninguém ter acesso a
ele.

#### Opção 1 — Reserva de DHCP no roteador

O roteador continua entregando o IP automaticamente, mas sempre **o mesmo** para aquele
computador. Não mexe em nada no Windows.

1. Acesse o roteador pelo navegador (normalmente o Gateway Padrão anotado no passo 1.1 —
   ex.: `http://192.168.1.1`)
2. Procure por **DHCP** → **Reserva de endereço**, **Address Reservation**, **DHCP estático**
   ou **Vincular IP-MAC** (o nome muda conforme a marca)
3. Localize o PC 1 na lista de aparelhos conectados e reserve o IP atual dele
4. Reinicie o PC 1 e confira com `ipconfig` que o IP continuou o mesmo

> **O ponto fraco desta opção:** a reserva mora no roteador. Um reset de fábrica — o que
> técnico de operadora faz com frequência — apaga a reserva, e o PC 1 volta a receber
> qualquer IP. Se isso acontecer, o sintoma vai ser o PC 2 "parando do nada", e a correção é
> refazer a reserva (ou migrar para a Opção 2).

#### Opção 2 — IP manual no Windows

O endereço passa a ser definido no próprio computador e não depende mais do roteador. Exige
cuidado com dois detalhes que costumam passar batido — respeitados os dois, é tão confiável
quanto a reserva.

1. **Configurações** → **Rede e Internet**
2. Clique na conexão ativa (**Wi-Fi** ou **Ethernet**)
3. Em **Configurações de IP** → **Atribuição de IP** → **Editar**
4. Troque de **Automático (DHCP)** para **Manual**
5. Ligue o **IPv4** e preencha:

| Campo | O que colocar |
|---|---|
| Endereço IP | Um IP da mesma faixa, **fora do intervalo que o roteador distribui** (veja o aviso abaixo) |
| Máscara de sub-rede | A mesma do `ipconfig` — quase sempre `255.255.255.0` |
| Gateway | O **Gateway Padrão** anotado no passo 1.1 |
| DNS preferencial | O mesmo IP do gateway, ou `8.8.8.8` |

> **Atenção 1 — escolha um IP fora da faixa do DHCP.** O roteador costuma distribuir de
> `.100` a `.200`. Se você fixar o PC 1 em `.150`, um dia o roteador entrega esse mesmo
> `.150` para um celular e os dois brigam pelo endereço. Prefira algo baixo, como `.10` ou
> `.20`, e confira a faixa nas configurações de DHCP do roteador.
>
> **Atenção 2 — preencha gateway e DNS.** Se deixar em branco, aquele computador fica sem
> internet. E sem internet vão junto a **emissão de NF-e** e o **atualizador do sistema**.

> **O ponto fraco desta opção:** a configuração mora no Windows do PC 1 e se perde sozinha em
> algumas situações — "Redefinição de rede" do Windows, troca de placa de rede, reinstalação
> do sistema. Em qualquer uma delas a máquina volta para DHCP em silêncio. Se um dia trocarem
> o roteador e a rede mudar de faixa (de `192.168.1.x` para `192.168.0.x`, por exemplo), o
> PC 1 fica ilhado até alguém reconfigurar à mão. Anote num papel colado na máquina qual IP
> foi fixado.

#### E o PC 2, pode ficar no DHCP?

**Pode, sem problema nenhum.** Ninguém precisa saber o IP do PC 2 — é ele que procura o
servidor, não o contrário. A única exigência é que ele caia na **mesma faixa de rede** (se o
servidor é `192.168.1.100`, o PC 2 precisa ser `192.168.1.alguma-coisa`), porque a regra de
liberação do banco no passo 1.4 autoriza a faixa inteira.

### 1.3 Permitir que o PostgreSQL escute a rede

Abra o arquivo `postgresql.conf`. Ele fica em:

```
C:\Program Files\PostgreSQL\16\data\postgresql.conf
```

*(troque `16` pela versão anotada no passo 2 da preparação)*

Procure a linha `listen_addresses`. Ela normalmente está comentada, assim:

```conf
#listen_addresses = 'localhost'
```

Deixe assim — **sem** o `#` no começo:

```conf
listen_addresses = '*'
```

O `*` significa "aceite conexões por qualquer adaptador de rede". Sem isso, o PostgreSQL só
conversa com programas do próprio PC 1.

> Edite como **Administrador**, senão o Bloco de Notas não deixa salvar. Clique com o botão
> direito no Bloco de Notas → **Executar como administrador** → e abra o arquivo por dentro
> dele.

### 1.4 Autorizar os computadores da loja

Na **mesma pasta**, abra o arquivo `pg_hba.conf`. Vá até o **final do arquivo** e acrescente
uma linha:

```conf
# Libera os computadores da rede da loja a acessar o banco imperial_colors
host    imperial_colors    postgres    192.168.1.0/24    scram-sha-256
```

**Como montar a faixa correta** — pegue o IP do servidor, troque o último número por `0` e
acrescente `/24`:

| Se o IP do servidor for… | A linha usa… |
|---|---|
| `192.168.1.100` | `192.168.1.0/24` |
| `192.168.0.50` | `192.168.0.0/24` |
| `10.0.0.15` | `10.0.0.0/24` |

O `/24` autoriza toda a faixa (de `.1` até `.254`). É por isso que o PC 2 pode ficar no DHCP
sem problema: qualquer IP que o roteador der a ele já está autorizado.

### 1.5 Reiniciar o PostgreSQL

As duas alterações acima só valem depois de reiniciar o serviço. No PowerShell **como
administrador**:

```powershell
Get-Service *postgres*
```

Anote o nome exato que aparecer (ex.: `postgresql-x64-16`) e reinicie:

```powershell
Restart-Service postgresql-x64-16
```

**Como conferir:** rode `Get-Service *postgres*` de novo. O status precisa estar `Running`.
Se voltar `Stopped`, há um erro de digitação em um dos dois arquivos — reveja os passos 1.3
e 1.4 antes de seguir.

### 1.6 Liberar a porta no Firewall do Windows

No PowerShell **como administrador**:

```powershell
New-NetFirewallRule -DisplayName "PostgreSQL Imperial Colors" -Direction Inbound -Protocol TCP -LocalPort 5432 -Action Allow
```

Isso autoriza o Windows do PC 1 a receber conexões na porta 5432, que é a porta do
PostgreSQL. Sem essa regra, o PC 2 tenta conectar e fica esperando até dar tempo esgotado.

### 1.7 Impedir que o servidor durma

Se o PC 1 suspender no meio do expediente, o PC 2 perde o banco e as vendas param.

1. **Configurações** → **Sistema** → **Energia**
2. **Tela e suspensão** → coloque **Nunca** em "Quando conectado, colocar o dispositivo em
   suspensão após"

A tela pode desligar normalmente — o que não pode é o computador suspender.

---

## Parte 2 — Instalar no PC novo

### 2.1 Copiar o programa

Baixe o pacote `ImperialColors-win-x64.zip` da página de releases do projeto, ou leve no
pendrive a partir do PC 1.

Extraia numa pasta fixa e fácil de achar:

```
C:\ImperialColors
```

> **Não instale o PostgreSQL neste computador.** Ele não precisa do banco — ele usa o banco
> do PC 1. Instalar um segundo PostgreSQL aqui cria um banco vazio paralelo, e o sintoma é
> confuso: o sistema abre normalmente, mas não mostra nenhum produto.

> **Por que não precisa instalar o .NET:** o pacote é *self-contained*, ou seja, já vem com
> tudo que precisa para rodar.

### 2.2 Configurar a conexão com o banco (`.env`)

Esta é **a única configuração obrigatória** do PC 2, e o passo em que dá errado com mais
frequência.

Copie o arquivo **`.env`** do PC 1 (está na pasta do `ImperialColors.exe`) para a pasta do
PC 2, ao lado do executável. Depois abra no Bloco de Notas e mude **uma única linha**: a do
`DB_HOST`.

```env
# No PC 2, DB_HOST aponta para o IP do SERVIDOR (anotado no passo 1.1)
DB_HOST=192.168.1.100
DB_PORT=5432
DB_NAME=imperial_colors
DB_USER=postgres
DB_PASSWORD=<a mesma senha que está no .env do PC 1>
DB_SSL_MODE=Prefer
```

Comparando as duas máquinas:

| Campo | No PC 1 (servidor) | No PC 2 |
|---|---|---|
| `DB_HOST` | `localhost` | **o IP do servidor** |
| `DB_PORT` | `5432` | `5432` — igual |
| `DB_NAME` | `imperial_colors` | igual |
| `DB_USER` | `postgres` | igual |
| `DB_PASSWORD` | a senha do PostgreSQL | **igual** |

> **Sobre a senha:** ela fica escrita em texto puro no `.env`, ao lado do executável, em toda
> máquina onde o sistema é instalado. É assim que o sistema funciona hoje — vale saber que
> quem tiver acesso a essa pasta no PC 2 consegue ler a senha do banco. Se o PC 2 for ficar
> num lugar de acesso público, considere restringir a pasta pelas permissões do Windows.

### 2.3 Não copie o `localsettings.json`

Se você copiou a pasta inteira do PC 1, **apague o arquivo `localsettings.json`** da pasta
do PC 2.

Esse arquivo guarda configurações **daquela máquina específica** — hoje, a impressora
escolhida. Copiado junto, o PC 2 nasce tentando imprimir numa impressora que está fisicamente
no outro balcão.

O sistema recria o arquivo sozinho quando você configurar a impressora no passo 2.6.

### 2.4 Testar a rede antes de abrir o sistema

Faça este teste **antes** de abrir o programa. Ele separa "problema de rede" de "problema do
sistema", e economiza muito tempo de investigação.

No PC 2, no PowerShell (troque pelo IP do seu servidor):

```powershell
Test-NetConnection -ComputerName 192.168.1.100 -Port 5432
```

| Resultado | O que significa | O que fazer |
|---|---|---|
| `TcpTestSucceeded : True` | A rede está OK e o PostgreSQL está respondendo | Siga para o passo 2.5 |
| `TcpTestSucceeded : False` | O PC 2 não alcança o banco | **Não abra o sistema ainda** — vá para [Quando algo dá errado](#quando-algo-dá-errado) |

### 2.5 Abrir o sistema

1. Execute o `ImperialColors.exe`
2. Faça login com o usuário administrador
3. Vá em **Configurações → Geral**, seção **Banco de Dados (.env)**
4. Clique em **Testar Conexão**

Se aparecer mensagem de sucesso, a instalação está funcionando. Se aparecer erro, o texto da
mensagem diz o motivo — compare com a tabela da última seção.

> O botão **Abrir .env**, ao lado, abre o arquivo de configuração direto pela tela, útil para
> corrigir o `DB_HOST` sem procurar a pasta.

### 2.6 Configurar a impressora do PC 2

**Configurações → Periféricos** → selecione a impressora que está ligada **neste**
computador.

Cada máquina tem a sua. O banco é compartilhado, os periféricos não.

### 2.7 Conferir a versão

**Configurações → Sobre o Sistema** → confira se a versão é **a mesma** do PC 1 (anotada na
preparação).

Se estiverem diferentes, use o botão **⭳ Atualizar Sistema** para deixar as duas iguais —
primeiro numa máquina, depois na outra. O motivo está explicado na
[última seção](#mantenha-as-duas-máquinas-na-mesma-versão).

---

## Parte 3 — Conferência final

Faça estes quatro testes com as duas máquinas abertas. Eles confirmam que a instalação está
realmente completa, e não só "abrindo sem erro".

| # | Teste | Resultado esperado |
|---|---|---|
| 1 | Cadastre um produto de teste no **PC 2** | Ele aparece na listagem de Estoque do **PC 1** (atualize a tela) |
| 2 | Altere o preço desse produto no **PC 1** | O novo preço aparece no **PC 2** |
| 3 | Faça uma venda de teste no **PC 2** | O estoque do produto cai nas **duas** máquinas |
| 4 | Imprima o cupom dessa venda no **PC 2** | Sai na impressora do PC 2, não na do PC 1 |

Depois **cancele a venda de teste** (Vendas → Registrar Devolução) e **exclua o produto de
teste**, para não sujar o histórico do cliente.

---

## Quando algo dá errado

### `TcpTestSucceeded : False` no passo 2.4

Verifique nesta ordem — a causa quase sempre está na primeira ou na segunda:

1. **O IP está certo?** Rode `ipconfig` no PC 1 de novo e compare com o `DB_HOST` do PC 2.
   Se o IP mudou, é o problema do passo 1.2 acontecendo — fixe o IP.
2. **O firewall foi liberado?** Refaça o passo 1.6 no PC 1.
3. **O PostgreSQL está rodando?** No PC 1: `Get-Service *postgres*` precisa mostrar
   `Running`.
4. **As duas máquinas estão na mesma rede?** Compare os três primeiros números do IPv4 das
   duas. `192.168.1.x` e `192.168.0.x` são redes diferentes e não se enxergam. Acontece
   quando uma máquina está no Wi-Fi principal e a outra numa rede de visitantes.

### "Erro ao conectar com o banco de dados" ao abrir o sistema

O sistema mostra o motivo dentro da própria mensagem. Os mais comuns:

| Trecho da mensagem | Causa | Solução |
|---|---|---|
| `password authentication failed` | Senha errada no `.env` | Copie o `DB_PASSWORD` do `.env` do PC 1 |
| `no pg_hba.conf entry for host` | A faixa de IP não foi autorizada | Refaça o passo 1.4 e reinicie o serviço (1.5) |
| `Connection refused` / `timeout` | Rede, firewall ou serviço parado | Volte para o teste do passo 2.4 |
| `database "imperial_colors" does not exist` | O `DB_NAME` está diferente do PC 1 | Compare os dois arquivos `.env` |

### O sistema abre, mas não mostra nenhum produto

Quase sempre significa que o PC 2 está conectado a **outro banco**, não ao do PC 1. Confira:

- O `DB_HOST` do PC 2 aponta mesmo para o IP do servidor, e não para `localhost`
- Ninguém instalou PostgreSQL no PC 2 por engano (passo 2.1)

### O sistema funcionava e parou depois de alguns dias

O primeiro suspeito é o IP do servidor ter mudado. Rode `ipconfig` no PC 1 e compare com o
`DB_HOST` do PC 2. Se for isso, resolva **de vez** com o passo 1.2 — só corrigir o `.env`
faz o problema voltar.

---

## Depois da instalação — o que manter

### Mantenha as duas máquinas na mesma versão

O sistema atualiza a estrutura do banco sozinho toda vez que abre. Como o banco é
compartilhado, se o PC 2 abrir com uma versão **mais nova** que a do PC 1, ele altera o banco
de todo mundo, e o PC 1 pode passar a apresentar erros.

Ao atualizar, atualize as duas máquinas na mesma ocasião, por **Configurações → Sobre o
Sistema → ⭳ Atualizar Sistema**.

### Backup é responsabilidade do servidor

O banco existe só no PC 1. A pasta de backup é configurada em **Configurações → Geral →
Pasta dos backups**, e o que importa é o backup **daquela máquina**. Fazer backup do PC 2 não
protege nada — ele não guarda dados.

### Adicionando um terceiro PC

Repita apenas a [Parte 2](#parte-2--instalar-no-pc-novo). A Parte 1 já está feita: a regra
`/24` do passo 1.4 já autoriza qualquer computador novo da mesma rede.

---

## Resumo em uma tabela

| Máquina | Instala PostgreSQL | `DB_HOST` no `.env` | IP precisa ser fixo |
|---|---|---|---|
| PC 1 — servidor | **Sim** | `localhost` | **Sim** (reserva de DHCP ou manual) |
| PC 2 — novo | Não | IP do PC 1 (ex.: `192.168.1.100`) | Não — pode ficar no DHCP |
| PC 3, 4… | Não | IP do PC 1 | Não |
