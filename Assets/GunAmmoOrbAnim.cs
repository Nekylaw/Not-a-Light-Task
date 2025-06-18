using UnityEngine;
using DG.Tweening;

public class GunAmmoOrbAnim : MonoBehaviour
{
    public float moveDuration = 2f;
    private SphereCollider sphereCollider;

    void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            Debug.LogError("SphereCollider manquant !");
            return;
        }

        foreach (Transform ball in transform)
        {
            MoveBallLoop(ball);
        }
    }
    
    void LoopMove(Transform ball)
    {
        Vector3 nextTarget = GetRandomPointInsideSphere();

        ball.DOLocalMove(nextTarget, moveDuration)
            .SetEase(Ease.OutCirc)
            .OnComplete(() => LoopMove(ball));
    }


    void MoveBallLoop(Transform ball)
    {
        Vector3 nextTarget = GetRandomPointInsideSphere();

        ball.DOLocalMove(nextTarget, moveDuration)
            .SetEase(Ease.OutCirc)
            .OnComplete(() => MoveBallLoop(ball)); 
    }

    Vector3 GetRandomPointInsideSphere()
    {
        Vector3 randomPoint = Random.insideUnitSphere * sphereCollider.radius;
        return randomPoint + sphereCollider.center;
    }
    
}