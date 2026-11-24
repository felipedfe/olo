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
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        // O sinal de menos inverte: ela passa a apontar "pra fora" da barra, como o Reflect espera.
        Vector2 normal = -collision.GetContact(0).normal;

        // só quebra se a bola realmente rebateu
        bool reflected = ball.Reflect(normal);
        if (reflected)
        {
            Break();
        }
    }

    private void Break()
    {
        // desativa a barra
        barCollider.enabled = false;

        // encolhe o visual até sumir
        visual.DOKill();
        visual.DOScale(Vector3.zero, breakDuration)
            .SetEase(Ease.InBack)
            .SetLink(gameObject);    // se a barra for destruída o tween morre junto
    }

    // esse método e o OnDisable são nativos da unity
    // basicamente isso tá dizendo que quando componente nasce ele adiciona esse evento e quando morre ele tira
    void OnEnable()
    {
        GameEvents.BallReset += Restore;
    }

    void OnDisable()
    {
        GameEvents.BallReset -= Restore;
    }

    private void Restore()
    {
        // se ainda estava encolhendo, para a animação
        visual.DOKill();

        visual.localScale = visualScale;
        barCollider.enabled = true;
    }

}
