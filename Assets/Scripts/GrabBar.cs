using UnityEngine;

public class GrabBar : MonoBehaviour
{
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
            ball.AttachTo(transform, normal); // lateral: gruda
        }
        else
        {
            ball.Reflect(normal); // ponta: reflete como a DefaultBar
        }
    }
}
