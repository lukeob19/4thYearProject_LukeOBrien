

/**********************************************************************************************

    Luke O Brien - P11011180
    4th Year Project - Procedural Generation of Dungeons
    CellularAutomataGenerator.cs

************************************************************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

public class CellularAutomataGenerator : MonoBehaviour
{
    // characters for the random seed
    private const string seedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    // variables to store the size of the dungeon
    // set them in the editor
    public int width;
    public int height;
    public bool useEditorValues = false;
    [Range(35, 60)] // creates a scrollbar in the editor
    public int randomFillPercent;  // the amount of the dungeon that is going to be randomly filled at the start 

    int[,] dungeon; // representing the dungeon as a 2D array
    string seed;
    
    // runs when the object is created
    private void Start()
    {
        // a check to see if i want to use values in the editor instead
        if (!useEditorValues)
        {
            //retrieving the dungeon size
            width = PlayerPrefs.GetInt("ppWidth");
            height = PlayerPrefs.GetInt("ppHeight");
        }
        GenerateDungeon(); // start generating the dungeon
   
    }

    // generates a new dungeon when left-click is pressed
    private void Update()   
    {
        if(Input.GetMouseButtonDown(0))
        {
            GenerateDungeon();
        }
    }

    // this is responsible for the generation process
    void GenerateDungeon()
    {
        dungeon = new int[width, height];   // setting the width and height of the dungeon
        RandomlyFillDungeon();  // filling the dungeon grid

        // how many times we want to smooth the dungeon
        // the number of times this loops will effect the outputted dungeon
        for (int i = 0; i < 5; i++)
        {
            SmoothDungeon();
        }

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

        // create a MeshGenerator and generate a mesh based on the 2d array
        MeshGenerator gen = GetComponent<MeshGenerator>();
        gen.GenerateMesh(borderedDungeon, 1);

        // pass the dungeon to be exported as a csv file
        FindObjectOfType<GameManager>().ExportDungeonData(borderedDungeon);
    }

    // randomly fills each position in the dungeon
    void RandomlyFillDungeon()
    {
        // getting a unique seed to generate random numbers
        seed = GenerateSeed();  
        System.Random rng = new System.Random(seed.GetHashCode());  

        // iterate through the dungeon array
        for(int column = 0; column < width; column++)  
        {
            for(int row = 0; row < height; row++)
            {
                // we always want the outside edges of the dungeon to be walls. to stop the player from
                // being able to leave the play area
                if((column == 0) || (column == width - 1) || (row == 0) || (row == height - 1))
                {
                    dungeon[column, row] = 1;
                }

                // generate a random number between 0 and 100. if the number is below the fill percent
                // the n the position in the array is set to 1 otherwise its set to 0
                else if(rng.Next(0, 100) < randomFillPercent)    
                {
                    dungeon[column, row] = 1;
                }
                else
                {
                    dungeon[column, row] = 0;
                }
            }
        }
    }

    // a function that returns a randomly generated seed
    private string GenerateSeed()
    {
        string seed = string.Empty; // create an empty string 
        int seedLength = 8; // the length of the seed string

        // appending random characters from the seedCharacters string until the seed is full
        for (int i = 0; i < seedLength; i++)
        {
            seed += seedCharacters[UnityEngine.Random.Range(0, seedCharacters.Length)];
        }

        return seed;    //  return the seed
    }


    // This is the function that is responsible for shaping the dungeon
    // It looks at each tile and decides whether it needs to change based on the neighbouring tiles.
    void SmoothDungeon()
    {
        for (int column = 0; column < width; column++)
        {
            for (int row = 0; row < height; row++)
            {
                // get the neighbouring wall count
                int neighbouringWallCount = GetSurroundingWallCount(column, row); 

                //smooth dungeon
                if(neighbouringWallCount > 4)
                {
                    dungeon[column, row] = 1;
                }
                else if(neighbouringWallCount < 4)
                {
                    dungeon[column, row] = 0;
                }
            }
        }
    }

    // loops through a 3x3 grid of tiles centered around the input tile
    // and counts the number of tiles that are walls
    int GetSurroundingWallCount(int tileX, int tileY)
    {
        int wallCount = 0;      // somewhere to store the wall count

        // iterate through a 3x3 grid
        for (int neighbourX = tileX - 1;neighbourX <= tileX +1; neighbourX++)
        {
            for (int neighbourY = tileY - 1; neighbourY <= tileY + 1; neighbourY++)
            {
                // making sure that we aren't at an edge tile of the dungeon
                if (neighbourX >= 0 && neighbourY >= 0 && neighbourX < width && neighbourY < height)   
                {
                    // making sure that we skip the center tile
                    if (neighbourX != tileX || neighbourY != tileY) 
                    {
                        // counting
                        wallCount += dungeon[neighbourX, neighbourY]; 
                    }
                }
                else
                {
                    // if the tile is at the edge of the dungeon, increment
                    wallCount++;  
                }
            }
        }
        // return counted walls
        return wallCount; 
    }

    // This functions job is to use the gizmos in the editor to display a representation of the dungeon 2D array
    //private void OnDrawGizmos()
    //{
    //    if(dungeon != null) // if the dungeon array was filled
    //    {
    //        for (int column = 0; column < width; column++)
    //        {
    //            for (int row = 0; row < height; row++)
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
