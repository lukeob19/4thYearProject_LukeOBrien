

/**********************************************************************************************

    Luke O Brien - P11011180
    4th Year Project - Procedural Generation of Dungeons
    GameManager.cs

************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class GameManager : MonoBehaviour
{
    // declare variables
    int csv;
    public Material floorMat;
    public GameObject level;
    public TextMeshProUGUI fileText;
    bool activated = false;

    // Start is called before the first frame update
    void Start()
    {
        // hide and lock the mouse cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        csv = PlayerPrefs.GetInt("ppCsv");  //get export option

        // create the dungeons floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.position = new Vector3(0, -4, 0);
        floor.transform.localScale = new Vector3(40, 0, 40);
        floor.GetComponent<MeshRenderer>().material = floorMat;
        floor.transform.parent = level.transform;
    }

    // Update is called once per frame
    void Update()
    {
       // if the key 'o' is pressed, go to the options menu
       if(Input.GetKeyDown("o"))
        {
            SceneManager.LoadScene("OptionsMenu");
        }
    }

    // create a .csv file that represents the 2d array of the dungeon
    // this allows dungeons generated to be used in other games
    public void ExportDungeonData(int[,] dungeon)
    {
        // if the user chose to export the data
        if(csv == 1)
        {
            //setting up the file path and filename of the soon to be exported .csv file
            Scene currentScene = SceneManager.GetActiveScene();
            string algorithm = currentScene.name;
            string path = Application.dataPath;
            int number = (int)UnityEngine.Random.Range(100, 1000000);

            // create a text file and iterate through the dungeon array while writing the data to the file
            using (StreamWriter writer = new StreamWriter($"{path}/{algorithm}CSV_{number}.txt"))
            {
                for (int column = 0; column < dungeon.GetLength(0); column++)
                {
                    for (int row = 0; row < dungeon.GetLength(1); row++)
                    {
                        writer.Write(dungeon[column, row] + ", ");
                    }
                    writer.Write("\n");
                }

                // freeing the memory used during the file output process
                writer.Flush();
                writer.Close();
            }
        }
    }
}
