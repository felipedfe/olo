using UnityEngine;
using UnityEngine.InputSystem;

public enum BallState
{
    Ready,
    Moving,
    Attached,
    Finished
}

public class BallController : MonoBehaviour
{
    [SerializeField] private BallState currentState = BallState.Ready;
    private Rigidbody2D rig;
    private Vector2 direction;
    private Vector2 initialDirection = new Vector2(1, 1).normalized;
    private Vector3 initialPosition;

    public float speed = 5f;

    void Awake() // esse método roda antes do Start()
    {
        direction = initialDirection;
        rig = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;
    }

    void OnMove(InputValue value)
    {

        if (!value.isPressed) return;

        if (currentState == BallState.Ready)
        {
            currentState = BallState.Moving;
            Debug.Log("Ball Moving");

            rig.linearVelocity = direction * speed;
        }
    }

    public void ResetPosition()
    {
        currentState = BallState.Ready;

        rig.linearVelocity = Vector2.zero;
        rig.angularVelocity = 0f;

        rig.position = initialPosition;
        direction = initialDirection;
    }

    public void Reflect(Vector2 normal)
    {
        if (currentState != BallState.Moving) return;

        // Esse método faz com q a bola sempre rebate num angulo de 45
        Vector2 axisNormal = SnapToAxis(normal);

        // só reflete se a bola estiver indo contra a superfície
        if (Vector2.Dot(direction, axisNormal) < 0)
        {
            direction = Vector2.Reflect(direction, axisNormal);
        }

        rig.linearVelocity = direction * speed;
    }

    // Transforma qualquer normal em (±1, 0) ou (0, ±1)
    // Esse método faz com q a bola sempre rebate num angulo de 45
    private Vector2 SnapToAxis(Vector2 normal)
    {
        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
        {
            return new Vector2(Mathf.Sign(normal.x), 0); // bateu na lateral
        }
        else
        {
            return new Vector2(0, Mathf.Sign(normal.y)); // bateu na ponta
        }
    }

}
