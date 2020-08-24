using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fps_Input : MonoBehaviour
{
    public class Fps_InputAxis {
        public KeyCode positive;
        public KeyCode negative;
    }

    public Dictionary<string, KeyCode> buttons = new Dictionary<string, KeyCode>();

    public Dictionary<string, Fps_InputAxis> axis = new Dictionary<string, Fps_InputAxis>();

    public List<string> unityAxis = new List<string>();

    void Start() {
        SetupDefaults();
    }

    private void SetupDefaults(string type="") {
        if(type == "" || type == "buttons") {
            if(buttons.Count == 0) {
                AddButton("Fire",KeyCode.Mouse0);
                AddButton("Aim",KeyCode.Mouse1);
                AddButton("Reload", KeyCode.R);
                AddButton("Jump", KeyCode.Space);
                AddButton("Crouch", KeyCode.C);
                AddButton("Sprint",KeyCode.LeftShift);
                AddButton("Menu", KeyCode.Tab);
                AddButton("PickUp",KeyCode.F);
            }
        }

        if (type == "" || type == "Axis") {
            if (axis.Count == 0) {
                AddAxis("Horzontal",KeyCode.W,KeyCode.S);
                AddAxis("Vertical", KeyCode.A, KeyCode.D);
            }
        }

        if (type == "" || type == "UnityAxis") {
            if (unityAxis.Count == 0) {
                AddUnityAxis("Mouse X");
                AddUnityAxis("Mouse Y");
                AddUnityAxis("Horizontal");
                AddUnityAxis("Vertical");
            }
        }
    }

    private void AddButton(string n,KeyCode k) {
        if (buttons.ContainsKey(n))
            buttons[n] = k;
        else
            buttons.Add(n,k);
    }

    private void AddAxis(string n,KeyCode pk,KeyCode nk) {
        if (axis.ContainsKey(n))
            axis[n] = new Fps_InputAxis() { positive = pk, negative = nk };
        else
            axis.Add(n, new Fps_InputAxis() { positive = pk, negative = nk });
    }

    private void AddUnityAxis(string n) {
        if (!unityAxis.Contains(n))
            unityAxis.Add(n);
    }

    public bool GetButton(string button) {
        if (buttons.ContainsKey(button))
            return Input.GetKey(buttons[button]);
        else
            return false;
    }

    public bool GetButtonDown(string button) {
        if (buttons.ContainsKey(button))
            return Input.GetKeyDown(buttons[button]);
        else
            return false;
    }

    public bool GetButtonUp(string button) {
        if (buttons.ContainsKey(button))
            return Input.GetKeyUp(buttons[button]);
        else
            return false;
    }

    public float GetAxis(string axis) {
        if (this.unityAxis.Contains(axis))
            return Input.GetAxis(axis);
        else
            return 0;
    }

    public float GetAxisRaw(string axis) {
        if (this.axis.ContainsKey(axis)) {
            
            if (Input.GetKey(this.axis[axis].positive))
                return 1;
            if (Input.GetKey(this.axis[axis].negative))
                return -1;
            return 0;
        }
        else if (unityAxis.Contains(axis)) {
            return Input.GetAxisRaw(axis);
        } else {
            return 0;
        }
    }
}
