using UnityEngine;

public class Wall : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Wall");
        if (collision.gameObject.tag == "Ball")
        {
            collision.gameObject.GetComponent<BallController>().ResetPosition();
            
        }
    }
}
