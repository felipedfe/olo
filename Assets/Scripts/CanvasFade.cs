using System.Collections;
using UnityEngine;

public class CanvasFade : MonoBehaviour
{
    CanvasGroup imageFader;
    // private float currentAlpha;

    [SerializeField] private float fadeDuration = 0.3f; // em segundos

    void Awake()
    {
        imageFader = GetComponent<CanvasGroup>();
        // Debug.Log(imageFader.alpha);
        // imageFader.alpha = 0;
        // currentAlpha = imageFader.alpha;
        // FadeIn(5f);
        // StartCoroutine(FadeIn(0.3f));1
    }

    // private void FadeIn(float speed)
    // {
    //     imageFader.alpha = Mathf.MoveTowards(imageFader.alpha, 0, speed * Time.deltaTime);
    // }

    // Clareia a tela. É uma coroutine pública: quem chamar pode esperar ela terminar
    // com "yield return canvasFade.FadeIn();" (ou só disparar com StartCoroutine)
    public IEnumerator FadeIn()
    {
        float speed = 1f / fadeDuration;

        while (imageFader.alpha > 0f)
        {
            imageFader.alpha = Mathf.MoveTowards(
                imageFader.alpha,
                0f,
                speed * Time.deltaTime
            );

            yield return null;
        }
    }

    public IEnumerator FadeOut()
    {
        float speed = 1f / fadeDuration;

        while (imageFader.alpha < 1f)
        {
            imageFader.alpha = Mathf.MoveTowards(
                imageFader.alpha,
                1f,
                speed * Time.deltaTime
            );

            yield return null;
        }
    }
}
