

/**********************************************************************************************

    Luke O Brien - P11011180
    4th Year Project - Procedural Generation of Dungeons
    MeshGenerator.cs

************************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    //variables
    public MeshFilter walls;
    public SquareGrid squareGrid;
    List<Vector3> vertices;
    List<int> triangles;

    Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();

    //faster to check than a list
    HashSet<int> checkedVertices = new HashSet<int>();  

    private bool firstGeneration = true;
    private float squareSize;

    // generates the mesh based on the 2d array passed in and the size of each tile
    public void GenerateMesh(int[,] dungeon, float squareSize)
    {
        this.squareSize = squareSize;

        //if not the first generation, destroy the old collidors
        if(!firstGeneration)
        {
            Destroy(GameObject.Find("walls").GetComponent<MeshCollider>());
        }
        firstGeneration = false;
        
        //clear the structures in the event that we regenerate
        triangleDict.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        //init 
        squareGrid = new SquareGrid(dungeon, squareSize);
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int column = 0; column < squareGrid.squares.GetLength(0); column++)
        {
            for (int row = 0; row < squareGrid.squares.GetLength(1); row++)
            {
                TriangulateSquare(squareGrid.squares[column, row]);
            }
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        int textureRepeatCount = 20;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-dungeon.GetLength(0) / 2 * squareSize, dungeon.GetLength(0) / 2 * squareSize, vertices[i].x) * textureRepeatCount;
            float percentZ = Mathf.InverseLerp(-dungeon.GetLength(1) / 2 * squareSize, dungeon.GetLength(1) / 2 * squareSize, vertices[i].z) * textureRepeatCount;
            uvs[i] = new Vector2(percentX, percentZ);
        }
        mesh.uv = uvs;
   
        CreateWallMesh(dungeon, squareSize);
    }

    // creates a mesh for the walls out of the triangles we got from the marching squares algorithm
    void CreateWallMesh(int[,] dungeon, float squareSize)
    {
        CalculateMeshOutlines();
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        foreach(List<int> outline in outlines)
        {
            for(int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); // left
                wallVertices.Add(vertices[outline[i + 1]]); // right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                // triangle 1
                wallTriangles.Add(startIndex);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                // triangle 2
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;

        //add the texture to the walls
        int textureRepeatCount = 30;
        Vector2[] uvs = new Vector2[wallVertices.Count];
        for (int i = 0; i < wallVertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-dungeon.GetLength(0) / 2 * squareSize, dungeon.GetLength(0) / 2 * squareSize, wallVertices[i].x) * textureRepeatCount;
            float percentY = Mathf.InverseLerp(-dungeon.GetLength(1) / 2 * squareSize, dungeon.GetLength(1) / 2 * squareSize, wallVertices[i].y) * textureRepeatCount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        wallMesh.uv = uvs;

        // create and add a collider to the walls
        MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;

    }

    // sets the players spawn point to a tile that has no walls
    // needs improvement
    void SetPlayerSpawn(int[,] dungeon)
    {
        for (int column = dungeon.GetLength(0) / 2; column < dungeon.GetLength(0); column++)
        {
            for (int row = dungeon.GetLength(1) / 2; row < dungeon.GetLength(1); row++)
            {
                if(dungeon[column, row] == 0)
                {
                    Vector3 spawnLocation = new Vector3(-dungeon.GetLength(0) / 2 + column * squareSize + squareSize / 2, 0,
                        -dungeon.GetLength(1) / 2 + row *  + squareSize / 2);
                    FindObjectOfType<PlayerController>().SetSpawn(spawnLocation);
                }
            }
        }
    }

    // this is where we get the type of triangle that each tile of the dungeon will be represented by
    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // 1 point:
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);

                //we know that these will never be the outline 
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    // create the mesh from the node points
    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
        {
            CreateTriangle(points[0], points[1], points[2]);
        }
        if (points.Length >= 4)
        {
            CreateTriangle(points[0], points[2], points[3]);
        }
        if (points.Length >= 5)
        {
            CreateTriangle(points[0], points[3], points[4]);
        }
        if (points.Length >= 6)
        {
            CreateTriangle(points[0], points[4], points[5]);
        }
    }

    // create the vertices
    void AssignVertices(Node[] points)
    {
        for(int i = 0; i < points.Length; i++)
        {
            //if we havent set the vertexIndex
            if (points[i].vertexIndex == -1)    
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    // create triangles using the vertices of the nodes passed in
    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    //store the triangles with a list of indexes
    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if(triangleDict.ContainsKey(vertexIndexKey))
        {
            triangleDict[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDict.Add(vertexIndexKey, triangleList);

        }
    }

    //trace around the outside edge of the mesh triangles
    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDict[vertexIndex];

        for(int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for(int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }
        return -1;
    }

    // if the two vertex's only share one triangle, then it is an outside edge
    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDict[vertexA];
        int sharedTriangleCount = 0;

        for(int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if(trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                if(sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

 
    void CalculateMeshOutlines()
    {
        for(int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if(!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);

                if(newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);
                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }


    //follows the outline of the vertices
    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if(nextVertexIndex != -1)  
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    
    //a triangle struct for when we generate the mesh
    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;
        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }
    
    //void OnDrawGizmos()
    //{
    //    if (squareGrid != null)
    //    {
    //        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
    //        {
    //            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
    //            {

    //                Gizmos.color = (squareGrid.squares[x, y].topLeft.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].topRight.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].bottomRight.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

    //                Gizmos.color = (squareGrid.squares[x, y].bottomLeft.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * .4f);

    //                Gizmos.color = Color.grey;
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * .15f);
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * .15f);                   
    //                Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * .15f);

    //            }
    //        }
    //    }
    //}
}

// a square struct that will represent one tile in the dungeon for the marching squares algorithm
public class Square
{
    public ControlNode topLeft, topRight, bottomRight, bottomLeft;
    public Node centerTop, centerBottom, centerRight, centerLeft;
    public int configuration;
    public Square(ControlNode tl, ControlNode tr, ControlNode br, ControlNode bl)
    {
        topLeft = tl;
        topRight = tr;
        bottomLeft = bl;
        bottomRight = br;

        centerTop = topLeft.right;
        centerBottom = bottomLeft.right;
        centerRight = bottomRight.above;
        centerLeft = bottomLeft.above;

        if (topLeft.active)
        {
            configuration += 8;
        }
        if (topRight.active)
        {
            configuration += 4;
        }
        if (bottomRight.active)
        {
            configuration += 2;
        }
        if (bottomLeft.active)
        {
            configuration += 1;
        }
    }
}

// represents the whole dungeon
public class SquareGrid
{
    public Square[,] squares;
    public SquareGrid(int[,] dungeon, float squareSize)
    {
        int nodeCountX = dungeon.GetLength(0);
        int nodeCountY = dungeon.GetLength(1);
        float dungeonWidth = nodeCountX * squareSize;
        float dungeonHeight = nodeCountY * squareSize;

        ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
        for (int column = 0; column < nodeCountX; column++)
        {
            for (int row = 0; row < nodeCountY; row++)
            {
                Vector3 position = new Vector3(-dungeonWidth / 2 + column * squareSize + squareSize / 2, 0,
                    -dungeonHeight / 2 + row * squareSize + squareSize / 2);

                controlNodes[column, row] = new ControlNode(position, dungeon[column, row] == 1, squareSize);
            }
        }

        squares = new Square[nodeCountX - 1, nodeCountY - 1];
        for (int column = 0; column < nodeCountX - 1; column++)
        {
            for (int row = 0; row < nodeCountY - 1; row++)
            {
                squares[column, row] = new Square(controlNodes[column, row + 1], controlNodes[column + 1, row + 1],
                                                  controlNodes[column + 1, row], controlNodes[column, row]);
            }
        }
    }
}

// represents nodes in the center of each side of the square
public class Node
{
    public Vector3 position;
    public int vertexIndex = -1;

    public Node(Vector3 pos)
    {
        position = pos;
    }
}

// represents the corner nodes on each square
// each control node is responsible for the nodes above and to the right of it
public class ControlNode : Node
{
    public bool active;
    public Node above, right;

    public ControlNode(Vector3 pos, bool act, float squareSize) : base(pos)
    {
        active = act;
        above = new Node(position + Vector3.forward * squareSize / 2f);
        right = new Node(position + Vector3.right * squareSize / 2f);

    }
}