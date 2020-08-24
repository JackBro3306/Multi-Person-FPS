using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour {

    [HideInInspector]
    public Animator curAnimator;
    [HideInInspector]
    public Action Reload1Cb;
    [HideInInspector]
    public Action Reload2Cb;

    Fps_PlayerParamter paramter;
    CharacterController characterCtrl;
    PlayerController playerCtrl;

    float net_h = 0;
    float net_v = 0;
    PlayerState net_state;
    void Start() {
        paramter = GetComponent<Fps_PlayerParamter>();
        characterCtrl = GetComponent<CharacterController>();
        curAnimator = GetComponent<Animator>();
        playerCtrl = GetComponent<PlayerController>();
    }


    void Update() {

        if (curAnimator == null) return;
        if (playerCtrl.ctrlType == PlayerController.CtrlType.None) return;
        //if (Mathf.Abs(paramter.inputMoveVector.x) > 0 || Mathf.Abs(paramter.inputMoveVector.y) > 0) {
        //    curAnimator.SetBool("Walking", true);
        //    if (paramter.inputMoveVector.y > 0 && paramter.inputSprint) {
        //        curAnimator.SetBool("Walking", false);
        //        curAnimator.SetBool("Runing", true);

        //        //if (paramter.inputJump) {
        //        //    curAnimator.SetTrigger("Jump");
        //        //}

        //    } else {
        //        curAnimator.SetBool("Runing", false);
        //    }

        //    curAnimator.SetFloat("Horzontal", paramter.inputMoveVector.x);
        //    curAnimator.SetFloat("Vertical", paramter.inputMoveVector.y);
        //} else {
        //    curAnimator.SetBool("Walking", false);

        //}
        float h = 0;
        float v = 0;
        PlayerState state = PlayerState.Idle;
        if (playerCtrl.ctrlType== PlayerController.CtrlType.Player) {
            state = playerCtrl.State;
            h = paramter.inputMoveVector.x;
            v = paramter.inputMoveVector.y;
        } else if (playerCtrl.ctrlType == PlayerController.CtrlType.Net) {
            state = net_state;
            h = net_h;
            v = net_v;
        }

        switch (state) {
            case PlayerState.None:
                curAnimator.SetBool("Walking", false);
                curAnimator.SetBool("Runing", false);
                break;
            case PlayerState.Idle:
                curAnimator.SetBool("Walking", false);
                curAnimator.SetBool("Runing", false);
                break;
            case PlayerState.Walk:
                curAnimator.SetBool("Walking", true);
                curAnimator.SetBool("Runing", false);
                break;
            case PlayerState.Crouch:
                curAnimator.SetBool("Walking", true);
                curAnimator.SetBool("Runing", false);
                break;
            case PlayerState.Run:
                curAnimator.SetBool("Walking", false);
                curAnimator.SetBool("Runing", true);
                break;
        }
        curAnimator.SetFloat("Horzontal", h);
        curAnimator.SetFloat("Vertical", v);


        //if (paramter.inputJump
        //        && characterCtrl.isGrounded
        //        && !paramter.inputAim) {
        //    curAnimator.SetTrigger("Jump");
        //}

    }

    public void NetMoveVec(float h,float v,PlayerState s) {
        net_h = h;
        net_v = v;
        net_state = s;
    }

    public void Reload1() {
        if (Reload1Cb != null)
            Reload1Cb();
    }

    public void Reload2() {
        if (Reload2Cb != null)
            Reload2Cb();
    }

    public void Death() {
        Vector3 temp = transform.position;
        transform.position = new Vector3(temp.x, temp.y-0.5f, temp.z);
        curAnimator.SetLayerWeight(1, 0);
        curAnimator.SetTrigger("Death");
    }
}
