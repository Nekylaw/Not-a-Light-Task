using UnityEngine;

public interface ICullable
{
    enum CullMode
    {
        Frustum,     
        Range      
    }

    CullMode Mode { get; }

    float CullRange { get; } 

    int CullableIndex { get; set; }

    void OnBecomeVisible();
    void OnBecomeInvisible();

    Vector3 GetCullPosition();
    float GetCullRadius();
}
