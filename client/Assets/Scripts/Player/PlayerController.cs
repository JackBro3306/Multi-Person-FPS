using LSGameServ.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState {
    None,
    Idle,
    Walk,
    Crouch,
    Run
}

public class PlayerController : MonoBehaviour {
    // 操控类型
    public enum CtrlType {
        None,
        Player,
        Net
    }
    // 默认操控类型为玩家
    public CtrlType ctrlType = CtrlType.Player;
    [Header("CameraFollow")]
    // 摄像机跟随的点
    public Transform CameraFollowPoint;
    // 垂直方向旋转的上半身
    public Transform upperBDTran;
    private Transform bodyTran;

    private PlayerState state = PlayerState.None;
    public PlayerState State {
        get {
            if (runing)
                state = PlayerState.Run;
            else if (walking)
                state = PlayerState.Walk;
            else if (crouching)
                state = PlayerState.Crouch;
            else
                state = PlayerState.Idle;

            return state;
        }
    }

    public float sprintSpeed = 10f;
    public float sprintJumpSpeed = 8f;
    public float normalSpeed = 6f;
    public float normalJumpSpeed = 7f;
    public float crouchSpeed = 2f;
    public float crouchJumpSpeed = 5;
    public float crouchDeltaHeight = 0.5f;

    public float gravity = 20f;
    //public float cameraMoveSpeed = 8;
    public AudioClip jumpAudio;
    public AudioClip[] moveAudioClips;

    [HideInInspector]
    //public bool isAim = false;

    private float speed;
    private float jumpSpeed;
    private Transform mainCamera;
    private float standardCamHeight;
    private float crouchingCamHeight;
    private bool grounded = false;
    public bool walking = false;
    private bool crouching = false;
    private bool canUp = true;
    private bool stopCrouching = false;
    public bool runing = false;
    private Vector3 normalControllerCenter = Vector3.zero;
    private Vector3 hitNormalCenter = Vector3.zero;
    private float normalControllerHeight = 0.0f;
    private float timer = 0;
    private CharacterController controller;
    private CapsuleCollider hitCollider;
    private AudioSource audioSource;
    private Fps_PlayerParamter paramter;
    private Vector3 moveDirection = Vector3.zero;
    private int audioClipIndex = 0;

    
    private Fps_Player player;
    #region 网络部分参数
    private float lastSendInfoTime;
    // last上次的位置信息
    Vector3 lPos;
    Vector3 lRot;

    // forecast预测的位置信息
    Vector3 fPos;
    Vector3 fRot;
    // 时间间隔
    float delta = 1;
    // 上次接收的时间
    float lastRecvInfoTime = float.MinValue;

    #endregion
    private void Awake() {
        mainCamera = Camera.main.transform;
        bodyTran = upperBDTran.root;
        audioSource = GetComponent<AudioSource>();
        controller = GetComponent<CharacterController>();
        hitCollider = GetComponent<CapsuleCollider>();
        paramter = GetComponent<Fps_PlayerParamter>();
        
    }
    void Start() {
        crouching = false;
        walking = false;
        runing = false;

        speed = normalSpeed;
        jumpSpeed = normalJumpSpeed;

        standardCamHeight = mainCamera.localPosition.y;
        crouchingCamHeight = standardCamHeight - crouchDeltaHeight;
        
        normalControllerCenter = controller.center;
        normalControllerHeight = controller.height;
        hitNormalCenter = hitCollider.center;
        // CharacterController只有在Move或者是SimpleMove才检测碰撞
        // CC不检测物理碰撞，使用新的碰撞器检测碰撞
        controller.detectCollisions = false;
        player = GetComponent<Fps_Player>();
    }

    void FixedUpdate() {
        UpdateMove();
        AudioManagement();
    }

    private void Update() {
        if(ctrlType == CtrlType.Net) {
            NetUpdate();
            return;
        }
        SendUnit();
        
    }

    private void LateUpdate() {
        if (ctrlType == CtrlType.Net) {
            // 人物的旋转
            NetUpdateRot();
            return;
        }
    }

    private void CurrentSpeed() {
        switch (State) {

            case PlayerState.Idle:
                speed = normalSpeed;
                jumpSpeed = normalJumpSpeed;
                break;
            case PlayerState.Walk:
                //speed = isAim ? normalSpeed / 2 : normalSpeed;
                speed =  normalSpeed;
                jumpSpeed = normalJumpSpeed;
                break;
            case PlayerState.Crouch:
                speed = crouchSpeed;
                jumpSpeed = crouchJumpSpeed;
                break;
            case PlayerState.Run:
                speed = sprintSpeed;
                jumpSpeed = sprintJumpSpeed;
                break;
        }
    }

