using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour
{
    public Mesh mesh;
    public Material material0;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "Maze";

        GetComponent<MeshRenderer>().materials[0] = material0;

    }
}
