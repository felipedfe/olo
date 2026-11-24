# 5 — Arquitetura: como o Olo está organizado e quando mudar

Este não é um plano de tarefa. É um **documento de referência**: explica as decisões de arquitetura que o Olo usa hoje, quais padrões ele ainda **não** usa (de propósito) e **quais sinais indicam que chegou a hora de mudar**.

A ideia veio do vídeo *"I Learned This From Airplanes"* (um dev de Godot falando da arquitetura do jogo dele). O princípio central dele vale para a gente também:

> Quando algo quebra, quebra **dentro de uma caixa**. Não leva o resto junto.

Na prática isso dá duas coisas: **falhas isoladas** (um bug no warp fica no warp) e **mudanças baratas** (criar uma barra nova não exige mexer na bola).

## Resumo

| Padrão | O Olo usa? | Onde |
|---|---|---|
| State machine | ✅ Sim (versão simples, com `enum`) | `BallController` |
| Componentes (composição) | ✅ Sim | `BarMover`, `CanvasFade` |
| "Objeto chama a ação da bola" | ✅ Sim | `DefaultBar`, `GrabBar`, `Warp`, `CameraBounds` |
| Event bus | ❌ Ainda não | — |
| Gerenciadores globais (GameManager / GameState) | ❌ Ainda não | — |

---

## 1. State machine: uma coisa de cada vez

### O que é

Em qualquer momento, a bola está fazendo **exatamente uma coisa**: parada esperando, andando, grudada numa barra, dentro de um warp ou terminada. Cada uma dessas situações é um **estado**. A state machine é só a regra que diz qual estado está ativo e quando ele troca.

### Como está no Olo

```csharp
public enum BallState { Ready, Moving, Attached, Warping, Finished }
```

Cada ação da bola começa conferindo **um** estado:

```csharp
public bool Reflect(Vector2 normal)
{
    if (currentState != BallState.Moving) return false;
    ...
}
```

### Por quê assim?

Sem estados, o código vira uma cadeia de `if (andando && !grudada && !no warp && ...)`. Com o `enum`, cada método pergunta uma coisa só, e fica fácil saber o que a bola pode ou não fazer em cada momento.

### Diferença do vídeo

No vídeo, **cada estado é um script separado** (uma classe por estado, cada uma com `Enter`, `Update` e `Exit`). Os estados não se conhecem, então mexer em um não quebra o outro.

No Olo, os estados são valores de um `enum` dentro do próprio `BallController`. Para o tamanho atual (5 estados, pouca lógica em cada), isso é **mais simples e mais fácil de ler**.

### ⚠️ Sinais de que é hora de mudar

- O `BallController` passou de umas **300 linhas** e boa parte é lógica de um estado específico.
- Algum estado ganhou **lógica própria grande** (ex.: o warp com animação, som, partículas e várias fases).
- Começam a aparecer **`switch (currentState)` repetidos** em vários métodos (`FixedUpdate`, `OnMove`, ...).
- Outro objeto do jogo (um inimigo, uma bola especial) precisa de uma state machine própria.

**Como seria:** uma classe base `BallStateBase` com `Enter()`, `Tick()` e `Exit()`, e uma classe por estado (`MovingState`, `AttachedState`, ...). O `BallController` só guarda o estado atual e faz a troca.

---

## 2. Componentes: escreve uma vez, usa em todo lugar

### O que é

**Composição em vez de herança.** Em vez de copiar a mesma lógica em vários scripts, você cria um componente pequeno com **uma função só** e coloca ele em quem precisar.

### Como está no Olo

- **`BarMover`**: o movimento de vai-e-vem. A `DefaultBar` e a `GrabBar` usam o mesmo componente. Se corrigir um bug no movimento, corrige nas duas de uma vez.
- **`CanvasFade`**: escurece e clareia a tela. Não sabe nada sobre bola nem câmera, só sabe fazer fade.

### Por quê assim?

Antes do `BarMover`, cada barra tinha sua própria cópia do movimento. Duas cópias = dois lugares para lembrar de corrigir.

### Regra prática

Quando você estiver prestes a **copiar e colar** um pedaço de lógica para um segundo script, é o sinal para extrair um componente. Candidatos prováveis no futuro:

- Um componente de **som de colisão** (toca um som quando algo bate), reutilizável em todas as barras.
- Um componente de **piscar/feedback visual** ao ser atingido.

---

## 3. "Cada objeto chama a ação da bola"

### O que é

Esse é o padrão principal do Olo, definido lá no [doc 1](1-default-bar.md):

- **A bola sabe *como* se mover.** Ela oferece ações públicas: `Reflect`, `AttachTo`, `WarpTo`, `ResetPosition`.
- **Cada objeto sabe *o que* acontece quando a bola bate nele.** Ele detecta a colisão e chama a ação certa.

```
DefaultBar   ──►  ball.Reflect(normal)
GrabBar      ──►  ball.AttachTo(bar, normal)
Warp         ──►  ball.WarpTo(posição)
CameraBounds ──►  ball.ResetPosition()
```

### Por quê assim?

A bola **não conhece** nenhum desses objetos. Para criar um objeto novo, você cria um script novo. A bola só muda quando precisa aprender uma ação nova.

É uma forma de comunicação **direta**: quem precisa de algo pega a referência e chama o método. Isso é ótimo enquanto cada acontecimento tem **um único interessado**.

