

/**********************************************************************************************

    Luke O Brien - P11011180
    4th Year Project - Procedural Generation of Dungeons
    Menus.cs

************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menus : MonoBehaviour
{
     
    // input fields
    public InputField widthInput;
    public InputField heightInput;
   
    // toggle switches
    public Toggle csvInput;
    public Toggle wfcToggle;
    public Toggle caToggle;
    public Toggle bspToggle;
    string tempWidth;
    string tempHeight;

    // booleans for the toggle switches
    bool wfc = true, ca = false, bsp = false;
    bool csv = true;

    // Start is called before the first frame update
    void Start()
    {
        // giving the user access to the mouse cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // setting the preset values for the input fields
        widthInput.text = "80";
        heightInput.text = "80";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Goes to the next scene
    public void Next()  
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // is called when the generate button is activated
    public void Generate()
    {
        int width, height, csvInt;  
        
        // error check for the input field values
        if(widthInput.text != null && heightInput.text != null)
        {
            // converting the values to int's
            width = int.Parse(tempWidth);
            height = int.Parse(tempHeight);

            // if the user has ticked the export csv box
            if (csv)
            {
                csvInt = 1;
            }
            else
            {
                csvInt = 0;
            }

            // if the inputted dungeon size doesn't fall inside the range, then use default values
            if ((width <= 5 && height <= 5) || (width > 100 && height > 100))
            {
                width = 80;
                height = 80;
            }

            // setting the persistant values
            PlayerPrefs.SetInt("ppWidth", width);
            PlayerPrefs.SetInt("ppHeight", height);
            PlayerPrefs.SetInt("ppCsv", csvInt);
            
            //go to the next scene
            NextScene();
        }
        else
        {
            Debug.Log("Please Enter a Dungeon Width & Height");
        }

    }

    // set the width
    public void SetWidth()
    {
        tempWidth = widthInput.text;
    }

    // set the height
    public void SetHeight()
    {
        tempHeight = heightInput.text;
    }

    // set the selected algorithm choice
    public void SetAlgorithm()
    {
        wfc = wfcToggle.isOn;
        ca = caToggle.isOn;
        bsp = bspToggle.isOn;
        
    }

    // set the export option
    public void SetExports()
    {
        csv = csvInput.isOn;

    }

    // go to the next scene
    private void NextScene()
    {
        if(wfc)
        {
            SceneManager.LoadScene("WaveFunctionCollapse");
        }
        else if(ca)
        {
            SceneManager.LoadScene("CellularAutomata");
        }
        else
        {
            SceneManager.LoadScene("BinarySpacePartition");
        }
    }

    // exit the application
    public void Quit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
