using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fps_FPInput : MonoBehaviour
{

    public bool LockCursor {
        get { return Cursor.lockState == CursorLockMode.Locked ? true : false; }
        set {
            Cursor.visible = value;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }

    private Fps_PlayerParamter paramter;
    private Fps_Input input;

    void Start() {
        // 设置鼠标不可见，并且锁定在游戏中
        Cursor.visible = false;
        Cursor.lockState =  CursorLockMode.Locked ;

        paramter = GetComponent<Fps_PlayerParamter>();
        input = GameObject.FindGameObjectWithTag(Tags.root).GetComponent<Fps_Input>() ;
    }

    void Update() {
        InitialInput();
    }

    private void InitialInput() {
        paramter.inputMoveVector = new Vector2(input.GetAxis("Horizontal"),input.GetAxis("Vertical"));
        paramter.inputSmoothLook = new Vector2(input.GetAxis("Mouse X"),input.GetAxis("Mouse Y"));
        paramter.inputCrouch = input.GetButton("Crouch");
        paramter.inputJump = input.GetButtonDown("Jump");
        paramter.inputSprint = input.GetButton("Sprint");
        paramter.inputFire = input.GetButton("Fire");
        paramter.inputPickUp = input.GetButton("PickUp");
        if (input.GetButtonDown("Aim")) {
            paramter.inputAim = true;
        }else if (input.GetButtonUp("Aim")) {
            paramter.inputAim = false;
        }

        paramter.inputReload = input.GetButton("Reload");
        paramter.inputMenu = input.GetButton("Menu");
    }
}
