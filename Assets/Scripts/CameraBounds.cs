using System.Collections; // necessário para usar coroutines (IEnumerator)
using Unity.Cinemachine;
using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    public CinemachineCamera cinemachine;
    public CanvasFade canvasFade;

    private void OnTriggerExit2D(Collider2D collision)
    {
        // ao parar o Play o Unity destrói os objetos e dispara este evento: nesse caso, ignora
        if (!gameObject.activeInHierarchy) return;
        if (canvasFade == null) return;

        if (collision.CompareTag("Ball"))
        {
            BallController ball = collision.gameObject.GetComponent<BallController>();

            // a sequência roda ao longo de vários frames, então precisa ser uma coroutine
            StartCoroutine(ResetSequence(ball));
        }
    }

    private IEnumerator ResetSequence(BallController ball)
    {
        // 1. escurece a tela e espera terminar
        yield return canvasFade.FadeOut();

        // 2. com a tela preta, volta a bola pro início e faz o corte seco da câmera
        cinemachine.PreviousStateIsValid = false;
        ball.ResetPosition();

        // 3. clareia a tela, já com a bola e a câmera no lugar
        yield return canvasFade.FadeIn();
    }
}
