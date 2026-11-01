# 3 — Warp (mesma direção)

Um par de warps: a bola entra num e sai no outro, **na mesma direção em que entrou** (↗ entra, ↗ sai).

## Objetivo

- A bola que encosta no **warp A** some, e depois de um pequeno **delay** aparece no **warp B** e continua andando, com a mesma direção e velocidade.
- Funciona nos dois sentidos: entrando no B, ela sai no A.
- A bola **não fica presa** indo e voltando entre os dois warps.
- O warp muda **onde** a bola está. Quem muda **para onde ela vai** continuam sendo as barras.

## Progresso

- [x] ~~Passo 1 — Importar o sprite do warp~~ (não precisou: o warp usa o círculo padrão da Unity)
- [x] [Passo 2 — Criar o GameObject `Warp`](#passo-2--criar-o-gameobject-warp)
- [x] [Passo 3 — Ação `WarpTo` na bola (com delay)](#passo-3--ação-warpto-na-bola-com-delay)
- [x] [Passo 4 — Script `Warp.cs`](#passo-4--script-warpcs)
- [x] [Passo 5 — Prefab e o par na cena](#passo-5--prefab-e-o-par-na-cena)
- [ ] [Passo 6 — Câmera pula junto (depois do plano 2)](#passo-6--câmera-pula-junto-depois-do-plano-2)

## A ideia

```
        ●  ← sai daqui, continuando ↗
       (B)
                      ↑ a bola "some" do A
                      e "aparece" no B
         (A)
        ↗
       ●  ← entra aqui
```

Mesma arquitetura das barras: o **warp** detecta a bola e chama uma ação dela, `ball.WarpTo(posição)`. A bola some, espera o delay, aparece no destino e volta a andar com a **mesma `direction`**, por isso ela sai na mesma direção.

Cada warp aponta para o outro pelo Inspector (campo `destination`). Não precisa de um objeto "WarpZone" para juntar os dois.

## Phaser → Unity

| Phaser (`warp.js` / `warpZone.js`) | Unity |
|---|---|
| `new WarpZone(x1, y1, x2, y2)` cria os dois warps | Dois GameObjects `Warp`, cada um com o outro em `destination` |
| `physics.add.overlap(ball, warp, ...)` | `CircleCollider2D` com **Is Trigger** + `OnTriggerEnter2D` |
| `body.setCircle(radius)` com 60% do tamanho | **Radius** menor no `CircleCollider2D` |
| `setPosition(otherWarp.x - largura/4, ...)` | `rig.position = destination.transform.position` (pivot no centro, sem ajuste) |
| `setVelocity(0)` + `alpha = 0`, `setTimeout(..., 100)`, `alpha = 1` + `moveBall()` | Coroutine `WarpRoutine`: para, esconde, `WaitForSeconds(warpDelay)`, mostra e volta a andar |
| Cooldown de 500 ms (`startCooldown`) | O warp de destino ignora a bola até ela **sair** dele (`OnTriggerExit2D`) |

---

## Passo 1 — Importar o sprite do warp

Não precisou: o warp usa o **Circle** padrão da Unity (2D → Sprites → Circle), pintado de laranja.

---

## Passo 2 — Criar o GameObject `Warp`

1. **Hierarchy → Create → 2D Object → Sprites → Circle**. Renomear para `Warp` e escolher a cor no **SpriteRenderer**.
2. Ajustar a **Scale** até o warp ficar um pouco maior que a bola (ficou `0.46`, com a bola em `0.35`).
3. **Add Component → CircleCollider2D**:
   - ligar **Is Trigger**;
   - diminuir o **Radius** de `0.5` (tamanho do desenho) para `0.3` (60%).

> **Por quê Is Trigger?** O warp não pode **bloquear** a bola, só **perceber** que ela passou por cima. É o `overlap` do Phaser (as barras usam `collider`, o warp usa `overlap`).
>
> **Por quê um Radius menor?** Para a bola precisar entrar **de verdade** no warp. Com o círculo do tamanho do desenho, só de raspar na borda ela já seria teleportada. No Phaser você fez a mesma coisa com `setCircle(... * 0.6)`.
>
> **E o Rigidbody2D?** O warp não precisa de um. Ele fica parado, e a bola já tem Rigidbody2D: isso basta para o `OnTriggerEnter2D` funcionar.

✅ **Teste:** na Scene view, o círculo verde do colisor fica **dentro** do desenho do warp. Dando Play, a bola passa por cima do warp sem bater nele (ainda não teleporta).

---

## Passo 3 — Ação `WarpTo` na bola (com delay)

### 3a. Novo estado `Warping`

No `enum BallState`, adicionar o estado de "dentro do warp":

```csharp
public enum BallState
{
    Ready,
    Moving,
    Attached,
    Warping,
    Finished
}
```

> **Por quê?** Durante o delay a bola não está andando, nem parada no início, nem grudada. Com um estado próprio, os outros métodos (`Reflect`, `AttachTo`, `OnMove`) já ignoram a bola sozinhos, porque todos conferem o estado antes de agir.

### 3b. Campos novos

Junto dos outros campos do `BallController`:

```csharp
public float warpDelay = 0.3f;          // quanto tempo a bola fica "dentro" do warp
private SpriteRenderer spriteRenderer;  // para esconder e mostrar a bola
```

E no `Awake()`:

```csharp
spriteRenderer = GetComponent<SpriteRenderer>();
```

No topo do arquivo, junto dos outros `using`:

```csharp
using System.Collections; // necessário para usar coroutines (IEnumerator)
```

### 3c. Métodos `WarpTo` e `WarpRoutine`

Adicionar perto do `Reflect`:

```csharp
// Manda a bola para outro ponto, mantendo a direção e a velocidade.
// Devolve true se a bola entrou no warp.
public bool WarpTo(Vector2 position)
{
    // só entra no warp a bola que está andando (não a parada no início nem a grudada)
    if (currentState != BallState.Moving) return false;

    StartCoroutine(WarpRoutine(position));
    return true;
}

private IEnumerator WarpRoutine(Vector2 position)
{
    currentState = BallState.Warping;

    // a bola para e some dentro do warp de entrada
    rig.linearVelocity = Vector2.zero;
    rig.bodyType = RigidbodyType2D.Kinematic; // nada empurra a bola enquanto ela está escondida
    spriteRenderer.enabled = false;

    // espera o delay (o jogo continua rodando normalmente enquanto isso)
    yield return new WaitForSeconds(warpDelay);

    // aparece no warp de saída
    rig.bodyType = RigidbodyType2D.Dynamic;
    rig.position = position;
    spriteRenderer.enabled = true;

    // volta a andar com a mesma direção: entrou ↗, sai ↗
    currentState = BallState.Moving;
    rig.linearVelocity = direction * speed;
}
```

### 3d. Reset no meio do warp

No começo do `ResetPosition()`, adicionar:

```csharp
// se a bola estava dentro de um warp, cancela a espera e mostra a bola de novo
StopAllCoroutines();
spriteRenderer.enabled = true;
```

> **Como funciona:**
> - **Coroutine** é um método que pode **pausar no meio** e continuar depois. O `yield return new WaitForSeconds(warpDelay)` é o `setTimeout` do Phaser: o código depois dele só roda quando o tempo passa, e o jogo não trava enquanto isso.
> - **`StartCoroutine(...)`** é o que começa a coroutine. Chamar `WarpRoutine(position)` sozinho não faz nada.
> - **Por que `Kinematic` durante o delay?** A bola parada continua no mundo, só invisível. Se uma barra com `BarMover` passar por ela, a física empurraria a bola para fora do lugar. Kinematic é o mesmo truque do `AttachTo`: a física para de mexer na bola.
> - **A direção não muda:** a bola guarda a `direction` o tempo todo, então no fim basta `direction * speed` para ela sair ↗ se entrou ↗. Quando os outros tipos de warp vierem (espelho, rotação), é aqui que a direção vai mudar.
> - **Por que `StopAllCoroutines()` no reset?** Se a bola resetar durante o delay (por exemplo, num botão de reiniciar no futuro), a coroutine acordaria depois e jogaria a bola no warp. Assim a espera é cancelada.
> - **Por que devolver `bool`?** O warp precisa saber se a bola entrou nele, para avisar o warp de destino (passo 4). Se a bola estava parada ou grudada, nada acontece e o destino não precisa ser avisado.
> - **Quanto de delay?** `0.3` segundos é um ponto de partida (o Phaser usava `0.1`). Como `warpDelay` é `public`, dá para ajustar no Inspector com o jogo rodando até ficar bom.

✅ **Teste:** o projeto compila sem erros no Console (ninguém chama o método ainda).

---

## Passo 4 — Script `Warp.cs`

Criar `Assets/Scripts/Warp.cs` e adicionar no GameObject `Warp`:

```csharp
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Warp : MonoBehaviour
{
    // o outro warp do par: é para lá que a bola vai
    public Warp destination;

    // fica true enquanto a bola que acabou de chegar ainda está em cima deste warp
    private bool ignoreBall = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // procura o script da bola no objeto que entrou no warp
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // a bola acabou de chegar por este warp: não manda ela de volta
        if (ignoreBall) return;

        // warp sem destino não teleporta (serve como warp "só de saída")
        if (destination == null) return;

        bool warped = ball.WarpTo(destination.transform.position);
        if (warped)
        {
            destination.ReceiveBall();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // a bola saiu de cima deste warp: da próxima vez que entrar, pode teleportar
        ignoreBall = false;
    }

    // Chamado pelo outro warp, logo antes de a bola aparecer aqui
    public void ReceiveBall()
    {
        ignoreBall = true;
    }
}
```

> **Como funciona:**
> - **O problema:** a bola aparece **em cima** do warp B. Na próxima atualização da física, o B percebe a bola entrando (`OnTriggerEnter2D`) e mandaria ela de volta para o A, que mandaria para o B... ping-pong infinito.
> - **No Phaser** isso foi resolvido com um cooldown de 500 ms. Funciona, mas depende do tempo: com a bola mais lenta, ou com um warp maior, ela ainda pode estar em cima do destino quando o tempo acaba.
> - **Aqui:** o A avisa o B (`ReceiveBall()`), e o B ignora a bola **até ela sair de cima dele** (`OnTriggerExit2D`). Não importa a velocidade nem o tamanho do warp.
> - **Por que `ReceiveBall()` em vez de mudar `destination.ignoreBall` direto?** Em C#, dá para mexer no campo `private` de outro objeto da **mesma classe**, mas fica difícil de entender quem mexe no quê. Com um método, fica claro que é o outro warp que avisa.

✅ **Teste:** ainda não dá para testar o teleporte (só existe um warp). O Console não mostra erros.

---

## Passo 5 — Prefab e o par na cena

1. Arrastar o `Warp` da Hierarchy para `Assets/Prefabs/`. Ele vira o prefab `Warp.prefab`.
2. Na cena, renomear o warp para `Warp A` e criar outro a partir do prefab, `Warp B`, em outro lugar da fase.
3. No **Warp A**, em **Destination**, arrastar o `Warp B`.
4. No **Warp B**, em **Destination**, arrastar o `Warp A`.

> **Por que um prefab?** Cada fase vai ter vários warps. Se um dia você trocar o sprite ou o tamanho do colisor, muda no prefab e todos mudam juntos.
>
> **Por que ligar os dois?** Cada warp só sabe para onde **ele** manda a bola. Se o B ficar sem destino, ele vira um warp só de saída: a bola entra no A, sai no B, e passar pelo B depois não faz nada. Pode ser útil num puzzle.

✅ **Teste:** posicione o `Warp A` no caminho da bola logo depois do lançamento e o `Warp B` em outro lugar.
- a bola entra no A, **some por um instante** e **sai do B na mesma direção** (↗ entra, ↗ sai);
- mudando o **Warp Delay** da bola no Inspector, o tempo escondida muda;
- a bola **não volta** para o A logo depois de sair do B;
- se, depois de rebater numa barra, a bola entrar no B, ela sai no A;
- depois de passar por um warp, a bola continua rebatendo nas barras normalmente;
- com a bola grudada numa `GrabBar` que passa por cima de um warp (barra com `BarMover`), nada acontece.

---

## Passo 6 — Câmera pula junto (depois do plano 2)

Só faz sentido depois do **Passo 7 do plano 2** (Cinemachine com Damping). Antes disso, pule este passo.

Com o Damping, a câmera **desliza** até o warp B depois do teleporte. Aqui o delay ajuda, e são duas opções:

**Opção 1 — a câmera desliza durante o delay.** Em vez de a bola ficar parada no warp A, ela vai para o B **logo no começo** (ainda escondida). A câmera tem o tempo do delay para chegar lá antes de a bola aparecer. Na `WarpRoutine`, mover o `rig.position = position;` para **antes** do `yield return`:

```csharp
    // a bola para, some e já vai (escondida) para o warp de saída
    rig.linearVelocity = Vector2.zero;
    rig.bodyType = RigidbodyType2D.Kinematic;
    spriteRenderer.enabled = false;
    rig.position = position; // a câmera começa a deslizar até aqui

    yield return new WaitForSeconds(warpDelay);

    rig.bodyType = RigidbodyType2D.Dynamic;
    spriteRenderer.enabled = true;
```

**Opção 2 — a câmera pula na hora.** Com o mesmo aviso do `ResetPosition()`, no lugar do `rig.position = position;` que fica depois do `yield return`:

```csharp
    // quanto a bola vai "pular" (do warp de entrada até o de saída)
    Vector3 jump = position - rig.position;

    rig.position = position;

    // avisa o Cinemachine que a bola foi teleportada, para a câmera pular junto
    CinemachineCore.OnTargetObjectWarped(transform, jump);
```

> **Qual escolher?** A opção 1 mostra ao jogador **para onde** a bola foi, e combina com o delay. Mas, se o warp B estiver longe e o delay for curto, a câmera ainda pode estar chegando quando a bola aparecer. Nesse caso, aumente o delay ou use a opção 2. Teste as duas.
>
> **Na opção 1 o ping-pong continua resolvido?** Sim. O `ReceiveBall()` já foi chamado no B antes de a bola chegar, e a bola chega Kinematic. Quando ela volta a ser Dynamic, o B percebe a bola e ignora, como antes.

✅ **Teste:** com o `Warp B` bem acima do `Warp A` (fora da tela), a bola aparece no B com a câmera já lá (deslizando na opção 1, pulando na opção 2).

---

## Fora do escopo deste plano

- **Warp espelho** (com formato de barra em pé ou deitada, que reflete a direção).
- **Warp com rotação** (com seta, que gira a direção).
- **Efeito visual** do teleporte (em vez de sumir de uma vez, a bola encolher no warp de entrada e crescer no de saída, com DOTween).
- **Warp que se move** (com `BarMover`).
- **Som** do teleporte.