    private void AudioManagement() {
        if (State == PlayerState.Walk) {
            audioSource.pitch = crouching ? 0.6f : 0.8f;

            if (!audioSource.isPlaying) {
                audioSource.clip = moveAudioClips[audioClipIndex];
                audioClipIndex++;
                audioClipIndex %= moveAudioClips.Length;


                audioSource.Play();
            }

        } else if (State == PlayerState.Run) {

            audioSource.pitch = 1f;

            if (!audioSource.isPlaying) {
                audioSource.clip = moveAudioClips[audioClipIndex];

                audioClipIndex++;
                audioClipIndex %= moveAudioClips.Length;

                audioSource.Play();
            }
        } else {
            audioSource.Stop();
        }
    }

    private void UpdateMove() {
        if (ctrlType != CtrlType.Player)
            return;
        if (grounded) {
            //获得相对于玩家坐标系的方向
            moveDirection = new Vector3(paramter.inputMoveVector.x, 0, paramter.inputMoveVector.y);
            //转化成相对于世界坐标系的方向
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
            if (paramter.inputJump) {
                moveDirection.y = jumpSpeed;
                //AudioSource.PlayClipAtPoint(jumpAudio, transform.position);
                CurrentSpeed();
            }
        }

        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
        grounded = controller.isGrounded;

        if (Mathf.Abs(moveDirection.x) > 0 && grounded || Mathf.Abs(moveDirection.z) > 0 && grounded) {
            if (paramter.inputMoveVector.y > 0 && paramter.inputSprint) {
                walking = false;
                runing = true;
                crouching = false;
            } else if (paramter.inputCrouch) {
                walking = true;
                runing = false;
                crouching = true;
            } else {
                walking = true;
                runing = false;
                crouching = false;
            }
        } else {
            if (walking)
                walking = false;
            if (runing)
                runing = false;
            if (paramter.inputCrouch)
                crouching = true;
            else
                crouching = false;
        }


        UpdateCrouch();
        CurrentSpeed();
    }

    private void UpdateCrouch() {
        if (crouching) {
            //// 调整摄像机高度
            //if (mainCamera.localPosition.y > crouchingCamHeight) {
            //    //相机往下降
            //    if (mainCamera.localPosition.y - (crouchDeltaHeight * Time.deltaTime * cameraMoveSpeed) < crouchingCamHeight)
            //        mainCamera.localPosition = new Vector3(mainCamera.localPosition.x, crouchingCamHeight, mainCamera.localPosition.z);
            //    else
            //        mainCamera.localPosition -= new Vector3(0, crouchDeltaHeight * Time.deltaTime * cameraMoveSpeed, 0);
            //} else
            //    mainCamera.localPosition = new Vector3(mainCamera.localPosition.x, crouchingCamHeight, mainCamera.localPosition.z);

            //调整CharacterController的高度
            controller.height = normalControllerHeight - crouchDeltaHeight;
            hitCollider.height = normalControllerHeight - crouchDeltaHeight;
            controller.center = normalControllerCenter - new Vector3(0, crouchDeltaHeight / 2, 0);
            hitCollider.center = normalControllerCenter - new Vector3(0, crouchDeltaHeight / 2, 0);


        } else {
            //判断头顶是否有障碍物
            Vector3 origin = transform.position;
            origin.y = crouchingCamHeight;
            Ray ray = new Ray(origin, Vector3.up);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, standardCamHeight - crouchingCamHeight)) {
                canUp = false;
            } else {
                canUp = true;
            }

