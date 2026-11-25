using UnityEngine;

// Controla o tamanho da barra: o collider (na raiz), o sprite (no Visual) e o fundo (se tiver) acompanham este valor.
// Funciona no editor: mudou o Size no Inspector, os dois mudam na hora.
// A Scale da barra fica sempre 1 x 1: o tamanho vem daqui, e o sprite Sliced não estica as bordas.
[RequireComponent(typeof(BoxCollider2D))]
public class BarSize : MonoBehaviour
{
    public Vector2 size = new Vector2(0.46f, 2.41f);   // tamanho da imagem da barra
    public Vector2 colliderPadding = Vector2.zero;     // quanto o collider passa (+) ou falta (-) em relação à imagem
    public SpriteRenderer visual;                      // o SpriteRenderer do filho Visual (Draw Mode = Sliced)

    [Header("Fundo (opcional)")]
    public SpriteRenderer background;                  // uma imagem de fundo (Draw Mode = Sliced); pode ficar vazio
    public Vector2 backgroundPadding = Vector2.zero;   // quanto o fundo passa (+) ou falta (-) em relação à imagem

    // a Unity chama este método no editor sempre que um valor deste componente muda no Inspector
    void OnValidate()
    {
        BoxCollider2D barCollider = GetComponent<BoxCollider2D>();
        barCollider.size = size + colliderPadding;

        // só muda o sprite se o tamanho for diferente: mudar o tamanho do sprite dentro do
        // OnValidate gera um aviso no Console (inofensivo), e assim ele só aparece quando
        // o Size realmente muda (e não toda vez que a cena abre ou os scripts recompilam)
        if (visual != null && visual.size != size)
        {
            visual.size = size;
        }

        // o fundo segue a mesma ideia do collider: tamanho da barra + uma margem fixa
        Vector2 backgroundSize = size + backgroundPadding;
        if (background != null && background.size != backgroundSize)
        {
            background.size = backgroundSize;
        }
    }
}
