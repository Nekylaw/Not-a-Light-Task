using UnityEngine;

struct ClearZoneData
{
    public Vector3 Position;
    public float Radius;

    public ClearZoneData(Vector3 position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}

