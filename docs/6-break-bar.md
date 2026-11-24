# 6 — BreakBar: barra que quebra depois de uma rebatida

Uma barra nova (não existia no Phaser): ela **rebate a bola como a DefaultBar**, mas **quebra e some** logo depois. Só serve para uma rebatida.

## Objetivo

- A bola bate na BreakBar e é **rebatida** normalmente (mesmo `Reflect` da DefaultBar).
- Logo depois a barra **quebra**: encolhe até sumir e para de colidir.
- Quando a bola **reseta** (sai da tela), a barra **volta inteira**. Cada tentativa começa com a fase completa, como num puzzle.
- Pode ficar parada ou se mover com o `BarMover`, igual às outras barras.

## Progresso

- [ ] [Passo 1 — Criar o evento `BallReset` (GameEvents)](#passo-1--criar-o-evento-ballreset-gameevents)
- [ ] [Passo 2 — Anunciar o reset na bola](#passo-2--anunciar-o-reset-na-bola)
- [ ] [Passo 3 — Criar o prefab `BreakBar`](#passo-3--criar-o-prefab-breakbar)
- [ ] [Passo 4 — Script `BreakBar.cs`: rebater e quebrar](#passo-4--script-breakbarcs-rebater-e-quebrar)
- [ ] [Passo 5 — Voltar inteira no reset](#passo-5--voltar-inteira-no-reset)
- [ ] [Passo 6 — Testar na cena](#passo-6--testar-na-cena)

## A ideia

Mesma arquitetura de sempre (ver [doc 1](1-default-bar.md) e [doc 5](5-arquitetura.md)):

```
BreakBar.OnCollisionEnter2D ──► ball.Reflect(normal)   (ação que a bola já tem)
                            └─► Break()                (assunto da barra, a bola nem sabe)
```

- **Rebater** é uma ação que a bola já sabe fazer. A bola **não muda nada**.
- **Quebrar** é responsabilidade da barra. A lógica fica toda no `BreakBar.cs`.

### A parte nova: o primeiro evento do jogo

A barra precisa **voltar** quando a bola reseta. Para isso ela precisa ficar sabendo do reset.

Daria para o `CameraBounds` procurar todas as BreakBars e mandar cada uma voltar, mas aí o `CameraBounds` passaria a conhecer barras, o que não é assunto dele. Essa é justamente a situação do [doc 5, seção 4](5-arquitetura.md#4-event-bus-o-intercom-do-jogo-ainda-não-usamos): um acontecimento (o reset) passa a interessar a quem não tem nada a ver com quem o causou.

Então a bola **anuncia** "resetei", e cada BreakBar **escuta** e se conserta sozinha:

```
BallController.ResetPosition() ──► GameEvents.AnnounceBallReset()
                                        │
                                        ├─► BreakBar 1 escuta → volta inteira
                                        ├─► BreakBar 2 escuta → volta inteira
                                        └─► (no futuro: som, placar...)
```

### Por que não usar `Destroy`?

Um objeto destruído **não volta**. Para a barra reaparecer no reset, ela só **finge** que sumiu: desliga o collider e encolhe o visual até zero. O GameObject continua existindo, e continua ouvindo o evento.

---

## Passo 1 — Criar o evento `BallReset` (GameEvents)

Criar `Assets/Scripts/GameEvents.cs`:

```csharp
// O "intercom" do jogo: quem quer avisar algo anuncia aqui,
// quem quer reagir se inscreve. Um não precisa conhecer o outro.
public static class GameEvents
{
    // a bola voltou para a posição inicial
    public static event System.Action BallReset;

    public static void AnnounceBallReset()
    {
        // se ninguém estiver inscrito, o evento é null: aí não tem a quem avisar
        if (BallReset != null)
        {
            BallReset();
        }
    }
}
```

**Por quê?**
- `static` significa que a classe **não é um componente**: ela não fica em nenhum GameObject, e qualquer script acessa direto com `GameEvents.`.
- `event System.Action` é uma lista de métodos sem parâmetros. Quem se inscreve (`+=`) entra na lista, e quando alguém anuncia, todos da lista são chamados.

✅ **Teste:** o projeto compila sem erros (Console limpo). Por enquanto nada muda no jogo.

---

## Passo 2 — Anunciar o reset na bola

No `BallController.cs`, no **final** do `ResetPosition()`:

```csharp
    public void ResetPosition()
    {
        ...
        transform.position = initialPosition;
        direction = initialDirection;

        // avisa quem estiver interessado (as BreakBars voltam inteiras, por exemplo)
        GameEvents.AnnounceBallReset();
    }
```

**Por quê aqui e não no `CameraBounds`?**
"A bola resetou" é um fato **da bola**. Se no futuro aparecer outro jeito de resetar (um botão de reiniciar, por exemplo), ele também vai chamar `ResetPosition()`, e o anúncio sai de graça.

E o reset acontece **com a tela preta** (entre o `FadeOut` e o `FadeIn` do `CameraBounds`), então o jogador não vê as barras reaparecendo.

✅ **Teste:** o jogo funciona exatamente como antes. A bola anuncia, mas ninguém está ouvindo ainda.

---

## Passo 3 — Criar o prefab `BreakBar`

A BreakBar tem a mesma estrutura da DefaultBar (raiz com `Rigidbody2D` + `BoxCollider2D` + `BarMover`, e o filho `Visual` com o sprite), então dá para partir dela:

1. Na pasta **Assets/Prefabs**, selecionar `DefaultBar.prefab` e apertar **Cmd + D** para duplicar.
2. Renomear a cópia para `BreakBar`.
3. Abrir o prefab (duplo clique) e, na raiz, **remover o componente `DefaultBar`** (o script). O `BarMover` fica.
4. No filho `Visual`, mudar a **cor** do SpriteRenderer para algo que pareça frágil (ex.: um vermelho ou um tom rachado), para o jogador distinguir das barras normais.

**Por quê duplicar e não fazer um Prefab Variant?**
Um Variant continua ligado à DefaultBar: se um dia você mudar a DefaultBar, a BreakBar muda junto. Como são barras com comportamentos diferentes, é mais seguro elas serem independentes.

✅ **Teste:** arrastar a BreakBar para a cena. A bola bate nela e **para de ricochetear direito**: sem script, quem age é a física pura da Unity. Isso é esperado até o próximo passo.

---

## Passo 4 — Script `BreakBar.cs`: rebater e quebrar

Criar `Assets/Scripts/BreakBar.cs`:

```csharp
using DG.Tweening;
using UnityEngine;

public class BreakBar : MonoBehaviour
{
    public Transform visual;              // filho com o SpriteRenderer: é ele que encolhe
    public float breakDuration = 0.15f;   // tempo para a barra encolher até sumir, em segundos

    private Collider2D barCollider;
    private Vector3 visualScale;          // tamanho original do visual, para voltar no reset

    void Awake()
    {
        barCollider = GetComponent<Collider2D>();
        visualScale = visual.localScale;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // procura o script da bola no objeto que bateu na barra
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // O sinal de menos inverte: ela passa a apontar "pra fora" da barra, como o Reflect espera.
        Vector2 normal = -collision.GetContact(0).normal;

        // só quebra se a bola realmente rebateu
        // (se ela estiver grudada ou no warp, o Reflect devolve false e a barra fica inteira)
        bool reflected = ball.Reflect(normal);
        if (reflected)
        {
            Break();
        }
    }

    private void Break()
    {
        // para de colidir na hora: a bola já foi rebatida e não pode bater de novo
        barCollider.enabled = false;

        // encolhe o visual até sumir
        visual.DOKill();
        visual.DOScale(Vector3.zero, breakDuration)
            .SetEase(Ease.InBack)    // dá uma "puxadinha" antes de sumir
            .SetLink(gameObject);    // se a barra for destruída, o tween morre junto
    }
}
```

No prefab, arrastar o filho `Visual` para o campo **Visual** do componente.

**Por quê?**
- **O `Reflect` é o mesmo da DefaultBar.** A bola não sabe que essa barra é diferente.
- **O collider desliga antes da animação.** Se ficasse ligado enquanto a barra encolhe, a bola poderia bater nela de novo.
- **O visual encolhe, não o objeto inteiro.** A raiz continua com o tamanho certo, e no reset é só devolver a escala do visual.
- **Por que não reaproveitar o `PlayPunch` da DefaultBar?** A barra vai sumir logo em seguida, e o encolhimento já é o feedback da batida. Se quiser os dois juntos (afunda e depois some), dá para montar com uma `DOTween.Sequence()`.

✅ **Teste:** a bola rebate na BreakBar com o ângulo certo de 45°, a barra encolhe e some, e na volta a bola **passa direto** por onde ela estava.

---

## Passo 5 — Voltar inteira no reset

No `BreakBar.cs`, inscrever no evento e criar o método que conserta a barra:

```csharp
    void OnEnable()
    {
        GameEvents.BallReset += Restore;
    }

    void OnDisable()
    {
        // sempre desinscrever! senão o evento tenta chamar uma barra que já foi destruída
        GameEvents.BallReset -= Restore;
    }

    private void Restore()
    {
        // se ainda estava encolhendo, para a animação
        visual.DOKill();

        visual.localScale = visualScale;
        barCollider.enabled = true;
    }
```

**Por quê?**
- **`OnEnable` / `OnDisable`** são o par certo para inscrever e desinscrever: a barra só ouve enquanto está ativa na cena. Isso só funciona porque a barra **nunca é desativada** ao quebrar (ela só esconde o visual e desliga o collider). Se fosse `gameObject.SetActive(false)`, o `OnDisable` rodaria e ela pararia de ouvir justo o evento que a faz voltar.
- **O `Restore` não anima.** O reset acontece com a tela preta, então a barra já aparece inteira quando a tela clareia.
- O `Restore` roda em **todas** as BreakBars, inclusive nas que não quebraram. Para essas, ele só devolve a escala que elas já têm, então não faz mal.

✅ **Teste:** quebrar uma BreakBar, deixar a bola sair da tela e, depois do fade, a barra está de volta e rebate de novo.

---

## Passo 6 — Testar na cena

Montar uma situação com **duas BreakBars** e checar:

- [ ] Batida na **lateral** e na **ponta**: rebate com 45° e quebra nos dois casos.
- [ ] A bola **não bate de novo** numa barra que já quebrou.
- [ ] **Duas** BreakBars quebradas voltam juntas no reset.
- [ ] Uma BreakBar **com `BarMover`** (com `moveOffset` diferente de zero): continua se movendo, quebra e volta no lugar certo do vai-e-vem.
- [ ] Parar o Play com uma barra quebrada e dar Play de novo: tudo volta ao normal, sem erro no Console.

---

## Futuro

- **Efeito de quebra:** partículas ou um sprite de "rachado" antes de sumir. Fica todo no `Break()`, a bola não muda.
- **Barra com N batidas** (quebra na terceira, por exemplo): um contador `hitsLeft` na BreakBar. Ela só chama `Break()` quando o contador chega a zero, e o `Restore()` devolve o valor inicial.
- **Mais ouvintes do `BallReset`:** som de reset, placar de tentativas... O evento já existe, então é só se inscrever.
