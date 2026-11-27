using UnityEngine;

public class FinishHole : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        ball.Finish();
    }
}
