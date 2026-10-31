using DG.Tweening;
using UnityEngine;

// Move a barra em vai-e-vem. Serve para qualquer barra
[RequireComponent(typeof(Rigidbody2D))]
public class BarMover : MonoBehaviour
{
    public Vector2 moveOffset = Vector2.zero; 
    public float duration = 2f;  
    private Rigidbody2D rig;
    private Tween moveTween;

    void Start()
    {
        if (moveOffset == Vector2.zero) return;

        rig = GetComponent<Rigidbody2D>();
        moveTween = rig.DOMove(rig.position + moveOffset, duration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(UpdateType.Fixed);  // anda no mesmo ritmo da física
    }

    void OnDestroy()
    {
        // se a barra tinha um tween, para ele
        if (moveTween != null)
        {
            moveTween.Kill();
        }
    }
}
