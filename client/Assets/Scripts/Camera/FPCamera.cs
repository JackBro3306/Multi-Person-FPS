using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FPCamera : MonoBehaviour {
    // 摄像机跟随的点
    private Transform CameraFollowPoint;
    // 距离
    public float distance = 0.01f;
    // 垂直方向旋转的上半身
    private Transform upperBDTran;
    // 人物全身
    private Transform bodyTran;
    // x轴（水平）速度
    public float sensitivityX = 15f;
    // y轴（垂直）速度
    public float sensitivityY = 15f;
    // x轴（水平）最小旋转值
    public float minX = -360f;
    // x轴（水平）最大旋转值
    public float maxX = 360f;
    // y轴（垂直）最小旋转值
    public float minY = -60f;
    // y轴（垂直）最大旋转值
    public float maxY = 60f;

    private float rotationX = 0;
    private float rotationY = 0f;

    private GameObject targetGo;
    private Fps_PlayerParamter paramter;
    private Fps_Player player;

    void Start () {
        //targetGo = GameObject.Find("Player1");
        //SetTarget(targetGo);

    }
	
	
	void Update () {
        
        if (targetGo) {
            ShootUp();
        }
    }

    private void LateUpdate() {
        
        if (targetGo) {
            
            PlayerCtrlRotate();
        }
        
    }

    private void PlayerCtrlRotate() {
        if (player.playerCtrl.ctrlType != PlayerController.CtrlType.Player) return;
        RotatePlayer();
        // 摄像机
        RotateCamera(-rotationY, rotationX);
    }

    // rotateX,rotationY,0
    public Vector3 GetRotate() {
        return new Vector3(bodyTran.localEulerAngles.y, upperBDTran.localEulerAngles.y,0);
    }

    // 初始化跟随
    public void InitFollow(Transform cf,Transform upper,Transform body) {
        CameraFollowPoint = cf;
        upperBDTran = upper;
        bodyTran = body;
    }
    
    // 旋转人物
    void RotatePlayer() {
        rotationX = angleX + horCurrent;
        rotationY = Mathf.Clamp((angleY + verCurrent), minY, maxY);
        // 人物水平方向
        bodyTran.localEulerAngles = new Vector3(bodyTran.localEulerAngles.x, rotationX, bodyTran.localEulerAngles.z);
        // 人物垂直方向
        upperBDTran.localEulerAngles += new Vector3(0, -rotationY, 0);
    }

    // 旋转摄像机
    void RotateCamera(float x=0,float y=0,float z=0) {
        // 摄像机
        Quaternion q = Quaternion.Euler(x, y, z);
        Vector3 direction = q * Vector3.forward;
        Vector3 tempVec = CameraFollowPoint.position - direction * distance;
        transform.position = tempVec;
        transform.LookAt(CameraFollowPoint.position);
    }

    private float mouseX;
    private float mouseY;
    private float angleX;
    private float angleY;
    //后坐力系统
    private float horCurrent;       //枪口位置
    private float verCurrent;
    private float verTotalOff;        //偏移量
    private float horTotalOff;
    private bool isFire = false;
    private float timeToPos = 0;

    private void ShootUp() {
        if (player.playerCtrl.ctrlType != PlayerController.CtrlType.Player) return;
        mouseX = paramter.inputSmoothLook.x;
        mouseY = paramter.inputSmoothLook.y;

        angleX += mouseX * sensitivityX * Time.deltaTime * 10;

        //压枪
        if (verTotalOff > 0 && mouseY < 0) {
            verTotalOff += mouseY * sensitivityY * Time.deltaTime * 10;
        } else {
            angleY += mouseY * sensitivityY * Time.deltaTime * 10;
        }


        if (isFire) {       //准星上调

            horCurrent = Mathf.Lerp(horCurrent, horTotalOff, Time.deltaTime * 10);
            verCurrent = Mathf.Lerp(verCurrent, verTotalOff, Time.deltaTime * 10);

            timeToPos -= Time.deltaTime;
            if (timeToPos < 0) {
                isFire = false;

            }
        } else {    //准星归位
            if (Mathf.Abs(verCurrent) > 0.1f) {
                verCurrent = Mathf.Lerp(verCurrent, 0, Time.deltaTime * 10);
                verTotalOff = verCurrent;
            } else {
                verCurrent = 0;
                verTotalOff = 0;
            }

            if (Mathf.Abs(horCurrent) > 0.1f) {
                horCurrent = Mathf.Lerp(horCurrent, 0, Time.deltaTime * 10);
                horTotalOff = horCurrent;
            } else {
                horCurrent = 0;
                horTotalOff = 0;
            }

        }
    }

    public void ShootOffSet(float horOff, float verOff, float time) {
        horTotalOff += horOff;
        verTotalOff += verOff;

        timeToPos = time;
        isFire = true;
    }

    public void SetTarget(GameObject go) {
        targetGo = go;
        if (targetGo) {
            player = targetGo.GetComponent<Fps_Player>(); ;
            // 确保刚体不改变旋转
            if (targetGo.GetComponent<Rigidbody>())
                targetGo.GetComponent<Rigidbody>().freezeRotation = true;
            CameraFollowPoint = player.playerCtrl.CameraFollowPoint;
            upperBDTran = player.playerCtrl.upperBDTran;
            bodyTran = upperBDTran.root;
            paramter = targetGo.GetComponent<Fps_PlayerParamter>();
        }
    }

    // 设置死亡时摄像机位置

    public void DeathView() {
        transform.position += new Vector3(0,5,-5);
        transform.LookAt(bodyTran.position);
    }
}
