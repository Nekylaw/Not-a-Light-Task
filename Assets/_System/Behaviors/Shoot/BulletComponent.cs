using Game.Services.LightSources;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BulletComponent : MonoBehaviour
{
    [SerializeField]
    private BulletSettings _bulletSettings = null;

    private Coroutine _attractOrbCoroutine = null;

    public bool Shoot() { return true; }

    private void Start()
    {
        Destroy(gameObject, _bulletSettings.Lifetime);
    }

    public void AttractTo(Vector3 lightPoint, LightSourceComponent lightSource)
    {
        if (_attractOrbCoroutine != null)
            return;

        _attractOrbCoroutine = StartCoroutine(AttractOrbCoroutine(lightPoint, lightSource));
    }

    private IEnumerator AttractOrbCoroutine(Vector3 lightPoint, LightSourceComponent lightSource)
    {
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        while (Vector3.Distance(transform.position, lightPoint) > 1)
        {
            transform.position = Vector3.MoveTowards(transform.position, lightPoint, 10 * Time.deltaTime); //@todo orbital effect here
            //Debug.Log("Attract");

            yield return null;
        }

        LightSourcesService.Instance.SwitchOn(lightSource);
        _attractOrbCoroutine = null;
    }
}
