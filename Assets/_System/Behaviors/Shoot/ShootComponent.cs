using UnityEngine;

public class ShootComponent : MonoBehaviour
{
    [SerializeField]
    private Transform _firePoint = null;

    public bool Shoot(Ray aimRay)
    {
        Debug.DrawRay(_firePoint.position, aimRay.direction * 50, Color.yellow, 1f);

        if (Physics.Raycast(aimRay, out RaycastHit hit))
            Debug.Log("Shoot on " + hit.collider.name);

        return true;
    }
}
