using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Warp : MonoBehaviour
{
    // o outro warp do par: é para lá que a bola vai
    public Warp destination;

    // fica true enquanto a bola que acabou de chegar ainda está em cima deste warp
    private bool ignoreBall = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // procura o script da bola no objeto que entrou no warp
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // a bola acabou de chegar por este warp: não manda ela de volta
        if (ignoreBall) return;

        // warp sem destino não teleporta (serve como warp "só de saída")
        if (destination == null) return;

        bool warped = ball.WarpTo(destination.transform.position);
        if (warped)
        {
            destination.ReceiveBall();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // a bola saiu de cima deste warp: da próxima vez que entrar, pode teleportar
        ignoreBall = false;
    }

    // Chamado pelo outro warp, logo antes de a bola aparecer aqui
    public void ReceiveBall()
    {
        ignoreBall = true;
    }
}
