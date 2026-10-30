using DG.Tweening;
using UnityEngine;

public class DefaultBar : MonoBehaviour
{
    public Vector2 moveOffset = Vector2.zero;
    public float duration = 2f;
    private Rigidbody2D rig;
    private Tween moveTween;

    void Start()
    {
        if (moveOffset == Vector2.zero) return;

        rig = GetComponent<Rigidbody2D>();
        moveTween = rig.DOMove(rig.position + moveOffset, duration)
            .SetEase(Ease.Linear) 
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(UpdateType.Fixed);  // anda no mesmo ritmo da física
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // procura o script da bola no objeto que bateu na barra
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // O sinal de menos inverte: ela passa a apontar "pra fora" da barra, como o Reflect espera.
        Vector2 normal = -collision.GetContact(0).normal;

        ball.Reflect(normal);
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
