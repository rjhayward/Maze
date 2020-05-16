using UnityEngine;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Security;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Entities.UniversalDelegates;

public class MazeGeneratorSystem : SystemBase
{
    // On start create 2 native arrays: verts and tris

    // On update fill the entities with new mesh values if key is pressed
    // Stitch the meshes together into the native arrays
    // Update the maze's mesh with this one stitched mesh (mesh update is expensive)

    public struct MeshData
    {
        public float3[] vertices { get; set; }
        public int[] triangles { get; set; }
    }

    public enum PipeCase
    {
        Invalid = 0,
        DeadEnd = 1,
        L = 2,
        T = 3,
        Cross = 4,
        Straight = 5
    }

    [System.Flags]
    public enum PipeConnections // enum representing flags for all possibilities of pipe connections
    {
        Empty = 0,
        Exists = 1,
        Up = 2,
        Right = 4,
        Down = 8,
        Left = 16
    }

    public static readonly float torusRadius = 10f;
    public static readonly float pipeRadius = 6f;
    public static readonly int pipeSegments = 17;
    public static readonly int torusSegments = 17;
    public static readonly float squareSize = 2 * torusRadius;// math.sqrt(2 * math.pow(torusRadius,2)) + pipeRadius;

    public static MeshData meshDataCrossCase = new MeshData();
    public static MeshData meshDataTCase = new MeshData();
    public static MeshData meshDataLCase = new MeshData();
    public static MeshData meshDataStraightCase = new MeshData();
    public static MeshData meshDataDeadEndCase = new MeshData();
    static Vector3 GetPoint(float u, float v)
    {

        Vector3 point;

        point.x = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Cos(u);
        point.y = pipeRadius * Mathf.Sin(v);
        point.z = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Sin(u);

        return point;
    }
    //Update mesh code
    void UpdateMesh(Vector3[] mazeVertices, int[] mazeTriangles)
    {
        Maze maze = GameObject.Find("Maze").GetComponent<Maze>();

        maze.mesh.Clear();

        maze.mesh.SetVertices(mazeVertices);
        maze.mesh.SetTriangles(mazeTriangles, 0); //submesh 0

        maze.mesh.RecalculateBounds();
        maze.mesh.RecalculateNormals();
    }

    public static MeshData StitchMeshes(MeshData meshData1, MeshData meshData2)
    {
        MeshData newMeshData = new MeshData();

        int newMeshVertsLength = meshData1.vertices.Length + meshData2.vertices.Length;

        newMeshData.vertices = new float3[newMeshVertsLength];

        for (int i = 0; i < meshData1.vertices.Length; i++)
        {
            newMeshData.vertices[i] = meshData1.vertices[i];
        }

        int k = 0;
        for (int i = meshData1.vertices.Length; i < newMeshVertsLength; i++)
        {
            newMeshData.vertices[i] = meshData2.vertices[k++];
        }

        int newMeshTrisLength = meshData1.triangles.Length + meshData2.triangles.Length;

        newMeshData.triangles = new int[newMeshTrisLength];

        for (int i = 0; i < meshData1.triangles.Length; i++)
        {
            newMeshData.triangles[i] = meshData1.triangles[i];
        }

        k = 0;
        for (int i = meshData1.triangles.Length; i < newMeshTrisLength; i++)
        {
            newMeshData.triangles[i] = meshData2.triangles[k++] + meshData1.vertices.Length;
        }

        return newMeshData;
    }
    public static PipeConnections[,] GetCaseArray(int[,] mazeArray)
    {
        PipeConnections[,] pipeCaseArray = new PipeConnections[16, 16]; // TODO un-hardcode this stuff lol

        for (int x = 0; x < 16;x++)
        {
            for (int y = 0; y < 16; y++)
            {
                if (mazeArray[x, y] == 0)
                {
                    pipeCaseArray[x, y] = PipeConnections.Empty;
                    continue;
                }
                pipeCaseArray[x, y] = PipeConnections.Exists;
                if (mazeArray[x, y] == 1)
                {
                    if (x - 1 >= 0)
                        if (mazeArray[x - 1, y] != 0)
                            pipeCaseArray[x, y] |= PipeConnections.Down;

                    if (x + 1 < 16)
                        if (mazeArray[x + 1, y] != 0)
                            pipeCaseArray[x, y] |= PipeConnections.Up;

                    if (y - 1 >= 0)
                        if (mazeArray[x, y - 1] != 0)
                            pipeCaseArray[x, y] |= PipeConnections.Right;

                    if (y + 1 < 16)
                        if (mazeArray[x, y + 1] != 0)
                            pipeCaseArray[x, y] |= PipeConnections.Left;
                }
            }
        }

        return pipeCaseArray;
    }

    public static MeshData GetCrossMeshData()
    {
        // TODO rename these lol!!
        List<float3> VertArray1 = new List<float3>(); // Root of T 
        List<float3> VertArray1Intersect = new List<float3>();


        List<float3> VertArray2 = new List<float3>(); // Left of T
        List<float3> VertArray2Intersect = new List<float3>();

        List<float3> VertArray3 = new List<float3>(); // Right of T 

        List<float3> UnrenderedArray = new List<float3>(); // opposite of Root


        // ARRAY 1
        float v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray1.Add(GetPoint(0, v));

            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        // ARRAY 2
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray2.Add(GetPoint(0, v));
            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        // ARRAY 3
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray3.Add(GetPoint(0, v) + squareSize * new Vector3(0f, 0f, 1f));
            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        // ARRAY 4
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            UnrenderedArray.Add(GetPoint(0, v) + squareSize * new Vector3(0f, 0f, 1f));
            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }


