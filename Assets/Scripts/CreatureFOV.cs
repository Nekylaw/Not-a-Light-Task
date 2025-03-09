using System;
using UnityEditor.XR;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class Creature : MonoBehaviour
{
    public float angle = 30;
    public float height = 1.0f ;
    public float distance = 10 ; 
    public Color meshColor = Color.blue;

    private Mesh mesh;

    Mesh CreateFOVMesh()
    {
        Mesh mesh = new Mesh();
        int segments = 10;
        int numTriangles = (segments * 4) + 2 + 2;
        int numVertices = numTriangles * 3;
        
       
        
        int[] triangles = new int[numVertices];
        Vector3[] vertices = new Vector3[numVertices];
       

        Vector3 bottomCenter = Vector3.zero;
        Vector3 bottomLeft = Quaternion.Euler(0, -angle, 0) * Vector3.forward * distance;
        Vector3 bottomRight = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
        
        Vector3 topCenter = bottomCenter + Vector3.up * height;
        Vector3 topLeft = bottomLeft + Vector3.up * height;
        Vector3 topRight = bottomRight + Vector3.up * height;
        
        int vertexIndex = 0;
        
        //left side
        vertices[vertexIndex++] = bottomCenter;
        vertices[vertexIndex++] = bottomLeft;
        vertices[vertexIndex++] = topLeft;
        
        vertices[vertexIndex++] = topLeft;
        vertices[vertexIndex++] = topCenter;
        vertices[vertexIndex++] = bottomCenter;
        
        //right side 
        vertices[vertexIndex++] = bottomCenter;
        vertices[vertexIndex++] = topCenter;
        vertices[vertexIndex++] = topRight;
        
        vertices[vertexIndex++] = topRight;
        vertices[vertexIndex++] = bottomRight;
        vertices[vertexIndex++] = bottomCenter;
        
      
        //far side
        
        float currentAngle = -angle;
        float deltaAngle = (angle * 2) / segments;
        for (int i = 0; i < segments; i++)
        {
           
            bottomLeft = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * distance;
            bottomRight = Quaternion.Euler(0, currentAngle + deltaAngle, 0) * Vector3.forward * distance;
        
            topLeft = bottomLeft + Vector3.up * height;
            topRight = bottomRight + Vector3.up * height;
            
            vertices[vertexIndex++] = bottomLeft;
            vertices[vertexIndex++] = bottomRight;
            vertices[vertexIndex++] = topRight;
        
            vertices[vertexIndex++] = topRight;
            vertices[vertexIndex++] = topLeft;
            vertices[vertexIndex++] = bottomLeft;
        
        
            //top
            vertices[vertexIndex++] = topCenter;
            vertices[vertexIndex++] = topLeft;
            vertices[vertexIndex++] = topRight;

            //bottom
        
            vertices[vertexIndex++] = bottomCenter;
            vertices[vertexIndex++] = bottomRight;
            vertices[vertexIndex++] = bottomLeft;
            
            currentAngle += deltaAngle;
        }
      
        
        for (int i = 0; i < numVertices; i++)
        {
            triangles[i] = i;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        
        return mesh;
    }

    private void OnValidate()
    {
        mesh = CreateFOVMesh();

    }

    private void OnDrawGizmos()
    {
        if (mesh)
        {
            Gizmos.color = meshColor;
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation);
        }
    }
    
    
}
