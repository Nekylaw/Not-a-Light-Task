using UnityEngine;

public interface ICullable
{
    enum ECullMode
    {
        Frustum,
        Range
    }

    ECullMode CullMode { get; }

    float CullRange { get; }

    int CullableIndex { get; set; }

    void OnBecomeVisible();
    void OnBecomeInvisible();

    Vector3 GetCullPosition();
    float GetCullRadius();
}
