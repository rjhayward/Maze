using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PipeCases : MonoBehaviour
{

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

    //[System.Flags]
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

    public static MeshData meshDataCrossCase;
    public static MeshData meshDataTCase;
    public static MeshData meshDataLCase;
    public static MeshData meshDataStraightCase;
    public static MeshData meshDataDeadEndCase;

    // Start is called before the first frame update
    void Start()
    {
        meshDataTCase = GetTMeshData();
        meshDataLCase = GetLMeshData();
        meshDataCrossCase = GetCrossMeshData();
        meshDataStraightCase = GetStraightMeshData();
        meshDataDeadEndCase = GetDeadEndMeshData();
    }

    static Vector3 GetPoint(float u, float v)
    {

        Vector3 point;

        point.x = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Cos(u);
        point.y = pipeRadius * Mathf.Sin(v);
        point.z = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Sin(u);

        return point;
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

        for (int x = 0; x < 16; x++)
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
            rotationQuaternion = new Quaternion();
            rotationQuaternion.eulerAngles = new Vector3(0, 90 * i, 0);

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
            Vector3 point1 = GetPoint(0, 0) + new Vector3(-pipeRadius, 0f, (squareSize / 2));
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
            VertArray3Intersect.Add(new Vector3(0f, 0f, 0f));
        }

        // ARRAY 5
        v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            if (i >= (3 * (pipeSegments - 1) / 4) || i <= ((pipeSegments) / 4))
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
            if (!((i >= (3 * (pipeSegments - 1) / 4)) || (i <= ((pipeSegments) / 4))))
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





    public static MeshData GetRotatedMeshDataByCase(PipeCase mazeCase, int eulerAngle)
    {
        MeshData meshData = new MeshData();

        switch (mazeCase)
        {
            case PipeCase.Invalid: //invalid
                break;
            case PipeCase.DeadEnd: // dead end case
                meshData = new MeshData { vertices = (float3[])meshDataDeadEndCase.vertices.Clone(), triangles = (int[])meshDataDeadEndCase.triangles.Clone() };//GetDeadEndMeshData();
                break;
            case PipeCase.L: // L case
                meshData = new MeshData { vertices = (float3[])meshDataLCase.vertices.Clone(), triangles = (int[])meshDataLCase.triangles.Clone() };//GetLMeshData();
                break;
            case PipeCase.T: // T-Junction case
                meshData = new MeshData { vertices = (float3[])meshDataTCase.vertices.Clone(), triangles = (int[])meshDataTCase.triangles.Clone() };//GetTMeshData();
                break;
            case PipeCase.Cross: // crossroads case
                GetTMeshData();
                meshData = new MeshData { vertices = (float3[])meshDataCrossCase.vertices.Clone(), triangles = (int[])meshDataCrossCase.triangles.Clone() };//GetCrossMeshData();
                break;
            case PipeCase.Straight: // straight pipe case
                meshData = new MeshData { vertices = (float3[])meshDataStraightCase.vertices.Clone(), triangles = (int[])meshDataStraightCase.triangles.Clone() };//GetStraightMeshData();
                break;
            default: //invalid
                break;
        }

        //rotate all vertices around pivot (centre)
        Quaternion rotationQuaternion = new Quaternion();
        rotationQuaternion.eulerAngles = new Vector3(0, eulerAngle, 0);

        Vector3 centre = new Vector3(0, 0, squareSize / 2);

        for (int i = 0; i < meshData.vertices.Length; i++)
        {
            meshData.vertices[i] = rotationQuaternion * ((Vector3)meshData.vertices[i] - centre) + centre;
        }

        return meshData; // new MeshData() { vertices = Vertices.ToArray(), triangles = Triangles.ToArray() };
    }


}
