

/**********************************************************************************************

    Luke O Brien - P11011180
    4th Year Project - Procedural Generation of Dungeons
    BinarySpacePartitionGenerator.cs

************************************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BinarySpacePartitionGenerator : MonoBehaviour
{
    // variables
    int height, width;
    public int minRoomSize, maxRoomSize;

    // 2d array to represent the dungeon
    private int[,] dungeon;

    // called once on start
    void Start()
    {
        // retrieve dungeon dimensions
        width = PlayerPrefs.GetInt("ppWidth");
        height = PlayerPrefs.GetInt("ppWidth");

        // create the dungeon
        SubDungeon rootSubDungeon = new SubDungeon(new Rect(0, 0, height, width));
        GenerateBinarySpacePartition(rootSubDungeon);
        rootSubDungeon.CreateRoom();

        // set up the dungeon 2d array
        Init2DArray();
        AddRoomsToDungeonArray(rootSubDungeon);
        AddCorridorsToDungeonArray(rootSubDungeon);
        //PrintDungeonInConsole();

        // this block of code is just adding a border of walls around the generated dungeon
        int borderSize = 5;
        int[,] borderedDungeon = new int[width + borderSize * 2, height + borderSize * 2];

        // iterate through the 2d array. GetLength(0) returns the width of the array
        for (int column = 0; column < borderedDungeon.GetLength(0); column++)
        {
            // GetLength(1) returns the height of the array
            for (int row = 0; row < borderedDungeon.GetLength(1); row++)
            {
                // putting the original array into the bordered one
                if (column >= borderSize && column < width + borderSize && row >= borderSize && row < height + borderSize)
                {
                    borderedDungeon[column, row] = dungeon[column - borderSize, row - borderSize];
                }
                else // add the border
                {
                    borderedDungeon[column, row] = 1;
                }
            }
        }

        // create the 3d mesh
        MeshGenerator gen = GetComponent<MeshGenerator>();
        gen.GenerateMesh(borderedDungeon, 1);

        //export to csv
        FindObjectOfType<GameManager>().ExportDungeonData(borderedDungeon);
    }

    // generates a new dungeon when I left-click
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Start();
        }
    }

    


    // this is where we create the binary space partition tree
    public void GenerateBinarySpacePartition(SubDungeon subDungeon)
    {
        // checks to see if the node has no children
        if (subDungeon.IsLeaf())
        {
            // if the sub-dungeon is too big, then split it
            if ((subDungeon.rect.width > maxRoomSize) || (subDungeon.rect.height > maxRoomSize) || (Random.Range(0.0f, 1.0f) > 0.25))
            {
                // if we split, then recursively continue spliting the sub dungeons
                if (subDungeon.Split(minRoomSize, maxRoomSize))
                {
                    GenerateBinarySpacePartition(subDungeon.left);
                    GenerateBinarySpacePartition(subDungeon.right);
                }
            }
        }
    }

    // this is where we add the rooms to the 2d array that will be used to represent the dungeon
    public void AddRoomsToDungeonArray(SubDungeon subDungeon)
    {
        // dont continue further if the subdungeon doesnt exist
        if (subDungeon == null)
        {
            return;
        }
        // if the node has no children
        if (subDungeon.IsLeaf())
        {
            // iterate through the room and add it to the dungeon array
            for (int i = (int)subDungeon.room.x; i < subDungeon.room.xMax; i++)
            {
                
                for (int j = (int)subDungeon.room.y; j < subDungeon.room.yMax; j++)
                {
                    dungeon[i, j] = 0;
                }
            }
        }
        // if its not a leaf then move further down the tree to its children
        else
        {
            AddRoomsToDungeonArray(subDungeon.left);
            AddRoomsToDungeonArray(subDungeon.right);
        }
    }

    // this is where we add the corridors to the 2d array that will be used to represent the dungeon
    void AddCorridorsToDungeonArray(SubDungeon subDungeon)
    {
        // dont continue further if the subdungeon doesnt exist
        if (subDungeon == null)
        {
            return;
        }

        // recursively traverse the tree
        AddCorridorsToDungeonArray(subDungeon.left);
        AddCorridorsToDungeonArray(subDungeon.right);

        // map the corridors to the dungeon 2d array
        foreach (Rect corridor in subDungeon.corridors)
        {
            for (int i = (int)corridor.x; i < corridor.xMax; i++)
            {
                for (int j = (int)corridor.y; j < corridor.yMax; j++)
                {
                    // create a corridor 3 tiles wide
                    dungeon[i, j] = 0;
                    dungeon[i + 1, j + 1] = 0;
                    dungeon[i - 1, j - 1] = 0;
                }
            }
        }
    }

    // initializing the array
    // we fill it with walls so that we can 'carve' the rooms into it
    void Init2DArray()
    {
        dungeon = new int[width, height];
        for (int column = 0; column < width; column++)
        {
            for (int row = 0; row < height; row++)
            {
                dungeon[column, row] = 1;
            }
        }
    }

    // prints the dungeon to the console
    void PrintDungeonInConsole()
    {
        for (int column = 0; column < width; column++)
        {
            string str = "";
            for (int row = 0; row < height; row++)
            {
                str += ", " + dungeon[column, row];
            }
            Debug.Log(str);
        }
    }

    

    // This functions job is to use the gizmos in the editor to display a representation of the dungeon 2D array
    //private void OnDrawGizmos()
    //{
    //    if (dungeon != null) // if the dungeon array was filled
    //    {
    //        for (int column = 0; column<width; column++)
    //        {
    //            for (int row = 0; row<height; row++)
    //            {
    //                if (dungeon[column, row] == 1)
    //                {
    //                    Gizmos.color = Color.black; // set the color of the gizmo to black
    //                }
    //                else
    //                {
    //                    Gizmos.color = Color.white; // set the color of the gizmo to black
    //                }
    //                Vector3 position = new Vector3(-width / 2 + column + 0.5f, 0, -height / 2 + row + 0.5f); //get the position of the array element
    //                Gizmos.DrawCube(position, Vector3.one); // draw a cube 
    //            }
    //        }
    //    }
    //}
}
// represents the splits
public class SubDungeon
{
    //represents the SubDungeons resulting from the split
    public SubDungeon left, right;

    // using a rect to represent a room in a SubDungeon
    public Rect rect;
    public Rect room = new Rect(-1, -1, 0, 0); // setting it to null

    //create a list of rect's to represent corridors
    public List<Rect> corridors = new List<Rect>();

    // constructor
    public SubDungeon(Rect mrect)
    {
        rect = mrect;
    }

    // checking to see if a sub dungeon is a leaf on the tree
    // in other words, if they have no child nodes
    public bool IsLeaf()
    {
        if (left == null && right == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // splits a subdungeon into two more subdungeons
    public bool Split(int minRoomSize, int maxRoomSize)
    {
        // make sure that it is a node with no children
        if (!IsLeaf())
        {
            return false;
        }

        bool splitHorizontally;

        // this block of code is deciding whether or not we split horizontally
        //based on the shape of the rooms
        if (rect.width / rect.height >= 1.20)
        {
            splitHorizontally = false;
        }
        else if (rect.height / rect.width >= 1.20)
        {
            splitHorizontally = true;
        }
        else
        {
            // randomly choose if we split horizontally
            splitHorizontally = Random.Range(0, 100) > 50;
        }

        // dont split if the room is going to be too small
        if (Mathf.Min(rect.height, rect.width) / 2 < minRoomSize)
        {
            return false;
        }

        // if we ARE splitting horizontally
        if (splitHorizontally)
        {
            // split so that the resulting sub-dungeons widths are not too small
            int split = Random.Range(minRoomSize, (int)(rect.width - minRoomSize));

            // create two new rooms in the resulting sub dungeons
            left = new SubDungeon(new Rect(rect.x, rect.y, rect.width, split));
            right = new SubDungeon(new Rect(rect.x, rect.y + split, rect.width, rect.height - split));
        }
        // we are splitting vertically
        else
        {
            int split = Random.Range(minRoomSize, (int)(rect.height - minRoomSize));

            // create two new rooms in the resulting sub dungeons
            left = new SubDungeon(new Rect(rect.x, rect.y, split, rect.height));
            right = new SubDungeon(new Rect(rect.x + split, rect.y, rect.width - split, rect.height));
        }

        return true;
    }

    // creates the rooms
    public void CreateRoom()
    {
        // if there is a left sub dungeon, create a room
        if (left != null)
        {
            left.CreateRoom();
        }
        // if there is a right sub dungeon, create a room
        if (right != null)
        {
            right.CreateRoom();
        }
        // if the left and right sub dungeons aren't empty, create a corridor between them
        if (left != null && right != null)
        {
            // create a corridor connecting two sister nodes
            CreateCorridorBetween(left, right);
        }
        // if it has no child nodes, set up the room
        if (IsLeaf())
        {
            // store the rooms dimensions and position
            int roomWidth = (int)Random.Range(rect.width / 2, rect.width - 2);
            int roomHeight = (int)Random.Range(rect.height / 2, rect.height - 2);
            int roomX = (int)Random.Range(1, rect.width - roomWidth - 1);
            int roomY = (int)Random.Range(1, rect.height - roomHeight - 1);

            // room position will be reletive to the dungeon, not relative to the sub-dungeon
            room = new Rect(rect.x + roomX, rect.y + roomY, roomWidth, roomHeight);
        }
    }

    // creates a corridor, connecting two rooms together
    public void CreateCorridorBetween(SubDungeon left, SubDungeon right)
    {
        //get the rooms to be connected
        Rect leftRoom = left.GetRoom();
        Rect rightRoom = right.GetRoom();

        // create the corridor at a random point in each room
        Vector2 leftPosition = new Vector2((int)Random.Range(leftRoom.x + 1, leftRoom.xMax - 1), (int)Random.Range(leftRoom.y + 1, leftRoom.yMax - 1));
        Vector2 rightPosition = new Vector2((int)Random.Range(rightRoom.x + 1, rightRoom.xMax - 1), (int)Random.Range(rightRoom.y + 1, rightRoom.yMax - 1));

        // if the left point is greater than the right point
        // swap them to make the code more understandable
        if (leftPosition.x > rightPosition.x)
        {
            Vector2 temp = leftPosition;
            leftPosition = rightPosition;
            rightPosition = temp;
        }

        // checks how well aligned the two vector positions are
        int horizontalAlignment = (int)(leftPosition.x - rightPosition.x);
        int verticalAlignment = (int)(leftPosition.y - rightPosition.y);

        // if the points are not aligned horizontally
        if (horizontalAlignment != 0)
        {
            // randomly choose to go horizontal then vertical or the opposite
            if (Random.Range(0, 1) > 2)
            {
                // add a corridor to the right
                corridors.Add(new Rect(leftPosition.x, leftPosition.y, Mathf.Abs(horizontalAlignment) + 1, 1));

                // if left point is below right point go up
                if (verticalAlignment < 0)
                {
                    corridors.Add(new Rect(rightPosition.x, leftPosition.y, 1, Mathf.Abs(verticalAlignment)));
                }
                // otherwise go down
                else
                {
                    corridors.Add(new Rect(rightPosition.x, leftPosition.y, 1, -Mathf.Abs(verticalAlignment)));
                }
            }
            else
            {
                // go up 
                if (verticalAlignment < 0)
                {
                    corridors.Add(new Rect(leftPosition.x, leftPosition.y, 1, Mathf.Abs(verticalAlignment)));
                }
                // or down
                else
                {
                    corridors.Add(new Rect(leftPosition.x, rightPosition.y, 1, Mathf.Abs(verticalAlignment)));
                }

                // then go right
                corridors.Add(new Rect(leftPosition.x, rightPosition.y, Mathf.Abs(horizontalAlignment) + 1, 1));
            }
        }
        else
        {
            // if the vectors are aligned horizontally
            // go up or down depending on the positions
            if (verticalAlignment < 0)
            {
                corridors.Add(new Rect((int)leftPosition.x, (int)leftPosition.y, 1, Mathf.Abs(verticalAlignment)));
            }
            else
            {
                corridors.Add(new Rect((int)rightPosition.x, (int)rightPosition.y, 1, Mathf.Abs(verticalAlignment)));
            }
        }
    }

    // get the room/rooms
    public Rect GetRoom()
    {
        // if the node has no children then return the room
        if (IsLeaf())
        {
            return room;
        }

        // if the node has child nodes then get the them
        if (left != null)
        {
            Rect leftRoom = left.GetRoom();
            if (leftRoom.x != -1)
            {
                return leftRoom;
            }
        }
        if (right != null)
        {
            Rect rightRoom = right.GetRoom();
            if (rightRoom.x != -1)
            {
                return rightRoom;
            }
        }

        // workaround non nullable structs
        return new Rect(-1, -1, 0, 0);
    }
}
