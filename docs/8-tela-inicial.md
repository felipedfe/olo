# 8 — Tela inicial: uma cena de menu antes das fases

Hoje, ao abrir o jogo, você já cai direto na fase. Este plano cria uma cena **`Menu`** com o título e um botão **Play**. Ela vira a **primeira cena do jogo publicado**, e as fases continuam podendo ser testadas direto no editor.

## Objetivo

- O jogo publicado começa na cena `Menu`.
- O botão **Play** escurece a tela, carrega a **primeira fase** da `LevelList` e clareia a tela.
- Ao terminar a **última fase**, o jogo **volta para o menu** (hoje ele recomeça na primeira fase).
- **Dar Play direto numa fase no editor continua funcionando**, sem passar pelo menu.

## Progresso

- [ ] [Passo 0 — Pré-requisito: doc 7 (fases)](#passo-0--pré-requisito-doc-7-fases)
- [ ] [Passo 1 — `LevelList` avisa quando acabaram as fases](#passo-1--levellist-avisa-quando-acabaram-as-fases)
- [ ] [Passo 2 — `GameManager` ganha `StartGame()` e volta ao menu no fim](#passo-2--gamemanager-ganha-startgame-e-volta-ao-menu-no-fim)
- [ ] [Passo 3 — Script `MenuScreen.cs`](#passo-3--script-menuscreencs)
- [ ] [Passo 4 — Criar a cena `Menu`](#passo-4--criar-a-cena-menu)
- [ ] [Passo 5 — Montar a tela (título + botão Play)](#passo-5--montar-a-tela-título--botão-play)
- [ ] [Passo 6 — `Menu` como primeira cena do build](#passo-6--menu-como-primeira-cena-do-build)
- [ ] [Passo 7 — Testar](#passo-7--testar)

## A ideia

As cenas **não conversam entre si**: quando uma carrega, tudo da anterior é destruído. Quem leva o jogo de uma cena para outra é o **GameManager**, que sobrevive às trocas (ver [doc 7](7-fases.md#como-o-gamemanager-sobrevive-às-cenas-opção-2-do-que-conversamos)).

```
Menu ──(botão Play)──► GameManager.StartGame() ──► Level01 ──► Level02 ──► ... ──► última fase
 ▲                                                                                     │
 └──────────────────────── GameManager carrega o Menu de volta ◄──────────────────────┘
```

- **O botão não sabe qual é a primeira fase.** Ele só pede ao GameManager: "começa o jogo".
- **O GameManager não sabe que existe um botão.** Ele só oferece a ação `StartGame()`.
- **Quem sabe a ordem das fases** continua sendo a `LevelList`.

É o mesmo padrão do resto do Olo: quem tem a responsabilidade **oferece ações**, e quem precisa **chama a ação**.

### De onde o jogo começa?

| Onde | Primeira cena |
|---|---|
| **Editor** | A cena que está **aberta** quando você aperta Play |
| **Build (WebGL/Poki)** | A cena de **índice 0** na Scene List do Build Profile |

Por isso a `Menu` fica no índice 0, e cada fase continua tendo o prefab do GameManager: assim dá para testar qualquer fase sozinha.

---

## Passo 0 — Pré-requisito: doc 7 (fases)

Este plano usa o `GameManager`, a `LevelList` e as cenas `Level01`/`Level02` do [doc 7](7-fases.md). **Termine o doc 7 antes**, até o passo 8 (testar) funcionar.

---

## Passo 1 — `LevelList` avisa quando acabaram as fases

Hoje o `GetNextLevel` volta para a primeira fase depois da última. Agora ele precisa **avisar** que as fases acabaram, para o GameManager decidir o que fazer (voltar ao menu).

No `LevelList.cs`, trocar o trecho que volta para a primeira fase:

```csharp
                int nextIndex = i + 1;

                // depois da última fase não tem próxima: devolve vazio
                // (quem decide o que fazer, como voltar ao menu, é o GameManager)
                if (nextIndex >= levelScenes.Length)
                {
                    return "";
                }

                return levelScenes[nextIndex];
```

O resto do método continua igual, incluindo o `return levelScenes[0];` do final (cena que não está na lista).

**Por quê devolver vazio, e não o nome do menu?**
A `LevelList` é a lista **das fases**. O menu não é uma fase. Ela responde só a pergunta "qual a próxima fase?", e "nenhuma" é uma resposta válida. Decidir **para onde ir** quando não tem próxima é trabalho do GameManager.

✅ **Teste:** o projeto compila.

---

## Passo 2 — `GameManager` ganha `StartGame()` e volta ao menu no fim

Hoje o `LoadNextLevel()` faz duas coisas juntas: **descobre** qual é a próxima cena e **carrega com fade**. O menu precisa só da segunda parte (carregar com fade a primeira fase). Então a gente separa as duas.

No `GameManager.cs`:

**1.** Adicionar o nome da cena do menu, junto com os outros campos:

```csharp
    public LevelList levelList;
    public CanvasFade fade;
    public string menuScene = "Menu";   // nome do arquivo da cena do menu, letra por letra
```

**2.** Trocar o `OnBallFinished()` e o `LoadNextLevel()` por estes três métodos:

```csharp
    // Ação pública: o botão Play do menu chama esta.
    public void StartGame()
    {
        if (isLoading) return;

        string firstLevel = levelList.levelScenes[0];
        StartCoroutine(LoadSceneWithFade(firstLevel));
    }

    private void OnBallFinished()
    {
        if (isLoading) return;

        string currentScene = SceneManager.GetActiveScene().name;
        string nextScene = levelList.GetNextLevel(currentScene);

        // acabaram as fases: volta para o menu
        if (nextScene == "")
        {
            nextScene = menuScene;
        }

        StartCoroutine(LoadSceneWithFade(nextScene));
    }

    // Escurece, troca de cena e clareia. Serve para qualquer cena: fase ou menu.
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        isLoading = true;

        // 1. escurece a tela e espera terminar
        yield return fade.FadeOut();

        // 2. com a tela preta, carrega a cena e espera ela terminar de carregar
        yield return SceneManager.LoadSceneAsync(sceneName);

        // 3. clareia a tela, já na cena nova
        yield return fade.FadeIn();

        isLoading = false;
    }
```

**Por quê?**
- **`LoadSceneWithFade` recebe o nome da cena.** Agora o mesmo código serve para "começar o jogo", "próxima fase" e "voltar ao menu". Sem ele, cada caminho teria a própria cópia do fade + carregamento.
- **O `isLoading` também protege o `StartGame()`.** Se o jogador clicar duas vezes rápido no Play, só a primeira troca começa.
- **`StartGame()` é `public`; o resto é `private`.** De fora, só dá para pedir "começa o jogo". Como a troca acontece por dentro é problema do GameManager.

✅ **Teste:** o projeto compila, e o teste do [doc 7, passo 8](7-fases.md#passo-8--testar) continua funcionando: terminar a `Level01` carrega a `Level02`. Terminar a `Level02` agora dá **um erro no Console** dizendo que a cena `Menu` não pode ser carregada. Isso é esperado: ela ainda não existe (passos 4 e 6).

---

## Passo 3 — Script `MenuScreen.cs`

Criar `Assets/Scripts/MenuScreen.cs`:

```csharp
using UnityEngine;

// Fica na tela do menu. Os botões chamam os métodos daqui.
public class MenuScreen : MonoBehaviour
{
    public void OnPlayClicked()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.StartGame();
    }
}
```

**Por quê um script só para isso? O botão não pode chamar o GameManager direto?**
Pelo Inspector até daria: arrastar o GameManager da cena para o **On Click** do botão. Mas aí aconteceria o mesmo problema do `CameraBounds` no [doc 7, passo 6](7-fases.md#passo-6--camerabounds-usa-o-fade-do-gamemanager):

```
Play na Level01 → termina a última fase → volta para o Menu
   └─► o GameManager do Menu vê que já existe um (o da Level01) → se destrói
   └─► o botão aponta para o GameManager que se destruiu → clicar não faz nada
```

O `MenuScreen` pede ao GameManager **oficial** (`GameManager.Instance`) na hora do clique. Assim funciona não importa por qual cena o jogo começou.

✅ **Teste:** o projeto compila.

---

## Passo 4 — Criar a cena `Menu`

1. **File → New Scene**, escolher o template **Basic 2D** (ou o 2D que aparecer na lista) e clicar **Create**.
2. **File → Save As**, salvar em **Assets/Scenes** com o nome `Menu` (tem que ser **igual** ao `menuScene` do GameManager).
3. Arrastar o **prefab do GameManager** (de **Assets/Prefabs**) para a Hierarchy, na **raiz**.
4. Selecionar a **Main Camera** e copiar a cor de **Background** da câmera da `Level01`, para o menu ter o mesmo fundo do jogo.

**Por quê o GameManager também no menu?**
No build, o menu é a **primeira** cena. Então o GameManager do menu é o que fica e sobrevive até o fim do jogo. É ele que leva o fade junto para as fases.

**Por quê cena nova, e não duplicar a `Level01`?**
A fase tem bola, barras, Cinemachine e `CameraBounds`, e o menu não precisa de nada disso. Começar vazio é mais rápido do que apagar tudo, e não sobra nada esquecido.

✅ **Teste:** dar Play na `Menu`: a tela fica vazia (só o fundo), e na Hierarchy o `GameManager` aparece em **DontDestroyOnLoad**.

---

## Passo 5 — Montar a tela (título + botão Play)

**1. O Canvas**
- **Hierarchy → botão direito → UI → Canvas**. O Unity cria também um **EventSystem**: ele é quem faz os cliques chegarem nos botões.
- Selecionar o **EventSystem**. Se no Inspector aparecer um aviso com o botão **Replace with InputSystemUIInputModule**, clique nele. O Olo usa o **Input System** novo, e o módulo antigo não recebe os cliques.
- No **Canvas Scaler** do Canvas: **UI Scale Mode → Scale With Screen Size**, com a mesma **Reference Resolution** do Canvas do fade. Assim o menu fica do mesmo tamanho em qualquer tela.

**2. O título**
- Botão direito no Canvas → **UI → Text - TextMeshPro**. Se o Unity pedir para importar o **TMP Essentials**, clique em **Import**.
- Renomear para `Title`, escrever `Olo`, aumentar a fonte e posicionar na parte de cima.

**3. O botão Play**
- Botão direito no Canvas → **UI → Button - TextMeshPro**. Renomear para `PlayButton`, e trocar o texto do filho para `Play`.

**4. Ligar o botão**
- Selecionar o **Canvas** → **Add Component → MenuScreen**.
- Selecionar o `PlayButton`. No componente **Button**, em **On Click ()**, clicar no **+**.
- Arrastar o **Canvas** (que tem o `MenuScreen`) para o campo do objeto e escolher **MenuScreen → OnPlayClicked**.

**5. O fade tem que ficar por cima do menu**
- Abrir o **prefab do GameManager** (duplo clique em Assets/Prefabs) e selecionar o `CanvasFade`.
- No componente **Canvas**, conferir que o **Sort Order** é **maior** que o do Canvas do menu (ex.: `100`, e o do menu fica `0`).
- Ainda no `CanvasFade`, conferir que o **Canvas Group** está com **Blocks Raycasts desmarcado** (hoje já está). Se estivesse marcado, a imagem do fade, mesmo invisível, ficaria na frente do botão e **engoliria o clique**.

**Por quê o Sort Order?**
Os dois Canvas são **Screen Space - Overlay**, e o de **Sort Order maior desenha por cima**. Se o menu ficasse por cima, o botão Play apareceria por cima da tela preta durante o fade.

✅ **Teste:** dar Play na `Menu` e clicar no **Play**: a tela escurece, a `Level01` carrega e a tela clareia.

---

## Passo 6 — `Menu` como primeira cena do build

1. **File → Build Profiles**.
2. Na **Scene List** geral, adicionar a `Menu` (botão **Add Open Scenes** com ela aberta, ou arrastar da pasta) e arrastar ela para o **topo**, no índice **0**:

```
0  Menu
1  Level01
2  Level02
```

3. Selecionar o profile **Web - Mobile - Release**. Se ele tiver uma **Scene List própria** (ela substitui a geral), fazer a mesma coisa nele.

**Por quê?**
- O jogo publicado sempre começa pela cena de **índice 0**.
- O `LoadSceneAsync("Menu")` só encontra cenas que estão na lista. Sem isso, voltar ao menu no fim do jogo dá o mesmo erro do teste do passo 2.

✅ **Teste:** a `Menu` aparece no topo da Scene List, nos dois lugares.

---

## Passo 7 — Testar

- [ ] **Play na `Menu`** → clicar no Play → a `Level01` carrega com fade.
- [ ] **Clicar várias vezes rápido no Play**: só uma troca acontece (o `isLoading` barra as outras).
- [ ] **Jogar até o fim da última fase** → volta para o `Menu` com fade.
- [ ] **No `Menu` de novo, clicar no Play** → a `Level01` carrega de novo. Este teste confere o `MenuScreen`: o GameManager do menu se destruiu, e o botão precisa achar o oficial.
- [ ] **Play direto na `Level02`** → tudo funciona como antes, e ao terminar volta para o `Menu`.
- [ ] Em qualquer momento, a Hierarchy mostra **um GameManager só**, em **DontDestroyOnLoad**.
- [ ] **Parar o Play e dar Play de novo** (várias vezes, partindo do menu e de uma fase): sem erros no Console.
- [ ] **Build Web**: o jogo abre no menu.

---

## Futuro

- **Fade ao abrir o jogo:** o `CanvasFade` começa com alpha `1` (preto) e o GameManager chama `FadeIn()` no `Start`, para o menu aparecer suave em vez de de uma vez.
- **Voltar ao menu no meio da fase** (botão de pausa ou tecla Esc): o GameManager ganha uma ação pública `GoToMenu()`, que só chama `LoadSceneWithFade(menuScene)`. Mesmo padrão do `StartGame()`.
- **Seleção de fases:** o menu mostra um botão por fase (lidos da `LevelList`), e o GameManager ganha `StartLevel(string sceneName)`. O `StartGame()` vira só `StartLevel(levelScenes[0])`.
- **Fases desbloqueadas / progresso salvo:** o "GameState" do [doc 5](5-arquitetura.md#5-gerenciadores-globais-os-departamentos-sempre-abertos-ainda-não-usamos), salvo em `PlayerPrefs` ou no Poki SDK. O menu lê esse estado para saber quais botões ficam liberados.
- **Poki SDK:** o `StartGame()` é o lugar natural para o `gameplayStart`, e o fim da última fase para o `gameplayStop`.