        //normalise start position for rotation
        for (int i = 0; i < VertArray2.Count; i++)
        {
            VertArray2[i] -= new float3(torusRadius, 0, 0);
            VertArray3[i] -= new float3(torusRadius, 0, 0);
        }

        //rotate 2nd and 4th array
        Quaternion rotationQuaternion = new Quaternion();
        rotationQuaternion.eulerAngles = new Vector3(0, 90, 0);

        Vector3 centre = new Vector3(0, 0, squareSize / 2);

        for (int i = 0; i < VertArray2.Count; i++)
        {
            VertArray2[i] = rotationQuaternion * ((Vector3)VertArray2[i] - centre) + centre;
            VertArray3[i] = rotationQuaternion * ((Vector3)VertArray3[i] - centre) + centre;
        }

        //un-normalise start position
        for (int i = 0; i < VertArray2.Count; i++)
        {
            VertArray2[i] += new float3(torusRadius, 0, 0);
            VertArray3[i] += new float3(torusRadius, 0, 0);
        }

        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray1Intersect.Add(new Vector3(0f, 0f, 0f));
        }

        // ARRAY 5
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            if (i >= (3 * (pipeSegments - 1) / 4) || i <= ((pipeSegments) / 4))
            {
                Vector3 IntersectionPoint;
                LineLineIntersection(out IntersectionPoint, VertArray1[i], UnrenderedArray[i] - VertArray1[i], VertArray2[i], VertArray3[i] - VertArray2[i]);
                VertArray2Intersect.Add(IntersectionPoint);
                VertArray1Intersect[i] = IntersectionPoint;
            }
            else
            {
                VertArray2Intersect.Add(GetPoint(0, v) + (squareSize / 2) * new Vector3(0f, 0f, 1f));
            }

            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        //normalise start position for rotation
        for (int i = 0; i < VertArray2Intersect.Count; i++)
        {
            VertArray2Intersect[i] -= new float3(torusRadius, 0, 0);
        }
        //rotate 6th array
        rotationQuaternion = new Quaternion();
        rotationQuaternion.eulerAngles = new Vector3(0, 90, 0);

        centre = new Vector3(0, 0, squareSize / 2);

        for (int i = 0; i < VertArray2Intersect.Count; i++)
        {
            VertArray2Intersect[i] = rotationQuaternion * ((Vector3)VertArray2Intersect[i] - centre) + centre;
        }

        //un-normalise start position
        for (int i = 0; i < VertArray2Intersect.Count; i++)
        {
            VertArray2Intersect[i] += new float3(torusRadius, 0, 0);
        }

        //List<Vector3> temp = new List<Vector3>();
        //for (int i = 0; i < (pipeSegments - 1); i++)
        //{
        //    temp.Add(new Vector3(0f, 0f, 0f));
        //}

        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            if (!((i >= (3 * (pipeSegments - 1) / 4)) || (i <= ((pipeSegments) / 4))))
            {
                VertArray1Intersect[VertArray1Intersect.Count - i] = VertArray2Intersect[(i + pipeSegments / 2) % (pipeSegments - 1)];
            }
        }

        //Debug.LogError(VertArray1.Count + "    " + VertArray1Intersect.Count);

        List<float3> ArrayA = new List<float3>();

        for (int i = 0; i < VertArray1.Count; i++)
        {
            ArrayA.Add(VertArray1[i]);
            ArrayA.Add(VertArray1Intersect[i]);
        }


        List<int> TrianglesA = new List<int>();

        int maxVertex = (2 * (pipeSegments - 1)) - 1;

        if (TrianglesA.Count < (maxVertex + 1) * 3)
        {
            for (int j = 0; j < (maxVertex + 1) * 3; j++)
            {
                if (TrianglesA.Count < (maxVertex + 1) * 3)
                {
                    TrianglesA.Add(0);
                }
            }
        }
        //create triangles list
        for (int i = 0; i < maxVertex + 1; i++)
        {
            if (i % 2 == 1)
            {
                TrianglesA[3 * i] = (i + 2) % (maxVertex + 1);
                TrianglesA[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesA[3 * i + 2] = i;
            }
            else
            {
                TrianglesA[3 * i] = i;
                TrianglesA[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesA[3 * i + 2] = (i + 2) % (maxVertex + 1);
            }
        }

        MeshData meshDataQuarterCross = new MeshData() { vertices = ArrayA.ToArray(), triangles = TrianglesA.ToArray() };

        for (int j = 0; j < meshDataQuarterCross.vertices.Length; j++)
        {
            meshDataQuarterCross.vertices[j] -= new float3(torusRadius, 0, 0);
        }

        MeshData[] meshDataArray = new MeshData[4];
        for (int i = 0; i < meshDataArray.Length; i++)
        {
            meshDataArray[i] = new MeshData() { vertices = new float3[meshDataQuarterCross.vertices.Length], triangles = new int[meshDataQuarterCross.triangles.Length] };
            meshDataQuarterCross.vertices.CopyTo(meshDataArray[i].vertices, 0);
            meshDataQuarterCross.triangles.CopyTo(meshDataArray[i].triangles, 0);
        }
        for (int i = 0; i < meshDataArray.Length; i++)
        {
            rotationQuaternion = new Quaternion
            {
                eulerAngles = new Vector3(0, 90 * i, 0)
            };

            centre = new Vector3(0, 0, squareSize / 2);

            for (int j = 0; j < meshDataArray[i].vertices.Length; j++)
            {
                meshDataArray[i].vertices[j] = rotationQuaternion * ((Vector3)meshDataArray[i].vertices[j] - centre) + centre;
            }
        }

        MeshData outMeshData;

        outMeshData = StitchMeshes(meshDataArray[0], meshDataArray[1]);
        outMeshData = StitchMeshes(outMeshData, meshDataArray[2]);
        outMeshData = StitchMeshes(outMeshData, meshDataArray[3]);
        return outMeshData;

    }

    public static MeshData GetLMeshData()
    {
        //List<float3> Vertices = new List<float3>();
        //List<int> Triangles = new List<int>();

        int renderTorusSegments = Mathf.RoundToInt(torusSegments * 0.25f); //render a quarter of a torus

        float3[] vertices = new float3[renderTorusSegments * pipeSegments * 4];

        int[] triangles = new int[renderTorusSegments * (pipeSegments) * 6];

        // fill up vertices array
        float u = 0;
        float v = 0;
        for (int i = 0; i < renderTorusSegments; i++) //for each torusSegment, we want to save all vertices in the loop (2 each time for a pipeSegments amount of times)
        {
            for (int j = 0; j < 2 * (pipeSegments - 1); j++)
            {

                Vector3 point1 = GetPoint(u + (2f * Mathf.PI / (torusSegments - 1)), v);

                Vector3 point2 = GetPoint(u, v);

                vertices[i * (pipeSegments * 2) + j * 2] = point1;
                vertices[i * (pipeSegments * 2) + j * 2 + 1] = point2;

                v += (2f * Mathf.PI) / (pipeSegments - 1);
            }
            u += (2f * Mathf.PI) / (torusSegments - 1);
        }

        //normalise start position
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= new float3(torusRadius, 0, 0);
        }


        //fill up triangles array
        int vertex = 0;
        for (int j = 0; j < renderTorusSegments; j++)
        {
            for (int i = 0; i < pipeSegments; i++)
            {
                triangles[6 * (pipeSegments) * j + 6 * i + 0] = vertex + 0;    //0  //reverse these for outside view
                triangles[6 * (pipeSegments) * j + 6 * i + 1] = vertex + 2;    //2
                triangles[6 * (pipeSegments) * j + 6 * i + 2] = vertex + 3;    //3

                triangles[6 * (pipeSegments) * j + 6 * i + 3] = vertex + 3;    //3  //reverse these for outside view
                triangles[6 * (pipeSegments) * j + 6 * i + 4] = vertex + 1;    //1
                triangles[6 * (pipeSegments) * j + 6 * i + 5] = vertex + 0;    //0 
                vertex += 2;
            }
        }

        return new MeshData() { vertices = vertices, triangles = triangles };
    }


    public static MeshData GetStraightMeshData()
    {
        List<float3> Vertices = new List<float3>();
        List<int> Triangles = new List<int>();
        //populate vertices list
        if (Vertices.Count < 2 * (pipeSegments - 1))
        {
            for (int j = 0; j < 2 * (pipeSegments - 1); j++)
            {
                if (Vertices.Count < 2 * (pipeSegments - 1))
                {
                    Vertices.Add(new float3(0f, 0f, 1f));
                }
            }
        }

        // create vertices list
        float v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            Vector3 point1 = GetPoint(0, v);
            Vector3 point2 = GetPoint(0, v) + squareSize * new Vector3(0f, 0f, 1f);

            v += (2f * Mathf.PI) / (pipeSegments - 1);

            Vertices[2 * i] = point1;

            Vertices[2 * i + 1] = point2;
        }

        //normalise start position
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] -= new float3(torusRadius, 0, 0);
        }
        //rotate all vertices around pivot (centre) to normalise rotation
        //Quaternion rotationQuaternion = new Quaternion();
        //rotationQuaternion.eulerAngles = new Vector3(0, 0, 0.5f * 360 / (pipeSegments-1));

        //Vector3 centre = new Vector3(0, 0, squareSize / 2);

        //for (int i = 0; i < Vertices.Count; i++)
        //{
        //    Vertices[i] = rotationQuaternion * ((Vector3)Vertices[i] - centre) + centre;
        //}


        //populate triangles list

        int maxVertex = (2 * (pipeSegments - 1)) - 1;

        if (Triangles.Count < (maxVertex + 1) * 3)
        {
            for (int j = 0; j < (maxVertex + 1) * 3; j++)
            {
                if (Triangles.Count < (maxVertex + 1) * 3)
                {
                    Triangles.Add(0);
                }
            }
        }

        //create triangles list
        for (int i = 0; i < maxVertex + 1; i++)
        {
            if (i % 2 == 1)
            {
                Triangles[3 * i] = (i + 2) % (maxVertex + 1);
                Triangles[3 * i + 1] = (i + 1) % (maxVertex + 1);
                Triangles[3 * i + 2] = i;
            }
            else
            {
                Triangles[3 * i] = i;
                Triangles[3 * i + 1] = (i + 1) % (maxVertex + 1);
                Triangles[3 * i + 2] = (i + 2) % (maxVertex + 1);
            }
        }

        return new MeshData() { vertices = Vertices.ToArray(), triangles = Triangles.ToArray() };
    }

    public static MeshData GetDeadEndMeshData()
    {
        List<float3> Vertices = new List<float3>();
        List<int> Triangles = new List<int>();
        //populate vertices list
        if (Vertices.Count < 2 * (pipeSegments - 1))
        {
            for (int j = 0; j < 2 * (pipeSegments - 1); j++)
            {
                if (Vertices.Count < 2 * (pipeSegments - 1))
                {
                    Vertices.Add(new float3(0f, 0f, 1f));
                }
            }
        }

        // create vertices list
        float v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            Vector3 point1 = GetPoint(0, 0) +  new Vector3(-pipeRadius, 0f, (squareSize / 2));
            Vector3 point2 = GetPoint(0, v) + squareSize * new Vector3(0f, 0f, 1f);

            v += (2f * Mathf.PI) / (pipeSegments - 1);

            Vertices[2 * i] = point1;

            Vertices[2 * i + 1] = point2;
        }

        //normalise start position
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] -= new float3(torusRadius, 0, 0);
        }
        //rotate all vertices around pivot (centre) to normalise rotation
        //Quaternion rotationQuaternion = new Quaternion();
        //rotationQuaternion.eulerAngles = new Vector3(0, 0, 0.5f * 360 / (pipeSegments-1));

        //Vector3 centre = new Vector3(0, 0, squareSize / 2);

        //for (int i = 0; i < Vertices.Count; i++)
        //{
        //    Vertices[i] = rotationQuaternion * ((Vector3)Vertices[i] - centre) + centre;
        //}


        //populate triangles list

        int maxVertex = (2 * (pipeSegments - 1)) - 1;

        if (Triangles.Count < (maxVertex + 1) * 3)
        {
            for (int j = 0; j < (maxVertex + 1) * 3; j++)
            {
                if (Triangles.Count < (maxVertex + 1) * 3)
                {
                    Triangles.Add(0);
                }
            }
        }

        //create triangles list
        for (int i = 0; i < maxVertex + 1; i++)
        {
            if (i % 2 == 1)
            {
                Triangles[3 * i] = (i + 2) % (maxVertex + 1);
                Triangles[3 * i + 1] = (i + 1) % (maxVertex + 1);
                Triangles[3 * i + 2] = i;
            }
            else
            {
                Triangles[3 * i] = i;
                Triangles[3 * i + 1] = (i + 1) % (maxVertex + 1);
                Triangles[3 * i + 2] = (i + 2) % (maxVertex + 1);
            }
        }

        return new MeshData() { vertices = Vertices.ToArray(), triangles = Triangles.ToArray() };
    }

    //NOTE: code below to get the intersection point of two lines was taken from
    //http://wiki.unity3d.com/index.php/3d_Math_functions?_ga=2.97783391.152188858.1589422343-1833034449.1588364422
    //
    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    //Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
    //same plane, use ClosestPointsOnTwoLines() instead.
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        
        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    public static MeshData GetTMeshData()
    {
        // TODO rename these lol!!
        List<float3> VertArray1 = new List<float3>(); // Root of T 
        List<float3> VertArray1Intersect = new List<float3>();


        List<float3> VertArray2 = new List<float3>(); // Left of T
        List<float3> VertArray2Intersect = new List<float3>();

        List<float3> VertArray3 = new List<float3>(); // Right of T
        List<float3> VertArray3Intersect = new List<float3>();

        List<float3> UnrenderedArray = new List<float3>(); // opposite of Root

        
        // ARRAY 1
        float v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray1.Add(GetPoint(0, v));

            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        // ARRAY 2
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray2.Add(GetPoint(0, v));
            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        // ARRAY 3
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray3.Add(GetPoint(0, v) + squareSize * new Vector3(0f, 0f, 1f));
            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        // ARRAY 4
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            UnrenderedArray.Add(GetPoint(0, v) + squareSize * new Vector3(0f, 0f, 1f));
            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }


        //normalise start position for rotation
        for (int i = 0; i < VertArray2.Count; i++)
        {
            VertArray2[i] -= new float3(torusRadius, 0, 0);
            VertArray3[i] -= new float3(torusRadius, 0, 0);
        }

        //rotate 2nd and 4th array
        Quaternion rotationQuaternion = new Quaternion();
        rotationQuaternion.eulerAngles = new Vector3(0, 90, 0);

        Vector3 centre = new Vector3(0, 0, squareSize / 2);

        for (int i = 0; i < VertArray2.Count; i++)
        {
            VertArray2[i] = rotationQuaternion * ((Vector3)VertArray2[i] - centre) + centre;
            VertArray3[i] = rotationQuaternion * ((Vector3)VertArray3[i] - centre) + centre;
        }

        //un-normalise start position
        for (int i = 0; i < VertArray2.Count; i++)
        {
            VertArray2[i] += new float3(torusRadius, 0, 0);
            VertArray3[i] += new float3(torusRadius, 0, 0);
        }

        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            VertArray1Intersect.Add(new Vector3(0f, 0f, 0f));
            VertArray3Intersect.Add(new Vector3(0f,0f,0f));
        }

            // ARRAY 5
            v = 0;
        for (int i = 0; i < (pipeSegments-1); i++)
        {
            if (i >= (3 * (pipeSegments-1) / 4) || i <= ((pipeSegments) / 4))
            {
                Vector3 IntersectionPoint;
                LineLineIntersection(out IntersectionPoint, VertArray1[i], UnrenderedArray[i] - VertArray1[i], VertArray2[i], VertArray3[i] - VertArray2[i]);
                VertArray3Intersect[i] = IntersectionPoint;
                VertArray2Intersect.Add(IntersectionPoint);
                VertArray1Intersect[i] = IntersectionPoint;
            }
            else
            {
                VertArray2Intersect.Add(GetPoint(0, v) + (squareSize / 2) * new Vector3(0f, 0f, 1f));
            }

            v += (2f * Mathf.PI) / (pipeSegments - 1);
        }

        //normalise start position for rotation
        for (int i = 0; i < VertArray2Intersect.Count; i++)
        {
            VertArray2Intersect[i] -= new float3(torusRadius, 0, 0);
        }        
        //rotate 6th array
        rotationQuaternion = new Quaternion();
        rotationQuaternion.eulerAngles = new Vector3(0, 90, 0);

        centre = new Vector3(0, 0, squareSize / 2);

        for (int i = 0; i < VertArray2Intersect.Count; i++)
        {
            VertArray2Intersect[i] = rotationQuaternion * ((Vector3)VertArray2Intersect[i] - centre) + centre;
        }

        //un-normalise start position
        for (int i = 0; i < VertArray2Intersect.Count; i++)
        {
            VertArray2Intersect[i] += new float3(torusRadius, 0, 0);
        }

        //List<Vector3> temp = new List<Vector3>();
        //for (int i = 0; i < (pipeSegments - 1); i++)
        //{
        //    temp.Add(new Vector3(0f, 0f, 0f));
        //}

        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            if (!((i >= (3 * (pipeSegments - 1) / 4))|| (i <= ((pipeSegments) / 4))))
            {
                VertArray3Intersect[i] = VertArray2Intersect[i];
                VertArray1Intersect[VertArray1Intersect.Count - i] = VertArray2Intersect[(i + pipeSegments / 2) % (pipeSegments - 1)];
            }
        }

        //Debug.LogError(VertArray1.Count + "    " + VertArray1Intersect.Count);

        List<float3> ArrayA = new List<float3>();

        for (int i = 0; i < VertArray1.Count; i++)
        {
            ArrayA.Add(VertArray1[i]);
            ArrayA.Add(VertArray1Intersect[i]);
        }


        List<float3> ArrayB = new List<float3>();

        for (int i = 0; i < VertArray2.Count; i++)
        {
            ArrayB.Add(VertArray2[i]);
            ArrayB.Add(VertArray2Intersect[i]);
        }


        List<float3> ArrayC = new List<float3>();

        for (int i = 0; i < VertArray3.Count; i++)
        {
            ArrayC.Add(VertArray3Intersect[i]);
            ArrayC.Add(VertArray3[i]);
        }


        List<int> TrianglesA = new List<int>();
        List<int> TrianglesB = new List<int>();
        List<int> TrianglesC = new List<int>();


        int maxVertex = (2 * (pipeSegments - 1)) - 1;

        if (TrianglesA.Count < (maxVertex + 1) * 3)
        {
            for (int j = 0; j < (maxVertex + 1) * 3; j++)
            {
                if (TrianglesA.Count < (maxVertex + 1) * 3)
                {
                    TrianglesA.Add(0);
                    TrianglesB.Add(0);
                    TrianglesC.Add(0);

                }
            }
        }
        //create triangles list
        for (int i = 0; i < maxVertex + 1; i++)
        {
            if (i % 2 == 1)
            {
                TrianglesA[3 * i] = (i + 2) % (maxVertex + 1);
                TrianglesA[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesA[3 * i + 2] = i;

                TrianglesB[3 * i] = (i + 2) % (maxVertex + 1);
                TrianglesB[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesB[3 * i + 2] = i;

                TrianglesC[3 * i] = (i + 2) % (maxVertex + 1);
                TrianglesC[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesC[3 * i + 2] = i;
            }
            else
            {
                TrianglesA[3 * i] = i;
                TrianglesA[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesA[3 * i + 2] = (i + 2) % (maxVertex + 1);

                TrianglesB[3 * i] = i;
                TrianglesB[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesB[3 * i + 2] = (i + 2) % (maxVertex + 1);

                TrianglesC[3 * i] = i;
                TrianglesC[3 * i + 1] = (i + 1) % (maxVertex + 1);
                TrianglesC[3 * i + 2] = (i + 2) % (maxVertex + 1);
            }
        }

        //for (int i = 0; i < ArrayA.Count; i++)
        //{
        //    Gizmos.DrawWireSphere(ArrayA[i], 0.5f);
        //}

        //for (int i = 0; i < ArrayB.Count; i++)
        //{
        //    Gizmos.DrawWireSphere(ArrayB[i], 0.5f);
        //}

        //for (int i = 0; i < ArrayC.Count; i++)
        //{
        //    Gizmos.DrawWireSphere(ArrayC[i], 0.5f);
        //}

        MeshData outputMeshData;

        MeshData meshDataQuarterCross = new MeshData() { vertices = ArrayA.ToArray(), triangles = TrianglesA.ToArray() };
        MeshData meshDataB = new MeshData() { vertices = ArrayB.ToArray(), triangles = TrianglesB.ToArray() };
        MeshData meshDataC = new MeshData() { vertices = ArrayC.ToArray(), triangles = TrianglesC.ToArray() };

        outputMeshData = StitchMeshes(StitchMeshes(meshDataQuarterCross, meshDataB), meshDataC);
        //normalise start position for rotation
        for (int i = 0; i < outputMeshData.vertices.Length; i++)
        {
            outputMeshData.vertices[i] -= new float3(torusRadius, 0, 0);
        }
        return outputMeshData;
    }


    
   

    public static MeshData GetMeshDataByCase(PipeCase mazeCase, int eulerAngle) 
    {
        
        //List<float3> Vertices = new List<float3>();
        //List<int> Triangles = new List<int>();

        MeshData meshData = new MeshData();

        switch (mazeCase)
        {
            case PipeCase.Invalid: //invalid
                break;
            case PipeCase.DeadEnd: // dead end case
                meshData = GetDeadEndMeshData();
                break;
            case PipeCase.L: // L case
                meshData = GetLMeshData();
                break;
            case PipeCase.T: // T-Junction case
                meshData = GetTMeshData();
                break;
            case PipeCase.Cross: // crossroads case
                GetTMeshData();
                meshData = GetCrossMeshData();
                break;
            case PipeCase.Straight: // straight pipe case
                meshData = GetStraightMeshData();
                break;
            default: //invalid
                break;
        }

        //rotate all vertices around pivot (centre)
        Quaternion rotationQuaternion = new Quaternion();
        rotationQuaternion.eulerAngles = new Vector3(0, eulerAngle, 0);

        Vector3 centre = new Vector3(0, 0, squareSize/2);

        for (int i = 0; i < meshData.vertices.Length; i++)
        {
            meshData.vertices[i] = rotationQuaternion * ((Vector3)meshData.vertices[i] - centre) + centre;
        }

        return meshData; // new MeshData() { vertices = Vertices.ToArray(), triangles = Triangles.ToArray() };
    }

    protected override void OnCreate()
    {
        //meshDataTCase = GetTMeshData();
        //meshDataLCase = GetLMeshData();
        //meshDataCrossCase = GetCrossMeshData();
        //meshDataStraightCase = GetStraightMeshData();
        //meshDataDeadEndCase = GetStraightMeshData();
    }
    protected override void OnUpdate()
    {



        float deltaTime = Time.DeltaTime;

        bool mazeNeedsUpdate = Input.GetKeyDown(KeyCode.Space);

        //size of array is maximum possible size of maze (256 tubes)
        NativeArray<int> numberOfVertsArray = new NativeArray<int>(256, Allocator.TempJob);
        NativeArray<int> numberOfTrisArray = new NativeArray<int>(256, Allocator.TempJob);

        //NativeArray<int> randSeed = new NativeArray<int>(1, Allocator.TempJob);
        //randSeed[0] = UnityEngine.Random.Range(0,177); //arbitrary random seed


        int[,] mazeArray2d = new int[,]
        {
            {0,1,1,1,1,1,0,1,1,1,1,1,0,0,1,0},
            {0,1,0,0,0,1,0,0,0,0,0,1,0,1,1,1},
            {0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,0},
            {0,1,0,1,0,0,1,0,0,0,1,0,0,1,0,0},
            {0,0,0,1,0,0,1,0,0,0,1,1,1,1,1,0},
            {0,1,1,1,1,1,1,0,0,0,0,0,1,0,1,1},
            {0,0,0,0,0,0,1,0,1,1,1,0,0,0,0,0},
            {2,1,1,1,1,1,1,0,1,0,1,1,1,1,1,2},
            {0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0},
            {1,1,0,0,0,1,0,1,0,1,1,1,1,1,1,1},
            {0,1,1,1,1,1,0,1,0,0,0,0,0,0,0,1},
            {0,0,0,1,0,1,0,1,1,1,1,1,1,1,0,1},
            {0,1,1,1,0,1,1,1,0,0,0,1,0,0,0,1},
            {0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1},
            {0,1,0,0,0,1,0,0,0,0,0,1,0,1,0,0},
            {0,1,1,1,1,1,1,1,1,1,0,1,0,1,1,1}
        };

        //int[,] mazeArray2d = new int[,]
        //{
        //    {0,0,0,0,0,1,0,0,0,1,0,0,0,0,1,1},
        //    {1,1,0,1,1,1,0,1,0,1,0,1,1,1,1,1},
        //    {1,1,0,1,1,1,0,1,0,1,0,0,0,0,1,1},
        //    {0,0,0,1,1,1,0,0,0,1,0,1,1,1,1,1},
        //    {1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1},
        //    {1,0,0,0,0,1,0,1,1,0,1,1,1,1,1,1},
        //    {1,0,1,1,1,1,0,1,1,0,1,0,0,0,0,1},
        //    {1,0,0,0,0,1,0,1,1,0,1,0,1,1,1,1},
        //    {1,1,1,1,0,1,0,1,1,0,1,0,1,1,1,1},
        //    {1,1,1,1,0,1,0,1,1,0,1,0,1,1,1,1},
        //    {1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1},
        //    {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        //    {1,1,1,1,0,1,0,0,0,1,0,0,0,0,1,1},
        //    {1,1,0,0,0,1,1,0,1,1,0,1,1,1,1,1},
        //    {1,1,0,1,0,1,1,0,1,1,0,1,1,1,1,1},
        //    {1,1,0,0,0,1,0,0,0,1,0,0,0,0,1,1}
        //};


        // TODO we want to convert the array above into an array of Enums showing us which directions have pipes attached

        PipeConnections[,] caseArray2d = GetCaseArray(mazeArray2d);

        NativeArray<PipeConnections> caseArray = new NativeArray<PipeConnections>(256, Allocator.TempJob);

        //convert 2D array into 1D array
        int index = 0;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                caseArray[index] = caseArray2d[x, y];
                index++;
            }
        }
        

        // assigns mesh data to each entity (currently just a placeholder mesh)
        Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices,
            ref Translation translation, ref Seed seed) =>
        {

            //entities[entityInQueryIndex] = entity;

            if (mazeNeedsUpdate && caseArray[entityInQueryIndex].HasFlag(PipeConnections.Exists))
            {
                //var random = new Unity.Mathematics.Random((uint)(entity.Index + entityInQueryIndex + randSeed[0] + 1) * 0x9F6ABC1);
                //var randomVector = math.normalizesafe(random.NextFloat3() - new float3(0.5f, 0.5f, 0.5f));
                //randomVector.y = 0;

                float3 newTranslation = new float3(0f, 0f, 0f);

                newTranslation.x = squareSize * (entityInQueryIndex % 16);
                newTranslation.y = 0f;
                newTranslation.z = squareSize * (entityInQueryIndex / 16);

                translation.Value = newTranslation;

               // translation.Value *= squareSize; //new Vector3((new Unity.Mathematics.Random((uint)(entityInQueryIndex+ (randSeed[0]++)))).NextFloat(-100f, 100f), 0, (new Unity.Mathematics.Random((uint)(entityInQueryIndex + (randSeed[1]++)))).NextFloat(-100f, 100f));

                MeshData meshData = new MeshData();

                // TODO choose case based on array of cases

                switch (caseArray[entityInQueryIndex])
                {
                    // Straight cases
                    case PipeConnections.Exists | PipeConnections.Left | PipeConnections.Right:
                        meshData = GetMeshDataByCase(PipeCase.Straight, 0);
                        break;
                    case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Down:
                        meshData = GetMeshDataByCase(PipeCase.Straight, 90);
                        break;
                    // L cases
                    case PipeConnections.Exists | PipeConnections.Down | PipeConnections.Right:
                        meshData = GetMeshDataByCase(PipeCase.L, 0);
                        break;
                    case PipeConnections.Exists | PipeConnections.Down | PipeConnections.Left:
                        meshData = GetMeshDataByCase(PipeCase.L, 90);
                        break;
                    case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Left:
                        meshData = GetMeshDataByCase(PipeCase.L, 180);
                        break;
                    case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Right:
                        meshData = GetMeshDataByCase(PipeCase.L, 270);
                        break;
                    // T cases
                    case PipeConnections.Exists | PipeConnections.Down | PipeConnections.Up | PipeConnections.Right:
                        meshData = GetMeshDataByCase(PipeCase.T, 0);
                        break;
                    case PipeConnections.Exists |  PipeConnections.Left | PipeConnections.Right | PipeConnections.Down :
                        meshData = GetMeshDataByCase(PipeCase.T, 90);
                        break;
                    case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Down | PipeConnections.Left:
                        meshData = GetMeshDataByCase(PipeCase.T, 180);
                        break;
                    case PipeConnections.Exists | PipeConnections.Left | PipeConnections.Right | PipeConnections.Up:
                        meshData = GetMeshDataByCase(PipeCase.T, 270);
                        break;
                    // DeadEnd cases
                    case PipeConnections.Exists | PipeConnections.Left:
                        meshData = GetMeshDataByCase(PipeCase.DeadEnd, 0); // TODO change to PipeCase.DeadEnd case
                        break;
                    case PipeConnections.Exists | PipeConnections.Up:
                        meshData = GetMeshDataByCase(PipeCase.DeadEnd, 90); // TODO change to PipeCase.DeadEnd case
                        break;
                    case PipeConnections.Exists | PipeConnections.Right:
                        meshData = GetMeshDataByCase(PipeCase.DeadEnd, 180); // TODO change to PipeCase.DeadEnd case
                        break;
                    case PipeConnections.Exists | PipeConnections.Down:
                        meshData = GetMeshDataByCase(PipeCase.DeadEnd, 270); // TODO change to PipeCase.DeadEnd case
                        break;
                    // Cross case
                    case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Down | PipeConnections.Left | PipeConnections.Right:
                        meshData = GetMeshDataByCase(PipeCase.Cross, 0);
                        break;
                    //exit case
                    case PipeConnections.Exists:
                        meshData = GetMeshDataByCase(PipeCase.Cross, 0);
                        break;
                        //default:
                        //    meshData = GetMeshDataByCase(PipeCase.Cross, 0); // TODO change to PipeCase.DeadEnd case
                        //    break;

                }




                if (meshData.vertices != null)
                {

                    float3[] vertices = meshData.vertices;
                    int[] triangles = meshData.triangles;


                    //populate vertex buffer with empty vertices
                    if (Vertices.Length < vertices.Length)
                    {
                        for (int j = 0; j < vertices.Length; j++)
                        {
                            if (Vertices.Length < vertices.Length)
                            {
                                Vertices.Add(new Float3BufferElement { Value = new float3(0f, 0f, 1f) });
                            }
                        }
                    }
                    //fill in vertices
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var vertex = Vertices[i];
                        vertex.Value = vertices[i] + translation.Value;
                        Vertices[i] = vertex;
                        numberOfVertsArray[entityInQueryIndex]++;
                    }

                    //populate triangles buffer with empty triangles
                    if (Triangles.Length < triangles.Length)
                    {
                        for (int j = 0; j < triangles.Length; j++)
                        {
                            if (Triangles.Length < triangles.Length)
                            {
                                Triangles.Add(new IntBufferElement { Value = 0 });
                            }
                        }
                    }
                    //fill in triangles
                    for (int i = 0; i < triangles.Length; i++)
                    {
                        var triangle = Triangles[i];
                        triangle.Value = triangles[i];
                        Triangles[i] = triangle;
                    }

                    //export the number of verts/tris to outside the job
                    numberOfVertsArray[entityInQueryIndex] = vertices.Length;
                    numberOfTrisArray[entityInQueryIndex] = triangles.Length;


                }

                // START OF PLACEHOLDER CODE

                //if (Vertices.Length < 2 * (pipeSegments - 1))
                //{
                //    //populate vertex buffer
                //    for (int j = 0; j < 2 * (pipeSegments - 1); j++)
                //    {
                //        if (Vertices.Length < 2 * (pipeSegments - 1))
                //        {
                //            Vertices.Add(new Float3BufferElement { Value = new float3(0f, 0f, 1f) });
                //        }
                //    }
                //}

                //float v = 0;
                ////used to generate the array of vertices as required 
                //Float3BufferElement vertex;
                //for (int i = 0; i < (pipeSegments - 1); i++)
                //{
                //    Vector3 point1 = (Vector3)translation.Value + GetPoint(0, v);
                //    Vector3 point2 = (Vector3)translation.Value + GetPoint(0, v) + torusRadius / 2 * new Vector3(0f, 0f, 2f);

                //    v += (2f * Mathf.PI) / (pipeSegments - 1);

                //    vertex = Vertices[2 * i];
                //    vertex.Value = point1;
                //    Vertices[2 * i] = vertex;

                //    vertex = Vertices[2 * i + 1];
                //    vertex.Value = point2;
                //    Vertices[2 * i + 1] = vertex;

                //    numberOfVertsArray[entityInQueryIndex] += 2;
                //}


                //int maxVertex = (2 * (pipeSegments - 1)) - 1;

                ////populate triangles buffer
                //if (Triangles.Length < (maxVertex + 1) * 3)
                //{
                //    for (int j = 0; j < (maxVertex + 1) * 3; j++)
                //    {
                //        if (Triangles.Length < (maxVertex + 1) * 3)
                //        {
                //            Triangles.Add(new IntBufferElement { Value = 0 });
                //        }
                //    }
                //}

                ////used to generate the array of triangles as required 
                //IntBufferElement triangle;
                //for (int i = 0; i < maxVertex + 1; i++)
                //{
                //    if (i % 2 == 1)
                //    {
                //        triangle = Triangles[3 * i];
                //        triangle.Value = (i + 2) % (maxVertex + 1);
                //        Triangles[3 * i] = triangle;

                //        triangle = Triangles[3 * i + 1];
                //        triangle.Value = (i + 1) % (maxVertex + 1);
                //        Triangles[3 * i + 1] = triangle;

                //        triangle = Triangles[3 * i + 2];
                //        triangle.Value = i;
                //        Triangles[3 * i + 2] = triangle;

                //    }
                //    else
                //    {
                //        triangle = Triangles[3 * i];
                //        triangle.Value = i;
                //        Triangles[3 * i] = triangle;

                //        triangle = Triangles[3 * i + 1];
                //        triangle.Value = (i + 1) % (maxVertex + 1);
                //        Triangles[3 * i + 1] = triangle;

                //        triangle = Triangles[3 * i + 2];
                //        triangle.Value = (i + 2) % (maxVertex + 1);
                //        Triangles[3 * i + 2] = triangle;
                //    }

                //    numberOfTrisArray[entityInQueryIndex] += 3;
                //}


                // END OF PLACEHOLDER CODE


            }
        }).ScheduleParallel();
        CompleteDependency();

        caseArray.Dispose();
        //randSeed.Dispose();

        int numVerts = 0;
        int numTris = 0;

        for (int i = 0; i < numberOfVertsArray.Length; i++)
        {
            numVerts += numberOfVertsArray[i];
        }

        for (int i = 0; i < numberOfTrisArray.Length; i++)
        {
            numTris += numberOfTrisArray[i];
        }





        NativeArray<Vector3> mazeVertices = new NativeArray<Vector3>(numVerts, Allocator.TempJob);
        NativeArray<int> mazeTriangles = new NativeArray<int>(numTris, Allocator.TempJob);


        // Stitch together meshes
        Entities.ForEach((int entityInQueryIndex, DynamicBuffer<IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices) =>
        {
            if (mazeNeedsUpdate)
            {
                int cumulativeNumVerts = 0;

                for (int i = 0; i < entityInQueryIndex; i++)
                {
                    cumulativeNumVerts += numberOfVertsArray[i];
                }

                for (int j = 0; j < Vertices.Length + cumulativeNumVerts; j++)
                {
                    if (j < cumulativeNumVerts)
                    {
                        mazeVertices[j] = mazeVertices[j];
                    }
                    else
                    {
                        mazeVertices[j] = Vertices[j - cumulativeNumVerts].Value;
                    }
                }

                int cumulativeNumTris = 0;
                for (int i = 0; i < entityInQueryIndex; i++)
                {
                    cumulativeNumTris += numberOfTrisArray[i];
                }

                for (int j = 0; j < Triangles.Length + cumulativeNumTris; j++)
                {
                    if (j < cumulativeNumTris)
                    {
                        mazeTriangles[j] = mazeTriangles[j];
                    }
                    else
                    {
                        mazeTriangles[j] = Triangles[j - cumulativeNumTris].Value + cumulativeNumVerts;
                    }
                }
            }
        }).Schedule();

        CompleteDependency();

        if (mazeNeedsUpdate)
        {
            UpdateMesh(mazeVertices.ToArray(), mazeTriangles.ToArray());
        }

        numberOfVertsArray.Dispose();

        numberOfTrisArray.Dispose();

        mazeVertices.Dispose();
        mazeTriangles.Dispose();
    }
}





// TODO check if any of this is still useful

//    for (int i = 0; i < entities.Length; i++)
//    {

//        Vector3[] originalVertices = maze.mesh.vertices;
//        int[] originalTriangles = maze.mesh.triangles;

//        Vector3[] verticesToWrite = new Vector3[vertices.Length + originalVertices.Length];
//        int[] trianglesToWrite = new int[triangles.Length + originalTriangles.Length];

//        for (int j = 0; j < vertices.Length + originalVertices.Length; j++)
//        {
//            if (j < originalVertices.Length)
//            {
//                verticesToWrite[j] = originalVertices[j];
//            }
//            else
//            {
//                verticesToWrite[j] = vertices[j - originalVertices.Length].Value;
//            }
//        }

//        for (int j = 0; j < triangles.Length + originalTriangles.Length; j++)
//        {
//            if (j < originalTriangles.Length)
//            {
//                trianglesToWrite[j] = originalTriangles[j];
//            }
//            else
//            {
//                trianglesToWrite[j] = triangles[j - originalTriangles.Length].Value + originalVertices.Length;
//            }
//        }


//    }
