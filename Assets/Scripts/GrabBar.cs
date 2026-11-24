using DG.Tweening;
using UnityEngine;

public class GrabBar : MonoBehaviour
{
    public Transform visual;              // filho com o SpriteRenderer: só ele balança no rebote
    public float punchDistance = 0.1f;    // quanto a barra afunda (em unidades do mundo)
    public float punchDuration = 0.15f;   // tempo total do rebote, em segundos

    void OnCollisionEnter2D(Collision2D collision)
    {
        // procura o script da bola no objeto que bateu na barra
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // O sinal de menos inverte: ela passa a apontar "pra fora" da barra
        Vector2 normal = -collision.GetContact(0).normal;

        // converte a normal para o "ponto de vista" da barra,
        // assim funciona com a barra em pé ou deitada
        Vector2 localNormal = transform.InverseTransformDirection(normal);
        bool hitLongSide = Mathf.Abs(localNormal.x) > Mathf.Abs(localNormal.y);

        if (hitLongSide)
        {
            // lateral: gruda. A bola gruda no visual (e não na raiz da barra),
            // assim ela acompanha o rebote e não fica "flutuando" enquanto o sprite afunda.
            // O AttachTo vem antes do punch: ele guarda a distância com o visual ainda no lugar.
            Transform attachTarget = visual;
            if (attachTarget == null)
            {
                attachTarget = transform; // sem visual configurado: gruda na barra mesmo
            }

            bool attached = ball.AttachTo(attachTarget, normal);
            if (attached)
            {
                PlayPunch(-normal);
            }
        }
        else
        {
            // ponta: reflete como a DefaultBar
            bool reflected = ball.Reflect(normal);
            if (reflected)
            {
                PlayPunch(-normal);
            }
        }
    }

    private void PlayPunch(Vector2 worldDirection)
    {
        if (visual == null) return;

        // o DOPunchPosition mexe no localPosition do visual, então o deslocamento
        // precisa estar no espaço local da barra (isso também compensa a escala 0.25 x 2)
        Vector3 worldOffset = worldDirection.normalized * punchDistance;
        Vector3 localOffset = transform.InverseTransformVector(worldOffset);

        // se ainda tinha um rebote rodando, termina ele (o visual volta para o lugar) antes de começar outro
        visual.DOKill(true);

        // vibrato 1 e elasticity 0: vai uma vez e volta, sem ficar tremendo
        visual.DOPunchPosition(localOffset, punchDuration, 1, 0f)
            .SetUpdate(UpdateType.Fixed) // mesmo ritmo do FixedUpdate que move a bola grudada
            .SetLink(gameObject);        // se a barra for destruída, o tween morre junto
    }
}
