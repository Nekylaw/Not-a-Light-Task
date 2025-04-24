using Game.Services.LightSources;
using System.Collections;
using UnityEngine;

public class OrbComponent : MonoBehaviour
{
    [SerializeField]
    private OrbSettings _orbSettings = null;

    private Coroutine _attractOrbCoroutine = null;

    public bool Shoot() { return true; }

    private void Start()
    {
        if (_orbSettings.HasLifetime)
            Destroy(gameObject, _orbSettings.Lifetime);
    }

    public void AttractTo(Vector3 lightPoint, LightSourceComponent lightSource)
    {
        if (_attractOrbCoroutine != null)
            return;

        _attractOrbCoroutine = StartCoroutine(AttractOrbCoroutine(lightPoint, lightSource));
    }

    private IEnumerator AttractOrbCoroutine(Vector3 lightPoint, LightSourceComponent lightSource)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        float timer = 0f;
        float duration = 3;
        float spiralSpeed = 1500 ; 
        float radius =  lightSource.Settings.AttractRange;
     

        Vector3 startPosition = transform.position;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            float currentRadius = Mathf.Lerp(radius, 0f, t);

            float angle = spiralSpeed * t * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius;

            Vector3 directionToCenter = (lightPoint - startPosition).normalized;
            Quaternion rotationToTarget = Quaternion.LookRotation(directionToCenter);
            Vector3 orbitalOffset = rotationToTarget * offset;

            transform.position = Vector3.Lerp(startPosition, lightPoint, t) + orbitalOffset;

            yield return null;
        }

        transform.position = lightPoint;

        LightSourcesService.Instance.SwitchOn(lightSource);
        _attractOrbCoroutine = null;
        Destroy(this.gameObject);   
    }

}
