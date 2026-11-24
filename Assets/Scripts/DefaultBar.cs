using DG.Tweening;
using UnityEngine;

public class DefaultBar : MonoBehaviour
{
    public Transform visual;              // filho com o SpriteRenderer: só ele balança no rebote
    public float punchDistance = 0.1f;    // quanto a barra afunda (em unidades do mundo)
    public float punchDuration = 0.15f;   // tempo total do rebote, em segundos

    void OnCollisionEnter2D(Collision2D collision)
    {
        // procura o script da bola no objeto que bateu na barra
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // O sinal de menos inverte: ela passa a apontar "pra fora" da barra, como o Reflect espera.
        Vector2 normal = -collision.GetContact(0).normal;

        bool reflected = ball.Reflect(normal);
        if (reflected)
        {
            // a normal aponta para fora da barra, então o menos empurra a barra para longe da bola
            PlayPunch(-normal);
        }
    }

    private void PlayPunch(Vector2 worldDirection)
    {
        if (visual == null) return;

        // o DOPunchPosition mexe no localPosition do visual, então o deslocamento
        // precisa estar no espaço local da barra (isso também compensa a escala dela)
        Vector3 worldOffset = worldDirection.normalized * punchDistance;
        Vector3 localOffset = transform.InverseTransformVector(worldOffset);

        // se ainda tinha um rebote rodando, termina ele (o visual volta para o lugar) antes de começar outro
        visual.DOKill(true);

        // vibrato 1 e elasticity 0: vai uma vez e volta, sem ficar tremendo
        visual.DOPunchPosition(localOffset, punchDuration, 1, 0f)
            .SetLink(gameObject); // se a barra for destruída, o tween morre junto
    }
}
