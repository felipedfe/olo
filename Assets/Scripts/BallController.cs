using System.Collections; // necessário para usar coroutines (IEnumerator)
using UnityEngine;
using UnityEngine.InputSystem;

public enum BallState
{
    Ready,
    Moving,
    Attached,
    Warping,
    Finished
}

public class BallController : MonoBehaviour
{
    [SerializeField] private BallState currentState = BallState.Ready;
    private Rigidbody2D rig;
    private Vector2 direction;
    private Vector2 initialDirection = new Vector2(1, 1).normalized;
    private Vector3 initialPosition;
    private Transform attachedBar;   // a barra em que a bola está grudada
    private Vector2 attachOffset;    // distância da bola até o centro da barra
    private SpriteRenderer spriteRenderer;  // para esconder e mostrar a bola

    public float speed = 5f;
    public float warpDelay = 0.3f;   // quanto tempo a bola fica "dentro" do warp

    void Awake() // esse método roda antes do Start()
    {
        direction = initialDirection;
        rig = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (currentState != BallState.Attached) return;

        // acompanha a barra, mantendo a mesma distância de quando grudou
        rig.MovePosition((Vector2)attachedBar.position + attachOffset);
    }

    void OnMove(InputValue value)
    {
        if (!value.isPressed) return;

        // o mesmo botão lança a bola (no início) e solta ela (quando está grudada)
        if (currentState == BallState.Ready || currentState == BallState.Attached)
        {
            Launch();
        }
    }

    private void Launch()
    {
        currentState = BallState.Moving;
        attachedBar = null;

        rig.bodyType = RigidbodyType2D.Dynamic; // liga a física de novo
        rig.linearVelocity = direction * speed;
    }

    public void ResetPosition()
    {
        if (currentState == BallState.Finished) return;

        // se a bola estava dentro de um warp, cancela a espera e mostra a bola de novo
        StopAllCoroutines();
        spriteRenderer.enabled = true;

        currentState = BallState.Ready;
        attachedBar = null;

        // se a bola resetar grudada, ela precisa voltar a ter física
        rig.bodyType = RigidbodyType2D.Dynamic;
        rig.linearVelocity = Vector2.zero;
        rig.angularVelocity = 0f;

        rig.position = initialPosition;
        transform.position = initialPosition;
        direction = initialDirection;

        // evento que reseta a bola
        GameEvents.AnnounceBallReset();
    }

    // Gruda a bola em "bar" (ela passa a acompanhar esse Transform).
    // Devolve true se a bola grudou.
    public bool AttachTo(Transform bar, Vector2 normal)
    {
        if (currentState != BallState.Moving) return false;

        Vector2 axisNormal = SnapToAxis(normal);

        // só gruda se estiver indo contra a barra (evita grudar de novo logo ao soltar)
        if (Vector2.Dot(direction, axisNormal) >= 0) return false;

        currentState = BallState.Attached;
        attachedBar = bar;
        attachOffset = rig.position - (Vector2)bar.position;

        // já prepara a direção de saída: espelhada, para sair para o outro lado
        direction = Vector2.Reflect(direction, axisNormal);

        // para a bola e deixa ela ser movida só pelo FixedUpdate enquanto estiver grudada
        rig.linearVelocity = Vector2.zero;
        rig.bodyType = RigidbodyType2D.Kinematic;
        return true;
    }

    // Rebate a bola na superfície. Devolve true se a bola estava andando e rebateu.
    public bool Reflect(Vector2 normal)
    {
        if (currentState != BallState.Moving) return false;

        // Esse método faz com q a bola sempre rebate num angulo de 45
        Vector2 axisNormal = SnapToAxis(normal);

        // só reflete se a bola estiver indo contra a superfície
        if (Vector2.Dot(direction, axisNormal) < 0)
        {
            direction = Vector2.Reflect(direction, axisNormal);
        }

        rig.linearVelocity = direction * speed;
        return true;
    }

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
        rig.position = position;

        // espera o delay (o jogo continua rodando normalmente enquanto isso)
        yield return new WaitForSeconds(warpDelay);

        // aparece no warp de saída
        rig.bodyType = RigidbodyType2D.Dynamic;
        spriteRenderer.enabled = true;

        // volta a andar com a mesma direção: entrou ↗, sai ↗
        currentState = BallState.Moving;
        rig.linearVelocity = direction * speed;
    }

    // Transforma qualquer normal em (±1, 0) ou (0, ±1)
    // Esse método faz com q a bola sempre rebate num angulo de 45
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

    public bool Finish()
    {
        // só rola o finish se a bola estiver andando
        if (currentState != BallState.Moving) return false;

        currentState = BallState.Finished;

        // para a bola e desliga a física
        rig.linearVelocity = Vector2.zero;
        rig.bodyType = RigidbodyType2D.Kinematic;

        GameEvents.AnnounceBallFinished();
        return true;
    }

}
