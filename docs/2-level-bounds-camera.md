# 2 — LevelBounds + câmera que segue a bola

Trocar as 4 `Walls` (presas ao tamanho da tela) por um limite da **fase inteira**, e fazer a câmera subir acompanhando a bola.

## Objetivo

- A bola volta para a posição inicial quando **sai da fase** (não quando sai da tela).
- A câmera **segue a bola na vertical**, com suavidade.
- A câmera **nunca mostra o que está fora da fase**.
- Quando a bola reseta, a câmera **volta junto na hora**, sem ficar "deslizando" lá de cima.
- **Todo mundo vê a mesma área** do jogo, em qualquer celular (letterbox, como o `Scale.FIT` do Phaser).

## Progresso

- [ ] [Passo 0 — Game view em retrato](#passo-0--game-view-em-retrato)
- [ ] [Passo 1 — Criar o retângulo da fase (LevelBounds)](#passo-1--criar-o-retângulo-da-fase-levelbounds)
- [ ] [Passo 2 — Script `LevelBounds.cs`](#passo-2--script-levelboundscs)
- [ ] [Passo 3 — Remover as Walls](#passo-3--remover-as-walls)
- [ ] [Passo 4 — Instalar o Cinemachine](#passo-4--instalar-o-cinemachine)
- [ ] [Passo 5 — Câmera seguindo a bola](#passo-5--câmera-seguindo-a-bola)
- [ ] [Passo 6 — Câmera presa dentro da fase (Confiner 2D)](#passo-6--câmera-presa-dentro-da-fase-confiner-2d)
- [ ] [Passo 7 — Câmera volta junto no reset](#passo-7--câmera-volta-junto-no-reset)
- [ ] [Passo 8 — Mesma área em qualquer tela (letterbox)](#passo-8--mesma-área-em-qualquer-tela-letterbox)

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
| `scale: { mode: Phaser.Scale.FIT }` (740×1360 com faixas) | `Camera.rect` calculado para manter 9:16 (`CameraLetterbox.cs`) |

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

## Passo 8 — Mesma área em qualquer tela (letterbox)

Faça este teste **antes**: na aba **Game**, troque a resolução para um iPhone recente (ex.: **1179 × 2556**, proporção 9:19.5) e depois para um iPad (ex.: **1536 × 2048**, 3:4).

- No iPhone, os **lados da fase são cortados**: barras perto da borda somem.
- No iPad, aparece **espaço fora da fase** dos lados.

Isso acontece porque a câmera fixa a **altura** (Size = 5) e a largura varia com a tela.

A solução é a mesma do Phaser (`Scale.FIT`): a câmera desenha **sempre em 9:16**, e o que sobra da tela vira faixa.

### 8a. Script `CameraLetterbox.cs` na `Main Camera`

Criar `Assets/Scripts/CameraLetterbox.cs` e adicionar na **`Main Camera`** (não na `FollowCamera`):

```csharp
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraLetterbox : MonoBehaviour
{
    // proporção em que o jogo foi desenhado (retrato 9:16)
    [SerializeField] private float targetWidth = 9f;
    [SerializeField] private float targetHeight = 16f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        float targetAspect = targetWidth / targetHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        if (screenAspect < targetAspect)
        {
            // tela mais alta que 9:16 (ex.: iPhone): faixas em cima e embaixo
            float height = screenAspect / targetAspect;
            cam.rect = new Rect(0, (1 - height) / 2, 1, height);
        }
        else
        {
            // tela mais larga que 9:16 (ex.: iPad): faixas nos lados
            float width = targetAspect / screenAspect;
            cam.rect = new Rect((1 - width) / 2, 0, width, 1);
        }
    }
}
```

> **Como funciona:**
> - **`cam.rect`** é a parte da tela onde a câmera desenha, em valores de 0 a 1. `(0, 0, 1, 1)` é a tela inteira. `(0, 0.1, 1, 0.8)` desenha só nos 80% do meio, deixando 10% em cima e 10% embaixo.
> - O script compara a proporção da tela com 9:16 e calcula um retângulo centralizado que **sempre** tem 9:16. Dentro dele, a câmera continua mostrando 5.625 × 10, exatamente a largura do `LevelBounds`.
> - **Por que na `Main Camera`?** O Cinemachine (`FollowCamera`) decide **para onde** a câmera olha. O `rect` decide **em que parte da tela** ela desenha. São coisas separadas: o Brain não mexe no `rect`.
> - **Por que no `Update`?** Para funcionar quando você troca a resolução da Game view no editor. No celular a tela não muda, e o cálculo é barato.

### 8b. Câmera de fundo para pintar as faixas

A área fora do `rect` **não é desenhada por ninguém**, e em alguns celulares isso aparece como "sujeira" (restos de frames anteriores). Uma segunda câmera, que não desenha nada além de uma cor, resolve:

1. **Hierarchy → Create → Camera**, renomear para `LetterboxBackground`.
2. **Remover** o componente **Audio Listener** dela (só pode existir um na cena, e ele já está na `Main Camera`).
3. **Rendering → Culling Mask**: `Nothing` (não desenha nenhum objeto).
4. **Environment → Background Type**: `Solid Color`, com a cor das faixas (ex.: preto).
5. **Priority** (ou **Depth**): **menor** que o da `Main Camera` (ex.: `-1`), para ela desenhar **antes**, por baixo.

> **Por quê?** Ela pinta a tela inteira com a cor de fundo, e a `Main Camera` desenha o jogo por cima, só no retângulo 9:16. As faixas ficam com uma cor limpa.

✅ **Teste** (trocando a resolução da Game view):
- **1080 × 1920** (9:16): tela cheia, sem faixas;
- **iPhone** (9:19.5): faixas em cima e embaixo, e a fase aparece **inteira** na largura;
- **iPad** (3:4): faixas nos lados, sem mostrar nada fora da fase;
- nos três casos, a **mesma área** da fase aparece.

> **E as faixas ficam vazias para sempre?** Não precisa. A interface (pontuação, pause) vai num **Canvas**, que cobre a tela **inteira**, faixas incluídas. Num celular alto, a faixa de cima é um bom lugar para o HUD. Isso fica para um plano futuro.

---

## Fora do escopo deste plano

- Fundo (background) cobrindo a fase inteira (o `tileSprite` do Phaser).
- Calcular a altura da fase automaticamente (ex.: a partir da barra mais alta).
- Várias fases / troca de cena.
