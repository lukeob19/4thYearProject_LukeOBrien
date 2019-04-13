

/**********************************************************************************************

    Luke O Brien - P11011180
    4th Year Project - Procedural Generation of Dungeons
    PlayerController.cs
    edited version from my GamesFleadh entry

************************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    //public variables
    public float moveSpeed;
    public float jumpForce;
    public float gravityScale;

    public CharacterController controller;
    public Animator anim;
    public Transform pivot;
    public float cameraRotateSpeed;
    public GameObject playerModel;

    public bool isRespawning = false;
    //private variables 
    private Vector3 moveDirection;


    // Use this for initialization
    void Start()
    {
        // when the player is spawned, this line will get the players CharacterController component
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {


        // setting the players movement
        float yStore = moveDirection.y;
        moveDirection = (transform.forward * Input.GetAxisRaw("Vertical")) + (transform.right * Input.GetAxisRaw("Horizontal"));
        moveDirection = moveDirection.normalized * moveSpeed;
        moveDirection.y = yStore;

        moveDirection.y = moveDirection.y + (Physics.gravity.y * gravityScale * Time.deltaTime); //applying gravity

        // using time.deltaTime so that the players movement is the same speed at any framerate
        controller.Move(moveDirection * Time.deltaTime);

        //basing the players movement on the cameras rotation
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            transform.rotation = Quaternion.Euler(0f, pivot.rotation.eulerAngles.y, 0f);

            //stopping the player model from snapping to a rotation using Slerp
            Quaternion newPlayerRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0f, moveDirection.z));
            playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, newPlayerRotation, cameraRotateSpeed * Time.deltaTime);
        }

        

        anim.SetBool("isGrounded", controller.isGrounded);
        anim.SetFloat("speed", (Mathf.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal"))));

    }

    // setting the players spawn point
    public void SetSpawn(Vector3 newSpawn)
    {
        transform.position = newSpawn;
    }
}
