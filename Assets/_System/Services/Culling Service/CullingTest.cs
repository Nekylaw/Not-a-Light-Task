using Game.Services.CullingService;
using UnityEngine;

public class CullerTest : MonoBehaviour, ICullable
{
    [SerializeField]
    private ICullable.ECullMode _mode;

    [SerializeField]
    private float _cullRange = 0f;

    [SerializeField]
    private float _cullRadius = 0f;

    public float CullRange => _cullRange;
    public ICullable.ECullMode CullMode => _mode;
    public int CullableIndex { get; set; }

    public void OnEnable()
    {
        CullingService.Instance.Register(this);
    }

    public void OnDisable()
    {
        CullingService.Instance.Unregister(this);
    }

    public void OnBecomeVisible()
    {
        //Debug.Log($"[CullerTest] {gameObject.name} became visible");
        GetComponent<Renderer>().enabled = true;

    }

    public void OnBecomeInvisible()
    {
        //Debug.Log($"[CullerTest] {gameObject.name} became invisible");
        GetComponent<Renderer>().enabled = false;

    }

    public Vector3 GetCullPosition() => transform.position;
    public float GetCullRadius() => _cullRadius;

    private void Update()
    {
        //Debug.Log($"[CullerTest] {gameObject.name} visibility: {_isVisible}");
    }
}