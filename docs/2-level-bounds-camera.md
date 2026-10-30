# 2 — LevelBounds + câmera que segue a bola

Trocar as 4 `Walls` (presas ao tamanho da tela) por um limite da **fase inteira**, e fazer a câmera subir acompanhando a bola.

## Objetivo

- A bola volta para a posição inicial quando **sai da fase** (não quando sai da tela).
- A câmera **segue a bola na vertical**, com suavidade.
- A câmera **nunca mostra o que está fora da fase**.
- Quando a bola reseta, a câmera **volta junto na hora**, sem ficar "deslizando" lá de cima.

## Progresso

- [ ] [Passo 0 — Game view em retrato](#passo-0--game-view-em-retrato)
- [ ] [Passo 1 — Criar o retângulo da fase (LevelBounds)](#passo-1--criar-o-retângulo-da-fase-levelbounds)
- [ ] [Passo 2 — Script `LevelBounds.cs`](#passo-2--script-levelboundscs)
- [ ] [Passo 3 — Remover as Walls](#passo-3--remover-as-walls)
- [ ] [Passo 4 — Instalar o Cinemachine](#passo-4--instalar-o-cinemachine)
- [ ] [Passo 5 — Câmera seguindo a bola](#passo-5--câmera-seguindo-a-bola)
- [ ] [Passo 6 — Câmera presa dentro da fase (Confiner 2D)](#passo-6--câmera-presa-dentro-da-fase-confiner-2d)
- [ ] [Passo 7 — Câmera volta junto no reset](#passo-7--câmera-volta-junto-no-reset)

## A ideia

Um **retângulo invisível** cobre a fase inteira. Ele fica parado no mundo; quem se move é a câmera, por dentro dele.

```
        ┌───────────────┐  ← topo da fase
        │               │
        │    barras     │
        │               │
        │ ┌───────────┐ │
        │ │  câmera   │ │  ← o que aparece na tela (vai subindo)
        │ │     ●     │ │
        │ └───────────┘ │
        │               │
        │       ●       │  ← posição inicial da bola
        └───────────────┘  ← começo da fase
           LevelBounds
```

Esse **mesmo retângulo** faz duas coisas:

1. **Reset da bola:** quando a bola sai dele, o `LevelBounds` chama `ball.ResetPosition()`. É o mesmo padrão da `DefaultBar`: o objeto decide e chama uma ação da bola.
2. **Limite da câmera:** o Cinemachine usa o retângulo para não deixar a câmera mostrar nada fora da fase.

A altura é **por fase**: cada cena tem o próprio `LevelBounds`, ajustado no editor. Nenhum número fica no código.

## Phaser → Unity

| Phaser (`stage1.js` / `levelLoader.js`) | Unity |
|---|---|
| `data.levelHeight` | Altura do PolygonCollider2D do `LevelBounds` (por cena) |
| `if (ball.x < -margin \|\| ball.y > levelHeight + margin) restoreBallInitialPosition()` | `OnTriggerExit2D` no `LevelBounds` chama `ball.ResetPosition()` |
| `cam.scrollY = Linear(cam.scrollY, target, 0.08)` | Cinemachine Camera seguindo a bola, com **Damping** |
| `cameras.main.setBounds(0, 0, width, levelHeight)` | **Cinemachine Confiner 2D** usando o mesmo colisor |
| `if (distância > viewportHeight) scrollY = target` (encaixe no reset) | `CinemachineCore.OnTargetObjectWarped(...)` |

---

## Passo 0 — Game view em retrato

Na aba **Game**, no menu de resolução (onde está escrito *Free Aspect* ou *16:9*), escolher ou criar **1080 × 1920** (retrato).

> **Por quê?** O jogo é vertical, como no Phaser. Com a câmera em `Size = 5`, a tela em retrato tem **10 de altura e 5,625 de largura**, e é essa largura que a fase vai ter. Em paisagem a câmera mostraria muito espaço dos lados, fora da fase.

✅ **Teste:** a Game view fica em pé, e a bola aparece perto da borda de baixo.

---

## Passo 1 — Criar o retângulo da fase (LevelBounds)

1. **Hierarchy → Create Empty**, renomear para `LevelBounds`, **Position = (0, 0, 0)**.
2. **Add Component → PolygonCollider2D**.
3. Ligar **Is Trigger**.
4. Em **Points → Paths → Element 0**, deixar **4 pontos**:

| Ponto | X | Y |
|---|---|---|
| 0 | -2.8125 | -5.5 |
| 1 | 2.8125 | -5.5 |
| 2 | 2.8125 | 25 |
| 3 | -2.8125 | 25 |

> **De onde vêm esses números?**
> - **X = ±2.8125**: metade da largura da tela em retrato (5,625 ÷ 2). A fase tem exatamente a largura da câmera.
> - **Y = -5.5**: um pouco abaixo da borda de baixo da câmera (-5). É a "margem" que o Phaser tinha (`margin = 30`), para a bola não resetar logo ao nascer.
> - **Y = 25**: o topo da fase. É só um valor de teste (uns 3 tamanhos de tela). Cada fase vai ter o seu.
>
> Também dá para ajustar arrastando: botão **Edit Collider** no PolygonCollider2D e mover os pontos na Scene view.
>
> **Por quê PolygonCollider2D e não BoxCollider2D?** O Confiner 2D do Cinemachine (passo 6) precisa de um colisor em forma de polígono. Usando o mesmo colisor para os dois, você ajusta o tamanho da fase num lugar só.
>
> **Por quê Is Trigger?** Um trigger **detecta** a bola sem **bloquear**. A bola fica dentro dele o tempo todo, sem bater em nada.

✅ **Teste:** na Scene view aparece um retângulo verde alto, com a câmera (retângulo branco) na parte de baixo dele.

---

## Passo 2 — Script `LevelBounds.cs`

Criar `Assets/Scripts/LevelBounds.cs` e adicionar no GameObject `LevelBounds`:

```csharp
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class LevelBounds : MonoBehaviour
{
    // Chamado quando algo SAI de dentro do retângulo da fase
    void OnTriggerExit2D(Collider2D collision)
    {
        // procura o script da bola no objeto que saiu
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return; // não foi a bola, então ignora

        ball.ResetPosition();
    }
}
```

> **Como funciona:**
> - **`OnTriggerExit2D`** (e não `Enter`): as Walls avisavam quando a bola **entrava** nelas. Aqui é o contrário: a bola está sempre **dentro** do retângulo, e o que interessa é quando ela **sai**.
> - **Mesma arquitetura do plano 1:** o `LevelBounds` decide o que acontece e chama a ação `ResetPosition()` da bola. A bola não sabe que o `LevelBounds` existe.
> - **Por que não usar a tag "Ball"?** O `GetComponent<BallController>()` já garante que é a bola, e não quebra se alguém esquecer de colocar a tag.

✅ **Teste** (ainda com as Walls na cena, mas desligadas: desmarcar a caixinha ao lado do nome `Walls` no Inspector):
- lançar a bola para fora **pelos lados**: ela volta para a posição inicial;
- deixar a aba **Scene** visível: a bola sai da área da câmera para cima e **não** reseta (ainda está dentro da fase); só reseta quando passa do topo (Y = 25).

---

## Passo 3 — Remover as Walls

Com o `LevelBounds` funcionando, as Walls não servem mais:

1. Apagar o GameObject `Walls` (e os 4 filhos).
2. Apagar o script `Assets/Scripts/Wall.cs`.
3. (Opcional) Remover a tag **Wall** em **Edit → Project Settings → Tags and Layers**.

> **Por quê apagar e não só desligar?** Objetos e scripts que não são usados confundem depois ("isso ainda faz alguma coisa?").

✅ **Teste:** Play, lançar a bola: tudo continua funcionando igual ao passo 2, e o Console não mostra erros.

---

## Passo 4 — Instalar o Cinemachine

**Window → Package Manager → Unity Registry → Cinemachine → Install.**

> **O que é?** O pacote oficial da Unity para câmeras. Em vez de escrever um script de câmera (como o `update()` do Phaser com `lerp` e `Clamp`), você configura o comportamento no Inspector: seguir, suavizar, limitar.
>
> Diferente do DOTween, o Cinemachine é um **pacote** do Package Manager: ele entra no `Packages/manifest.json` e é baixado sozinho em outra máquina.

✅ **Teste:** o menu **GameObject → Cinemachine** aparece, e o Console não mostra erros.

---

## Passo 5 — Câmera seguindo a bola

1. **GameObject → Cinemachine → Cinemachine Camera** (ou a opção de câmera 2D, se aparecer). Renomear para `FollowCamera`.
   - A Unity adiciona sozinha um **Cinemachine Brain** na `Main Camera`. É ele que pega o que a `FollowCamera` decide e aplica na câmera de verdade.
2. Na `FollowCamera`, em **Tracking Target**, arrastar o `Ball`.
3. Em **Position Control**, escolher **Position Composer** (se não estiver já).
4. No **Position Composer**:
   - **Damping**: `X = 0`, `Y = 0.5` (suavidade na vertical, como o `0.08` do `lerp` do Phaser);
   - **Lens → Orthographic Size**: `5` (o mesmo da `Main Camera`).

> **Como funciona:** a `Main Camera` não se move mais sozinha. A `FollowCamera` é uma câmera "virtual" que calcula onde a câmera deveria estar (em cima da bola, com suavidade), e o Brain aplica isso na `Main Camera`. Não precisa de nenhum script.
>
> **Damping** é o "atraso" para a câmera alcançar a bola: `0` gruda na hora, valores maiores deixam mais suave. Vale testar valores entre `0.2` e `1`.

✅ **Teste:** Play, lançar a bola: a câmera sobe acompanhando. (Ela ainda mostra fora da fase, por exemplo abaixo do começo: isso é o próximo passo.)

---

## Passo 6 — Câmera presa dentro da fase (Confiner 2D)

1. Na `FollowCamera`: **Add Extension → Cinemachine Confiner 2D**.
2. Em **Bounding Shape 2D**, arrastar o `LevelBounds` (o PolygonCollider2D dele).

> **Como funciona:** o Confiner não deixa a câmera mostrar nada fora do polígono. É o `cameras.main.setBounds(...)` do Phaser.
>
> - No começo, a bola está perto do fundo da fase: a câmera **não desce** para centralizar a bola, fica parada mostrando o começo da fase.
> - Como o polígono tem a largura da tela, a câmera **não tem para onde ir nos lados**: ela só sobe e desce.
> - Se um dia você mudar o tamanho do `LevelBounds` com o jogo rodando, o Confiner precisa ser avisado (`InvalidateBoundingShapeCache()`). Com o tamanho ajustado no editor, não precisa de nada.

✅ **Teste:**
- no início, a bola aparece perto da borda de baixo (a câmera não mostra abaixo da fase);
- subindo, a câmera acompanha até o topo e para lá (não mostra acima de Y = 25);
- a câmera nunca anda para os lados.

---

## Passo 7 — Câmera volta junto no reset

Faça este teste **antes** de mexer no código: deixe a bola subir bastante e sair pelo lado. A câmera vai **deslizar** lá de cima até a bola, por causa do Damping. No Phaser você resolvia isso "encaixando" a câmera direto quando a distância era grande.

No Cinemachine, o jeito certo é avisar que a bola foi **teleportada**. No `BallController.cs`:

```csharp
using Unity.Cinemachine; // no topo do arquivo
```

```csharp
public void ResetPosition()
{
    currentState = BallState.Ready;

    rig.linearVelocity = Vector2.zero;
    rig.angularVelocity = 0f;

    // quanto a bola vai "pular" (da posição atual até a inicial)
    Vector3 jump = initialPosition - transform.position;

    rig.position = initialPosition;
    direction = initialDirection;

    // avisa o Cinemachine que a bola foi teleportada, para a câmera pular junto
    CinemachineCore.OnTargetObjectWarped(transform, jump);
}
```

> **Como funciona:** sem o aviso, o Cinemachine acha que a bola andou muito rápido e aplica o Damping, deslizando a câmera. Com o `OnTargetObjectWarped`, ele entende que foi um teleporte e move a câmera pela mesma distância, na hora.
>
> **E a arquitetura?** A bola chamar o Cinemachine é aceitável aqui: ela é quem se teleporta, então é ela quem avisa. O `LevelBounds` continua só decidindo **quando** resetar.

✅ **Teste:** a bola sobe bastante, sai pelo lado e a câmera **volta na hora** para o começo da fase, sem deslizar.

---

## Fora do escopo deste plano

- Fundo (background) cobrindo a fase inteira (o `tileSprite` do Phaser).
- Calcular a altura da fase automaticamente (ex.: a partir da barra mais alta).
- Várias fases / troca de cena.
