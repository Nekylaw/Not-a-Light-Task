using UnityEngine;

[System.Serializable]
public class SerializableMatrix4x4
{
    public float[] Values = new float[16];

    public SerializableMatrix4x4(Matrix4x4 matrix)
    {
        for (int i = 0; i < 16; i++)
            Values[i] = matrix[i];
    }

    public Matrix4x4 ToMatrix()
    {
        Matrix4x4 m = new();
        for (int i = 0; i < 16; i++)
            m[i] = Values[i];
        return m;
    }
}