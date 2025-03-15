using System;
using System.Collections.Generic;
using UnityEditor.XR;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class CreatureBehaviour : MonoBehaviour
{
    
#region FOV-Vars
    public float angle = 30;
    public float height = 1.0f ;
    public float distance = 10 ; 
    public Color meshColor = Color.blue;
    public int scanFrequency = 30;
    public LayerMask lightLayer;
    public LayerMask obstacleLayer;
    public List<GameObject> Objects = new List<GameObject>();
    
    Collider[] colliders = new Collider[50];
    Mesh mesh;
    int count;
    float scanInterval;
    float scanTimer;
    

    #endregion

    private GameObject lightEdibleObject;

    public void Enter()
    {
       
        
    }

    void Start()
    {
        scanInterval = 1.0f / scanFrequency;
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0)
        {
            scanTimer += scanInterval;
            Scan();
        }
        
    }
    
    
#region FOV-Gizmos
    
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
            
            //far side
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
             scanInterval = 1.0f / scanFrequency;
     
         }
     
         private void OnDrawGizmos()
         {
             if (mesh)
             {
                 Gizmos.color = meshColor;
                 Gizmos.DrawMesh(mesh, transform.position, transform.rotation);
             }
             
             Gizmos.DrawWireSphere(transform.position, distance);
             for (int i = 0; i < count; i++)
             {
                 Gizmos.DrawSphere(colliders[i].transform.position,1f);
             }
             //draw gizmo for objects that are in sight of the creature
             Gizmos.color =  Color.green;
             foreach (var obj in Objects)
             {
                 Gizmos.DrawSphere(obj.transform.position,1.2f);
             }
         }
         
#endregion

#region FOV-Detection
private void Scan()
    {
        count = Physics.OverlapSphereNonAlloc(transform.position, distance, colliders, lightLayer, QueryTriggerInteraction.Collide);
        Objects.Clear();
        for (int i = 0; i < count; i++)
        {
            GameObject obj = colliders[i].gameObject;
            if (IsInSight(obj))
            {
                Objects.Add(obj);
            }
        }
        
    }
public bool IsInSight(GameObject obj)
{
    Vector3 origin = transform.position;
    Vector3 dest = obj.transform.position;
    Vector3 direction = dest - origin;
    
    //if light not in the range of the creature 
    if (direction.y < 0 || direction.y > height)
    {
        return false;
    }
    
    //if light not in the sight of the creature
    direction.y = 0;
    float deltaAngle = Vector3.Angle(direction, transform.forward);
    if (deltaAngle > angle)
    {
        return false;
    }

    origin.y += height / 2;
    dest.y = origin.y;
    
    //cant see the light if there's an obstacle in sight of the creature
    if (Physics.Linecast(origin, dest, obstacleLayer))
    {
        return false;
    }
    
    return true;
}
#endregion
    
    
}
