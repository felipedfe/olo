# 1 — DefaultBar: barra que reflete a bola

Porte da `DefaultBar` do projeto Phaser (`ricochet-game/js/components/defaultBar.js`) para a Unity.

## Objetivo

Uma barra que:

- **reflete a bola** quando ela bate (na lateral ou nas pontas);
- pode ficar **parada** ou se **mover em vai-e-vem** em **qualquer direção**;
- serve tanto **em pé quanto deitada** (é um prefab só: a orientação vem da rotação).

## Progresso

- [ ] [Passo 0 — Instalar o DOTween](#passo-0--instalar-o-dotween)
- [ ] [Passo 1 — Ajustar o Rigidbody2D da bola](#passo-1--ajustar-o-rigidbody2d-da-bola)
- [ ] [Passo 2 — Criar a barra parada (só física, sem script)](#passo-2--criar-a-barra-parada-só-física-sem-script)
- [ ] [Passo 3 — Ricochete (Reflect na bola, colisão na barra)](#passo-3--ricochete-reflect-na-bola-colisão-na-barra)
- [ ] [Passo 4 — Barra deitada (só rotação)](#passo-4--barra-deitada-só-rotação)
- [ ] [Passo 5 — Movimento com DOTween (qualquer direção)](#passo-5--movimento-com-dotween-qualquer-direção)
- [ ] [Passo 6 — Transformar em Prefab](#passo-6--transformar-em-prefab)
- [ ] [Passo 7 (opcional) — Barras de tamanhos diferentes sem esticar a arte](#passo-7-opcional--barras-de-tamanhos-diferentes-sem-esticar-a-arte)

## Arquitetura: quem é responsável pelo quê

A bola vai interagir com vários objetos (barras, GrabBar, buraco, warp…). Para ela não virar um `if/else` gigante, a responsabilidade é dividida:

- **A bola (`BallController`) sabe *como* se mover.** Ela oferece métodos públicos, as "ações" dela: `Reflect(normal)` agora, e no futuro coisas como `AttachTo(...)` e `Finish()`.
- **Cada objeto sabe *o que* acontece quando a bola bate nele.** O próprio objeto detecta a colisão e chama a ação certa da bola.

```
DefaultBar.OnCollisionEnter2D  ──►  ball.Reflect(normal)
GrabBar.OnCollisionEnter2D     ──►  ball.AttachTo(this)     (futuro)
Hole.OnTriggerEnter2D          ──►  ball.Finish()           (futuro)
```

É o mesmo padrão do Pong (`csj`): o `Score` detecta a bola e chama `ball.ResetBall()`, e a bola não sabe nada sobre o `Score`.

**Resultado:** para criar um objeto novo, você cria um script novo. A bola só muda quando ela precisa aprender uma ação nova.

## Phaser → Unity

| Phaser (`defaultBar.js`) | Unity |
|---|---|
| `physics.add.image(...)` + `setImmovable()` | `BoxCollider2D` + `Rigidbody2D` **Kinematic** |
| `setSize(70%, 95%)` (hitbox) | Tamanho do `BoxCollider2D` |
| `tweens.add({ y: finalY, yoyo, repeat: -1 })` | DOTween: `rig.DOMove(...).SetLoops(-1, LoopType.Yoyo)` |
| `orientation: 'hor'` | Rotation Z = 90 no Transform |
| `physics.add.collider(ball, bar, changeDirection)` | `OnCollisionEnter2D` no script da barra, que chama `ball.Reflect(normal)` |
| `changeDirection`: compara posições para saber se bateu na lateral ou na ponta | `Vector2.Reflect(direction, normal)`: a normal da colisão já diz qual face foi atingida |
| `setTimeout(100ms)` para não colidir duas vezes | `Vector2.Dot(direction, normal) < 0`: só reflete se a bola estiver indo **contra** a barra |
| `moveBall()` reaplica a velocidade | `rig.linearVelocity = direction * speed` |

---

## Passo 0 — Instalar o DOTween

1. Instalar o **DOTween (HOTween v2)** gratuito pela Asset Store / Package Manager (My Assets).
2. Quando abrir o painel, clicar em **Setup DOTween…** (ou depois em **Tools → Demigiant → DOTween Utility Panel → Setup DOTween…**).
3. Confirmar que o módulo **Physics2D** está marcado e clicar em **Apply**.

> **Por quê?** Os atalhos que movem um `Rigidbody2D` (`rig.DOMove`) ficam no módulo Physics2D. Sem ele, o código não compila.

✅ **Teste:** o projeto compila sem erros no Console.

---

## Passo 1 — Ajustar o Rigidbody2D da bola

No `Ball`, em **Rigidbody2D**:

- **Constraints → Freeze Rotation Z**: ligado
- **Collision Detection**: `Continuous`

> **Por quê?**
> - Sem travar a rotação, a bola ganha giro quando raspa na barra, e o atrito muda a trajetória.
> - `Continuous` impede que a bola, se ficar rápida, "pule" através da barra entre um frame de física e outro.

---

## Passo 2 — Criar a barra parada (só física, sem script)

1. **Hierarchy → Create → 2D Object → Sprites → Square** e renomear para `DefaultBar`.
2. **Transform → Scale**: `0.26 × 1.75` (mesma proporção do Phaser: barra 30×200 px para uma bola de 40 px).
3. Posicionar a barra no caminho da bola (a bola sai em diagonal para cima e para a direita).
4. **Add Component → BoxCollider2D**.
5. **Add Component → Rigidbody2D** com **Body Type = Kinematic**.

> **Por quê Kinematic?** É o equivalente do `setImmovable()`. A bola não consegue empurrar a barra, mas a barra ainda pode se mover por código (passo 5).

✅ **Teste:** dar Play e apertar **Q**. A bola **bate e não reflete direito**: ela para, escorrega ou desvia de um jeito estranho. É esperado, porque a física da Unity está decidindo sozinha o que acontece. No próximo passo a gente assume o controle.

---

## Passo 3 — Ricochete (Reflect na bola, colisão na barra)

Aqui entra a [arquitetura](#arquitetura-quem-é-responsável-pelo-quê): a bola ganha o método `Reflect`, e quem decide chamá-lo é a barra.

### 3a. Adicionar o método `Reflect` no `BallController.cs`

```csharp
// Chamado pelos objetos que fazem a bola ricochetear (ex.: DefaultBar)
public void Reflect(Vector2 normal)
{
    if (currentState != BallState.Moving) return;

    // arredonda a normal para uma das 4 direções retas (evita ângulo torto ao bater na quina)
    Vector2 axisNormal = SnapToAxis(normal);

    // só reflete se a bola estiver indo contra a superfície
    if (Vector2.Dot(direction, axisNormal) < 0)
    {
        direction = Vector2.Reflect(direction, axisNormal);
    }

    // reaplica sempre, para a física não deixar a bola mais lenta ou escorregando
    rig.linearVelocity = direction * speed;
}

// Transforma qualquer normal em (±1, 0) ou (0, ±1)
private Vector2 SnapToAxis(Vector2 normal)
{
    if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
    {
        return new Vector2(Mathf.Sign(normal.x), 0); // bateu na lateral
    }
    else
    {
        return new Vector2(0, Mathf.Sign(normal.y)); // bateu na ponta
    }
}
```

> **Como funciona:**
> - **`public`**: outros scripts (a barra) podem chamar esse método. É o mesmo que o `ResetBall()` do Pong, que o `Score` chamava.
> - **Normal**: uma seta perpendicular à superfície atingida, apontando "pra fora" da barra, em direção à bola. Se a bola bate na lateral direita da barra, a normal é `(1, 0)`. Se bate na ponta de cima, é `(0, 1)`.
> - **`Vector2.Reflect`**: espelha a direção em relação a essa normal, como luz num espelho. Batendo na lateral, inverte o X. Batendo na ponta, inverte o Y. É o que o `changeDirection` do Phaser fazia, mas sem comparar posições nem usar números mágicos.
> - **`SnapToAxis` (ângulo sempre 45°)**: quando a bola bate na **quina** da barra, a física devolve uma normal diagonal (da quina até o centro da bola), com um ângulo qualquer. Refletir nela deixa a saída torta. Como as barras só ficam em pé ou deitadas, a normal é arredondada para o eixo mais próximo: `(±1, 0)` (lateral) ou `(0, ±1)` (ponta). Refletir uma diagonal num eixo reto só inverte o X **ou** o Y, então a bola sempre sai a 45°. Isso só vale enquanto as barras estiverem a 0° ou 90°; barras inclinadas precisariam de outra regra.
> - **`Vector2.Dot < 0`**: o produto escalar é negativo quando a bola está indo **contra** a normal, ou seja, entrando na barra. Isso evita refletir duas vezes seguidas (o papel do `setTimeout` no Phaser).
> - **Por que refletir `direction` e não `rig.linearVelocity`?** Quando a colisão é avisada, a física já alterou a velocidade da bola. A `direction` guardada é a direção "limpa", de antes do impacto.
> - **`rig.linearVelocity = direction * speed`** (fora do `if`): mantém a velocidade sempre constante, como o `moveBall()`. Se ficasse dentro do `if`, quando ele não entrasse a bola ficaria com a velocidade que a física deixou depois do impacto (mais lenta ou escorregando pela barra).

### 3b. Criar `Assets/Scripts/DefaultBar.cs`

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultBar : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        // procura o script da bola no objeto que bateu na barra
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return; // não foi a bola, então ignora

        // Aqui (no script da barra) a normal do contato aponta da bola para a barra.
        // O sinal de menos inverte: ela passa a apontar "pra fora" da barra, como o Reflect espera.
        Vector2 normal = -collision.GetContact(0).normal;

        // seta vermelha na Scene view para conferir a normal (pode apagar depois do teste)
        Debug.DrawRay(collision.GetContact(0).point, normal, Color.red, 1f);

        ball.Reflect(normal);
    }
}
```

Adicionar esse componente no GameObject `DefaultBar`.

> **Como funciona:**
> - **`OnCollisionEnter2D` na barra**: a Unity avisa a colisão para os **dois** objetos envolvidos. Então a barra pode reagir sozinha, sem a bola precisar saber que barras existem.
> - **`GetComponent<BallController>()`**: confere se quem bateu foi a bola. Se não foi (no futuro pode ser outra coisa), retorna `null` e sai.
> - **`[RequireComponent(typeof(Rigidbody2D))]`**: se alguém adicionar o script num objeto sem Rigidbody2D, a Unity adiciona um automaticamente. É uma proteção contra esquecimento.

✅ **Teste:**
- deixar a aba **Scene** visível durante o Play: a seta vermelha deve apontar **pra fora** da barra, em direção à bola. (Se apontar para dentro, é só tirar o sinal de menos da normal.)
- bater na **lateral** da barra: a bola volta no X e continua subindo;
- bater na **ponta** (de cima ou de baixo): a bola inverte o Y;
- a velocidade não muda depois da batida.

---

## Passo 4 — Barra deitada (só rotação)

Duplicar a barra (**Ctrl/Cmd + D**) e colocar **Rotation Z = 90**.

> **Por quê funciona sem mudar código?** O `Reflect` só olha a normal da face atingida. Ele não sabe, nem precisa saber, se a barra está em pé ou deitada.

✅ **Teste:** a bola reflete corretamente na barra deitada também.

---

## Passo 5 — Movimento com DOTween (qualquer direção)

Completar o `DefaultBar.cs` (o `OnCollisionEnter2D` do passo 3 continua igual):

```csharp
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultBar : MonoBehaviour
{
    [SerializeField] private Vector2 moveOffset = Vector2.zero; // (0,0) = parada
    [SerializeField] private float duration = 2f;               // segundos para ir (e o mesmo para voltar)

    private Tween moveTween;

    void Start()
    {
        if (moveOffset == Vector2.zero) return;

        Rigidbody2D rig = GetComponent<Rigidbody2D>();

        moveTween = rig.DOMove(rig.position + moveOffset, duration)
            .SetEase(Ease.Linear)          // ease: "Linear"
            .SetLoops(-1, LoopType.Yoyo)   // yoyo: true, repeat: -1
            .SetUpdate(UpdateType.Fixed);  // anda no mesmo ritmo da física
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // ...igual ao passo 3
    }

    void OnDestroy()
    {
        // se a barra tinha um tween, para ele
        if (moveTween != null)
        {
            moveTween.Kill();
        }
    }
}
```

> **Detalhes importantes:**
> - **`rig.DOMove`, e não `transform.DOMove`**: mexer no Transform de um objeto com física "teleporta" a barra, e a colisão fica imprecisa. No Rigidbody2D, o DOTween usa `MovePosition`, que a física entende.
> - **`UpdateType.Fixed`**: o tween roda no `FixedUpdate`, sincronizado com a física.
> - **`moveOffset` é relativo** à posição inicial da barra: `(0, -2)` desce 2 unidades, `(3, 0)` vai 3 para a direita, `(2, 2)` anda na diagonal.
> - **`Kill()` no `OnDestroy`**: evita o tween continuar rodando sobre um objeto destruído (por exemplo, ao trocar de cena).
> - No Phaser, o `speed: 2000` era em **ms**. Aqui, `duration: 2` é em **segundos**.

✅ **Teste** (no Inspector de cada barra):
- `moveOffset = (0, 0)`: parada;
- `moveOffset = (0, -2)`: sobe e desce;
- `moveOffset = (3, 0)`: vai para os lados;
- a bola continua refletindo certinho com a barra em movimento.

---

## Passo 6 — Transformar em Prefab

1. Arrastar o `DefaultBar` da Hierarchy para a pasta `Assets/Prefabs`.
2. Montar algumas barras na cena a partir do prefab, variando posição, rotação, `moveOffset` e `duration`. Por exemplo, reproduzir um trecho do `stage1.json` do Phaser.

> **Por quê?** Uma mudança no prefab (sprite, collider, script) vale para todas as barras da cena de uma vez.

✅ **Teste:** editar o prefab (ex.: a cor) e ver todas as barras mudarem.

---

## Passo 7 (opcional) — Barras de tamanhos diferentes sem esticar a arte

Equivalente ao `DefaultBar2` do Phaser (`tileWidth` / `tileHeight`):

1. No **SpriteRenderer**, colocar **Draw Mode = Sliced** (ou **Tiled**).
2. Voltar a **Scale** para `1 × 1` e controlar o tamanho pelo campo **Size**.
3. No **BoxCollider2D**, ligar **Auto Tiling** para o colisor acompanhar o tamanho.

> Só faz diferença quando tiver uma arte de barra de verdade (com bordas). Com o quadrado branco, esticar pela escala funciona igual.

---

## Fora do escopo deste plano

- Paredes / limites da tela (hoje a bola sai da tela).
- `GrabBar` (estado `Attached`): mesmo padrão, a `GrabBar` detecta a bola e chama uma nova ação dela, como `ball.AttachTo(this)`.
- Reset da bola para a posição inicial.
