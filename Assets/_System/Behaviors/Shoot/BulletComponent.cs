using UnityEngine;

public class BulletComponent : MonoBehaviour
{
    [SerializeField]
    private BulletSettings _bulletSettings = null;

    public bool Shoot() { return true; }

    private void Start()
    {
        Destroy(gameObject, _bulletSettings.Lifetime);
    }

}