---

## 4. Event bus: o "intercom" do jogo (ainda não usamos)

### O que é

Em vez de um sistema chamar outro diretamente, ele **anuncia** que algo aconteceu num canal global, e quem estiver interessado **escuta** e reage. Quem anuncia não sabe quem está ouvindo.

### Como é hoje (comunicação direta)

O `CameraBounds` faz tudo sozinho quando a bola sai da tela:

```
CameraBounds ──► canvasFade.FadeOut()
             ──► cinemachine.PreviousStateIsValid = false
             ──► ball.ResetPosition()
             ──► canvasFade.FadeIn()
```

Hoje isso está ótimo: é uma sequência só, fácil de ler, e você clica no método e vê exatamente o que acontece.

### Como seria com event bus

```
CameraBounds ──► anuncia "a bola saiu da tela"
                     │
                     ├─► CanvasFade escuta  → faz o fade
                     ├─► Som escuta         → toca um efeito
                     ├─► Placar escuta      → conta uma tentativa
                     └─► Analytics escuta   → registra a tentativa
```

Adicionar um sistema novo (ex.: tremer a tela) **não mexe no `CameraBounds`**. O sistema novo só se inscreve no evento.

### Como fazer no Unity (quando chegar a hora)

A forma mais simples é uma classe estática com `event` do C#:

```csharp
// GameEvents.cs
public static class GameEvents
{
    public static event System.Action BallReset;

    public static void AnnounceBallReset()
    {
        if (BallReset != null)
        {
            BallReset();
        }
    }
}
```

Quem anuncia:

```csharp
GameEvents.AnnounceBallReset();
```

Quem escuta:

```csharp
void OnEnable()
{
    GameEvents.BallReset += TocarSom;
}

void OnDisable()
{
    // sempre desinscrever! senão o evento chama um objeto que já foi destruído
    GameEvents.BallReset -= TocarSom;
}
```

### Por que ainda não usamos

- O jogo tem poucos scripts. Comunicação direta é **mais fácil de ler e de depurar**: com evento, você não vê de cara quem reage ao anúncio.
- Nenhum acontecimento tem hoje vários interessados sem relação entre si.

### ⚠️ Sinais de que é hora de mudar

- **Um acontecimento precisa avisar 3 ou mais sistemas que não têm nada a ver entre si** (ex.: bola resetou → fade + som + placar).
- Um script começa a ter **referências públicas para coisas que não são dele** só para avisá-las (ex.: o `CameraBounds` ganhando campos `audioSource`, `scoreUI`, `analytics`...).
- Você percebe que está **mexendo no mesmo script toda vez** que adiciona um sistema novo (som, UI, efeitos).

**Primeiros candidatos prováveis no Olo:** `BallReset` (bola voltou ao início), `BallFinished` (fase completa) e `BallHit` (bola bateu em algo, útil para som e efeitos).

---

## 5. Gerenciadores globais: os "departamentos sempre abertos" (ainda não usamos)

### O que é

Alguns sistemas precisam existir **o tempo todo**, de qualquer cena. No Godot eles se chamam *autoloads*. No vídeo, ele tem dois:

- **GameManager**: a autoridade sobre o **modo do jogo**. O jogo está rodando? Pausado? Na tela de vitória? Ninguém pausa o jogo por conta própria, todo mundo pergunta ao GameManager.
- **GameState**: a **memória persistente**. Quais fases foram completadas? Quantas estrelas em cada uma? Tudo que precisa sobreviver à troca de cena fica aqui. Uma fonte de verdade só.

### Como fazer no Unity (quando chegar a hora)

- Um **singleton** com `DontDestroyOnLoad` (um objeto que não é destruído ao trocar de cena), ou
- Um **ScriptableObject** para guardar dados (fica salvo como asset e qualquer script pode referenciar).

### Por que ainda não usamos

O Olo ainda não tem pausa, seleção de fases, nem progresso salvo. Criar isso agora seria estrutura sem uso.

### ⚠️ Sinais de que é hora de mudar

- O jogo passa a ter **mais de uma fase** e precisa lembrar qual o jogador já completou.
- Entra **pausa**, **menu** ou **tela de vitória** (o estado `Finished` da bola vai levar a isso).
- Dois scripts diferentes começam a guardar **a mesma informação** (ex.: a fase atual guardada na UI e também no script da fase).
- Integração com o **Poki SDK** (eventos de início/fim de partida, anúncios): faz sentido centralizar num gerenciador, em vez de espalhar chamadas pelo jogo.

---

## Checklist rápido: "preciso mudar a estrutura?"

Use quando for adicionar uma feature nova:

- [ ] Vou **copiar e colar** lógica de outro script? → extrair um **componente**.
- [ ] Um acontecimento vai avisar **3+ sistemas sem relação**? → criar um **evento**.
- [ ] Um script está ganhando referências para coisas que **não são dele** só para avisá-las? → criar um **evento**.
- [ ] Preciso de algo que **sobreviva à troca de cena** ou que vários lugares consultem? → criar um **gerenciador global**.
- [ ] O `BallController` está grande e cheio de lógica de um estado só? → **um script por estado**.

Se nenhuma resposta for "sim", a estrutura atual dá conta. **Não adiantar arquitetura antes de precisar** também é uma decisão de arquitetura.
