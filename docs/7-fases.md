# 7 — Fases: terminar uma fase e carregar a próxima

Hoje o Olo tem uma cena só (`Game.unity`). Este plano prepara o jogo para ter **várias fases**: a bola entra no buraco, a tela escurece, a próxima fase carrega e a tela clareia.

## Objetivo

- Cada fase é **uma cena** (`Level01.unity`, `Level02.unity`...), editada visualmente como hoje.
- A bola entra no **buraco** (`FinishHole`), a fase termina e a **próxima carrega** com fade.
- Um **GameManager** sobrevive à troca de cena e é quem faz a troca.
- **Dá para dar Play em qualquer fase** direto no editor, e tudo funciona.

## Progresso

- [x] [Passo 0 — Pré-requisito: `GameEvents`](#passo-0--pré-requisito-gameevents)
- [x] [Passo 1 — Ação `Finish` na bola + evento `BallFinished`](#passo-1--ação-finish-na-bola--evento-ballfinished)
- [x] [Passo 2 — Script `FinishHole.cs`](#passo-2--script-finishholecs)
- [x] [Passo 3 — Lista de fases (ScriptableObject)](#passo-3--lista-de-fases-scriptableobject)
- [x] [Passo 4 — Script `GameManager.cs`](#passo-4--script-gamemanagercs)
- [x] [Passo 5 — Prefab do GameManager (com o fade dentro)](#passo-5--prefab-do-gamemanager-com-o-fade-dentro)
- [x] [Passo 6 — `CameraBounds` usa o fade do GameManager](#passo-6--camerabounds-usa-o-fade-do-gamemanager)
- [ ] [Passo 7 — Criar as cenas das fases](#passo-7--criar-as-cenas-das-fases)
- [ ] [Passo 8 — Testar](#passo-8--testar)

## A ideia

Cada peça faz uma coisa só. Nenhuma conhece mais do que precisa:

```
FinishHole.OnTriggerEnter2D ──► ball.Finish()                  (objeto chama ação da bola, como sempre)
                                     │
BallController.Finish()     ──► GameEvents.AnnounceBallFinished()  (a bola anuncia: "terminei")
                                     │
GameManager escuta          ──► FadeOut
                            ──► carrega a próxima fase (LevelList diz qual é)
                            ──► FadeIn
```

- **A bola não sabe que existem fases.** Ela só para e anuncia que terminou.
- **O buraco não sabe que existe GameManager.** Ele só chama a ação da bola.
- **O GameManager não sabe de buraco nem de barra.** Ele só escuta "terminou" e troca a cena.
- **A ordem das fases** fica num asset (`LevelList`), editável no Inspector.

### Como o GameManager sobrevive às cenas (opção 2 do que conversamos)

O GameManager vira um **prefab** que fica em **todas as cenas de fase**. A primeira cópia que nasce fica e sobrevive às trocas (`DontDestroyOnLoad`). As cópias das fases seguintes percebem que já existe uma e se destroem.

```
Play na Level01: o GameManager da Level01 fica e sobrevive
   └─► carrega Level02: o GameManager da Level02 vê que já existe um → se destrói
Play direto na Level03: o GameManager da Level03 é o primeiro → ele fica
```

Ver [doc 5](5-arquitetura.md) para o porquê de cada padrão (evento, ScriptableObject, singleton).

---

## Passo 0 — Pré-requisito: `GameEvents`

Este plano usa o `GameEvents.cs`, criado no [doc 6, passo 1](6-break-bar.md#passo-1--criar-o-evento-ballreset-gameevents).

- Se o doc 6 já foi feito, o arquivo já existe: siga para o passo 1.
- Se não, crie só o `GameEvents.cs` como está no doc 6 (passo 1) antes de continuar.

---

## Passo 1 — Ação `Finish` na bola + evento `BallFinished`

**1.** No `GameEvents.cs`, adicionar um segundo evento, do mesmo jeito que o `BallReset`:

```csharp
    // a bola entrou no buraco: a fase terminou
    public static event System.Action BallFinished;

    public static void AnnounceBallFinished()
    {
        if (BallFinished != null)
        {
            BallFinished();
        }
    }
```

**2.** No `BallController.cs`, criar a ação `Finish`, junto com as outras ações (`Reflect`, `AttachTo`, `WarpTo`):

```csharp
    // A bola entrou no buraco: para de vez e avisa que a fase terminou.
    // Devolve true se a bola terminou.
    public bool Finish()
    {
        // só termina a bola que está andando (não a grudada nem a que está dentro de um warp)
        if (currentState != BallState.Moving) return false;

        currentState = BallState.Finished;

        // para a bola e desliga a física, para nada mais empurrar ela
        rig.linearVelocity = Vector2.zero;
        rig.bodyType = RigidbodyType2D.Kinematic;

        GameEvents.AnnounceBallFinished();
        return true;
    }
```

**3.** Ainda no `BallController.cs`, proteger o `ResetPosition()`: logo na primeira linha dele, antes do `StopAllCoroutines()`:

```csharp
    public void ResetPosition()
    {
        // a bola que já terminou a fase não volta para o início (a fase está trocando)
        if (currentState == BallState.Finished) return;

        // ... o resto continua igual
```

**Por quê?**
- O estado `Finished` já existia no `enum`, só não era usado. Agora ele tem uma ação que leva até ele.
- **`Finished` é uma trava.** No mesmo frame em que a bola toca o buraco, ela para e muda de estado. Parada dentro do buraco, ela não sai da tela, então o `CameraBounds` não dispara o reset. E, se algum dia alguma coisa chamar o reset mesmo assim, o guarda do passo 3 ignora. Assim, "entrou no buraco" e "saiu da tela" nunca acontecem juntos.
- Um segundo toque no buraco (dois triggers seguidos) também é ignorado: o `Finish()` só aceita a bola em `Moving`.
- No estado `Finished`, o `OnMove` não faz nada (ele só lança nos estados `Ready` e `Attached`), então o jogador não consegue relançar a bola enquanto a fase troca.
- **A bola anuncia, em vez de chamar o GameManager.** Assim a bola continua sem saber que existem fases, e no futuro outros sistemas (som de vitória, estrelas) podem escutar o mesmo evento.

✅ **Teste:** o projeto compila. Nada muda no jogo ainda, porque ninguém chama `Finish()`.

---

## Passo 2 — Script `FinishHole.cs`

O objeto `FinishHole` já existe na cena (só com o sprite). Falta ele detectar a bola.

**1.** Criar `Assets/Scripts/FinishHole.cs`:

```csharp
using UnityEngine;

public class FinishHole : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        ball.Finish();
    }
}
```

**2.** No `FinishHole` da cena:
- **Add Component → BoxCollider2D**, com **Is Trigger** marcado. Ajustar o tamanho para ficar um pouco **menor** que o sprite, assim a bola precisa entrar de verdade no buraco.
- **Add Component → FinishHole** (o script).
- Arrastar o `FinishHole` para **Assets/Prefabs**, para virar prefab e ser reutilizado nas outras fases.

**Por quê trigger?**
Com trigger, a bola **atravessa** o buraco em vez de bater nele: é o mesmo caso do `Warp`. O `Finish()` para a bola logo em seguida.

✅ **Teste:** a bola entra no buraco e **para**. Nada mais acontece (ainda não existe GameManager para escutar), e apertar o botão não relança a bola.

---

## Passo 3 — Lista de fases (ScriptableObject)

**1.** Criar `Assets/Scripts/LevelList.cs`:

```csharp
using UnityEngine;

// A ordem das fases do jogo. É um asset: fica na pasta do projeto, não numa cena.
[CreateAssetMenu(fileName = "LevelList", menuName = "Olo/Level List")]
public class LevelList : ScriptableObject
{
    // nomes das cenas, na ordem em que o jogador joga
    public string[] levelScenes;

    // Devolve o nome da fase que vem depois de "currentScene".
    public string GetNextLevel(string currentScene)
    {
        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (levelScenes[i] == currentScene)
            {
                int nextIndex = i + 1;

                // depois da última fase, volta para a primeira (até existir uma tela de fim de jogo)
                if (nextIndex >= levelScenes.Length)
                {
                    nextIndex = 0;
                }

                return levelScenes[nextIndex];
            }
        }

        // a cena atual não está na lista (ex.: uma cena de teste): começa pela primeira
        return levelScenes[0];
    }
}
```

**2.** Criar o asset: na pasta **Assets**, criar uma pasta **Data**, clicar com o botão direito dentro dela e ir em **Create → Olo → Level List**. Deixar o nome `LevelList`.

**3.** No Inspector do asset, preencher **Level Scenes** com os nomes das cenas (os nomes ainda serão criados no passo 7): `Level01`, `Level02`.

**Por quê?**
- **ScriptableObject** porque é um **dado que você configura** e o jogo só lê (ver [doc 5](5-arquitetura.md) e a conversa sobre GameConfig). Ele não pertence a nenhuma cena, então não some na troca.
- **Nomes como `string`** porque o Unity não deixa referenciar uma cena diretamente num script em tempo de jogo. O nome precisa ser **igual** ao do arquivo da cena, letra por letra.
- **A fase atual vem do nome da cena aberta**, e não de um número guardado. É isso que faz o Play direto na `Level03` funcionar: o GameManager pergunta "em qual cena estou?" e a lista responde qual vem depois.
- No futuro, este é o lugar natural para mais informações por fase (nome de exibição, jogadas para 3 estrelas...). Aí o `string[]` vira uma lista de uma pequena classe `LevelInfo`.

✅ **Teste:** o asset aparece na pasta Data, com os dois nomes preenchidos no Inspector.

---

## Passo 4 — Script `GameManager.cs`

Criar `Assets/Scripts/GameManager.cs`:

```csharp
using System.Collections; // necessário para usar coroutines (IEnumerator)
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // o GameManager "oficial": qualquer script acha ele por aqui
    public static GameManager Instance;

    public LevelList levelList;
    public CanvasFade fade;   // filho do GameManager: sobrevive à troca de cena junto com ele

    private bool isLoading;   // evita começar duas trocas de fase ao mesmo tempo

    void Awake()
    {
        // já existe um GameManager vivo (veio de uma fase anterior): este aqui sobra
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameEvents.BallFinished += OnBallFinished;
    }

    void OnDestroy()
    {
        // só o GameManager oficial se desinscreve e libera o lugar
        // (a cópia que se destruiu no Awake nunca se inscreveu)
        if (Instance == this)
        {
            GameEvents.BallFinished -= OnBallFinished;
            Instance = null;
        }
    }

    private void OnBallFinished()
    {
        if (isLoading) return;

        StartCoroutine(LoadNextLevel());
    }

    private IEnumerator LoadNextLevel()
    {
        isLoading = true;

        // 1. escurece a tela e espera terminar
        yield return fade.FadeOut();

        // 2. com a tela preta, carrega a próxima fase e espera ela terminar de carregar
        string currentScene = SceneManager.GetActiveScene().name;
        string nextScene = levelList.GetNextLevel(currentScene);
        yield return SceneManager.LoadSceneAsync(nextScene);

        // 3. clareia a tela, já na fase nova
        yield return fade.FadeIn();

        isLoading = false;
    }
}
```

**Por quê?**
- **A proteção contra duplicata no `Awake`** é o coração da opção 2: cada fase tem uma cópia, mas só a primeira fica.
- **Inscrever no `Awake` e desinscrever no `OnDestroy`** (e não no `OnEnable`/`OnDisable`, como a BreakBar). Assim, a cópia que se destrói **nunca chega a se inscrever**, porque ela sai do `Awake` pelo `return` antes da linha do `+=`. O GameManager oficial nunca é desativado, então o par `Awake`/`OnDestroy` é seguro.
- **`Instance = null` no `OnDestroy`** é importante neste projeto: o Domain Reload está desligado, então o `static Instance` sobreviveria de um Play para o outro apontando para um GameManager que já não existe.
- **`LoadSceneAsync` com `yield return`** espera a cena nova estar pronta antes do `FadeIn`. Com o `LoadScene` normal, a troca só acontece no frame seguinte, e o fade poderia clarear antes da hora.
- **A coroutine roda no GameManager**, que sobrevive à troca. Se rodasse num objeto da fase, ela morreria no meio do caminho, com a tela preta.

✅ **Teste:** o projeto compila.

---

## Passo 5 — Prefab do GameManager (com o fade dentro)

Hoje o `CanvasFade` está solto na cena. Se ele continuasse assim, seria destruído **no meio** da troca de fase, com a tela preta. Por isso ele passa a ser filho do GameManager e sobrevive junto.

1. **Hierarchy → Create Empty**, renomear para `GameManager`. Ele precisa ficar na **raiz** da Hierarchy (não pode ser filho de nada), porque o `DontDestroyOnLoad` só funciona em objetos da raiz.
2. **Add Component → GameManager** (o script).
3. Arrastar o objeto `CanvasFade` da Hierarchy **para dentro** do `GameManager`, para virar filho dele. Ele não precisa ser um prefab próprio: no item 5, os filhos entram junto no prefab do GameManager.
4. No componente GameManager:
   - **Level List** ← o asset `LevelList` da pasta Data;
   - **Fade** ← o objeto `Image` (filho do `CanvasFade`). É nele que está o componente `CanvasFade`; arrastar o pai não funciona, porque um campo de componente só aceita o objeto que tem aquele componente.
5. Arrastar o `GameManager` para **Assets/Prefabs**, para virar prefab.

**Por quê o Canvas pode ser filho de um objeto vazio?**
O Canvas em **Screen Space - Overlay** desenha por cima da tela inteira, não importa onde ele está na Hierarchy.

✅ **Teste:** dar Play e, na Hierarchy, ver que o `GameManager` (com o `CanvasFade` dentro) foi para uma seção chamada **DontDestroyOnLoad**. Esse é o sinal de que ele vai sobreviver às trocas.

---

## Passo 6 — `CameraBounds` usa o fade do GameManager

Hoje o `CameraBounds` recebe o `canvasFade` pelo Inspector. Isso **para de funcionar** com várias fases: na `Level02`, a referência do Inspector aponta para o fade da **cópia** do GameManager daquela cena, e essa cópia se destrói no `Awake`. O `CameraBounds` ficaria apontando para um fade que não existe mais.

A solução é ele pedir o fade ao GameManager **oficial**, na hora em que precisa.

No `CameraBounds.cs`:

**1.** Apagar o campo `public CanvasFade canvasFade;`.

**2.** Trocar o `OnTriggerExit2D` e o começo do `ResetSequence`:

```csharp
    private void OnTriggerExit2D(Collider2D collision)
    {
        // ao parar o Play o Unity destrói os objetos e dispara este evento: nesse caso, ignora
        if (!gameObject.activeInHierarchy) return;
        if (GameManager.Instance == null) return;

        if (collision.CompareTag("Ball"))
        {
            BallController ball = collision.gameObject.GetComponent<BallController>();

            // a sequência roda ao longo de vários frames, então precisa ser uma coroutine
            StartCoroutine(ResetSequence(ball));
        }
    }

    private IEnumerator ResetSequence(BallController ball)
    {
        // o fade mora no GameManager (ele sobrevive à troca de fase)
        CanvasFade canvasFade = GameManager.Instance.fade;

        // 1. escurece a tela e espera terminar
        yield return canvasFade.FadeOut();
        ...  // o resto continua igual
    }
```

**Por quê pedir na hora, e não guardar no `Awake`?**
Na primeira cena, a Unity não garante em que ordem os `Awake` rodam. Se o `Awake` do `CameraBounds` rodasse antes do `Awake` do GameManager, o `Instance` ainda estaria vazio. Pedindo só quando a bola sai da tela, o GameManager com certeza já existe.

✅ **Teste:** a bola sai da tela e o reset com fade funciona **igual a antes**.

---

## Passo 7 — Criar as cenas das fases

1. Na pasta **Assets/Scenes**, renomear `Game.unity` para `Level01.unity`.
2. Selecionar `Level01.unity` e apertar **Cmd + D** para duplicar. Renomear a cópia para `Level02.unity`.
3. Abrir a `Level02` e mudar alguma coisa para dar para diferenciar (mover barras, o buraco ou a posição inicial da bola).
4. Adicionar as duas cenas à lista de cenas do build: **File → Build Profiles**. O profile **Web - Mobile - Release** pode ter uma **Scene List** própria, que substitui a geral. Confira as duas e coloque `Level01` e `Level02`, nessa ordem.

**Por quê duplicar?**
A cena duplicada já vem com tudo que toda fase precisa: bola, câmera, `CameraBounds`, `FinishHole` e o **prefab do GameManager**. Criando as fases sempre assim, você nunca esquece o GameManager.

**Por quê o Build Profile?**
O `LoadSceneAsync` só encontra cenas que estão na lista do build. Se a cena não estiver lá, o Console mostra um erro dizendo que ela não pode ser carregada.

✅ **Teste:** as duas cenas aparecem na Scene List do profile.

---

## Passo 8 — Testar

- [ ] **Play na `Level01`**, levar a bola ao buraco: a tela escurece, a `Level02` carrega e a tela clareia.
- [ ] **Na `Level02`**, a bola sai da tela: o reset com fade funciona (é o fade do GameManager da `Level01`, que sobreviveu).
- [ ] **Terminar a `Level02`**: volta para a `Level01` (por enquanto a lista recomeça).
- [ ] **Play direto na `Level02`**: tudo funciona, incluindo o reset e terminar a fase.
- [ ] Durante o jogo, a Hierarchy mostra **um GameManager só**, dentro de **DontDestroyOnLoad**.
- [ ] **Trocar de fase não dispara o fade de reset**: na troca, o `CameraBounds` da fase antiga é destruído e pode disparar o `OnTriggerExit2D`. O `activeInHierarchy` deve barrar isso, como já barra ao parar o Play.
- [ ] **Parar o Play e dar Play de novo** (várias vezes): sem erros no Console. Este teste confere o `Instance = null` com o Domain Reload desligado.

---

## Futuro

- **Tela de fim de jogo:** em vez de voltar para a primeira fase, o `GetNextLevel` avisa que acabou, e o GameManager carrega uma cena de vitória.
- **Menu e seleção de fases:** o menu vira a primeira cena do jogo publicado e também recebe o prefab do GameManager. As fases continuam com o prefab, para testar direto.
- **Progresso salvo** (fases completadas, estrelas): um estado separado, salvo em `PlayerPrefs` ou no Poki SDK. O GameManager atualiza esse estado quando escuta o `BallFinished`.
- **Poki SDK:** o GameManager é o lugar natural para avisar o início e o fim de cada partida.