            if (canUp) {
                //// 调整摄像机高度
                //if (mainCamera.localPosition.y < standardCamHeight) {
                //    //相机往上升
                //    if (mainCamera.localPosition.y - (crouchDeltaHeight * Time.deltaTime * cameraMoveSpeed) > standardCamHeight)
                //        mainCamera.localPosition = new Vector3(mainCamera.localPosition.x, crouchingCamHeight, mainCamera.localPosition.z);
                //    else
                //        mainCamera.localPosition += new Vector3(0, crouchDeltaHeight * Time.deltaTime * cameraMoveSpeed, 0);
                //} else
                //    mainCamera.localPosition = new Vector3(mainCamera.localPosition.x, standardCamHeight, mainCamera.localPosition.z);

                controller.height = normalControllerHeight;
                hitCollider.height = normalControllerHeight;
                controller.center = normalControllerCenter;
                hitCollider.center = hitNormalCenter;
            }



        }
    }

    private Vector3 GetRotate() {
        return new Vector3(bodyTran.localEulerAngles.y, upperBDTran.localEulerAngles.y, 0);
    }

    #region 网络部分
    // 初始化位置数据
    public void InitNetCtrl() {
        // 初始化摄像机跟随
        lPos = transform.position;
        // x代表水平旋转，y代表垂直旋转
        Vector3 tempVec = GetRotate();
        lRot = tempVec;
        fPos = transform.position;
        fRot = tempVec;
        finalRot = Vector3.zero;
    }
    // 发送同步信息到服务器
    private void SendUnit() {
        if (ctrlType != CtrlType.Player)
            return;
        // 100毫秒发送一次位置信息
        if (Time.time - lastSendInfoTime > 0.1f) {
            SendUnitInfo();
            lastSendInfoTime = Time.time;
        }
    }

    private void SendUnitInfo() {
        GameMessage message = new GameMessage();
        message.type = System.BitConverter.GetBytes((int)Protocol.UpdateUnitInfo);
        UnitInfo unitInfo = new UnitInfo();
        unitInfo.id = GameMgr._instance.id;
        Vector3 pos = transform.position;
        unitInfo.posX = pos.x;
        unitInfo.posY = pos.y;
        unitInfo.posZ = pos.z;

        Vector3 rot = player.fPCamera.GetRotate();
        unitInfo.rotateX = rot.x;
        unitInfo.rotateY = rot.y;
        unitInfo.rotateZ = rot.z;

        unitInfo.input_h = paramter.inputMoveVector.x;
        unitInfo.input_v = paramter.inputMoveVector.y;
        unitInfo.moveState = (uint)State;
        message.data = ProtoTransfer.Serialize(unitInfo);
        NetMgr.GetInstance().tcpSock.Send(message);
    }
    /// <summary>
    /// 位置预测
    /// </summary>
    /// lPos，lRot：上次的位置和旋转同步信息
    /// nPos，nRot：本次收到的位置和旋转信息
    /// 
    public void NetForecastInfo(Vector3 nPos,Vector3 nRot) {
        // 预测的位置
        fPos = lPos + (nPos - lPos) * 2;
        fRot = lRot + (nRot - lRot) * 2;
        // 若出现异常的网络延迟
        if (Time.time - lastSendInfoTime > 0.3f) {
            fPos = nPos;
            fRot = nRot;
        }

        // 时间
        delta = Time.time - lastRecvInfoTime;
        // 更新
        lPos = nPos;
        lRot = nRot;
        lastRecvInfoTime = Time.time;
    }
    // 一些状态的同步
    public void NetMoveState(PlayerState state,float h,float v,PlayerState s) {
        if (player == null)
            player = GetComponent<Fps_Player>();

        player.playerAnim.NetMoveVec(h,v,s);
    }
    // 处理玩家的位置同步
    public void NetUpdate() {
        // 当前位置
        Vector3 pos = transform.position;
        // 获得自身旋转值
        Vector3 rot = GetRotate();
        // 更新位置
        if(delta > 0) {
            transform.position = Vector3.Lerp(pos,fPos,delta);
            finalRot = Vector3.Lerp(rot, fRot, delta*2);
            if (Mathf.Abs(finalRot.x - fRot.x) < 0.001f
                || Mathf.Abs(finalRot.y - fRot.y) < 0.001f
                || Mathf.Abs(finalRot.z - fRot.z) < 0.001f)
                finalRot = fRot;
            
        }
        
    }

    Vector3 finalRot;
    private void NetUpdateRot() {
        if (delta > 0) {
            // 人物水平方向
            bodyTran.localEulerAngles = new Vector3(bodyTran.localEulerAngles.x, finalRot.x, bodyTran.localEulerAngles.z);
            // 人物垂直方向，需要在latedUpdate中才能生效
            upperBDTran.localEulerAngles = new Vector3(upperBDTran.localEulerAngles.x, finalRot.y, upperBDTran.localEulerAngles.z);
        }
    }
    
    #endregion
}
