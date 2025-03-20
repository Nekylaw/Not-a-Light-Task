using UnityEngine;

public class FogRenderer : MonoBehaviour
{
    public Material fogMaterial;
    private ComputeBuffer clearZonesBuffer;

    GameObject[] _clearZones = new GameObject[0];

    struct ClearZone
    {
        public Vector3 position;
        public float radius;
    }

    private ClearZone[] clearZones;
    private const int MAX_ZONES = 100; 

    void Start()
    {
        clearZones = new ClearZone[MAX_ZONES];
        clearZonesBuffer = new ComputeBuffer(MAX_ZONES, sizeof(float) * 4);
        fogMaterial.SetInt("_ClearZoneCount", 0);

        _clearZones = GameObject.FindGameObjectsWithTag("Receptacle");

        Debug.Log("Clear count: " + _clearZones.Length);
    }

    void Update()
    {
        int count = 0;
        foreach (var obj in _clearZones)
        {
            if (count >= MAX_ZONES) break;

            clearZones[count].position = obj.transform.position;
            clearZones[count].radius = /*obj.transform.localScale.x * 0.5f*/ 10f;
            count++;
        }

        // Envoyer les zones au shader
        clearZonesBuffer.SetData(clearZones);
        fogMaterial.SetInt("_ClearZoneCount", count);
        fogMaterial.SetBuffer("_ClearZones", clearZonesBuffer);
    }

    void OnDestroy()
    {
        if (clearZonesBuffer != null) clearZonesBuffer.Release();
    }
}
