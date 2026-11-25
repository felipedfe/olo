using UnityEngine;

// Controla o tamanho da barra: o collider (na raiz) e o sprite (no Visual) acompanham este valor.
// Funciona no editor: mudou o Size no Inspector, os dois mudam na hora.
// A Scale da barra fica sempre 1 x 1: o tamanho vem daqui, e o sprite Sliced não estica as bordas.
[RequireComponent(typeof(BoxCollider2D))]
public class BarSize : MonoBehaviour
{
    public Vector2 size = new Vector2(0.32f, 2.22f);   // tamanho da imagem da barra
    public Vector2 colliderPadding = Vector2.zero;     // quanto o collider passa (+) ou falta (-) em relação à imagem
    public SpriteRenderer visual;                      // o SpriteRenderer do filho Visual (Draw Mode = Sliced)

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
    }
}
