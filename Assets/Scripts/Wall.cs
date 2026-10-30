using UnityEngine;

public class Wall : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Ball")
        {
            collision.gameObject.GetComponent<BallController>().ResetPosition();
            
        }
    }
}
