using UnityEngine;

public class WarpSpin : MonoBehaviour
{
    public float degreesPerSecond = 90f;

    void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
