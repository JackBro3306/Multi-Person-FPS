using DG.Tweening;
using LSGameServ.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomListPanel : PanelBase {
    private Text idText;
    private Text winText;
    private Text defeatText;
    private Transform contentTran;
    private GameObject roomPrefab;
    private Button logoutBtn;
    private Button createBtn;
    private Button reflashBtn;
    private GameObject roomUICameraGo;
    Transform listPageTran;
    Transform playerinfoPageTran;
    #region 生命周期
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="args"></param>
    public override void Init(params object[] args) {
        base.Init(args);
        //设置路径
        skinPath = "UIPanel/RoomListPanel";
        //设置层级
        layer = PanelLayer.Panel;
        
    }

    public override void OnShowing() {
        base.OnShowing();

        InitUI();
        listPageTran.localPosition += new Vector3(485,0,0);
        playerinfoPageTran.localPosition += new Vector3(-312, 0, 0);

        playerinfoPageTran.DOLocalMoveX(-388, 0.5f);
        listPageTran.DOLocalMoveX(385, 0.5f).OnComplete(() => {
            logoutBtn.onClick.AddListener(OnLogoutClick);
            createBtn.onClick.AddListener(OnCreateClick);
            reflashBtn.onClick.AddListener(OnReflashClick);
        });
        // 生成
        GameObject roomUICamera = Resources.Load<GameObject>("UIPrefab/RoomUICamera");
        roomUICameraGo = Instantiate(roomUICamera);

        //监听
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.GetAchieve, RecvGetAchieve);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.GetRoomList, RecvGetRoomList);

        //发送查询
        // 查询房间列表
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.GetRoomList);
        message.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(message);

        // 查询玩家信息
        message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.GetAchieve);
        message.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(message);
    }

    void InitUI() {
        //获取Transform
        Transform skinTran = skin.transform;
        listPageTran = skinTran.Find("RoomListPage");
        playerinfoPageTran = skinTran.Find("PlayerInfoPage");

        //获取各个组件
        idText = playerinfoPageTran.Find("IDText").GetComponent<Text>();
        winText = playerinfoPageTran.Find("ScoreImg/WinText/Value").GetComponent<Text>();
        defeatText = playerinfoPageTran.Find("ScoreImg/DefeatText/Value").GetComponent<Text>();

        contentTran = listPageTran.Find("ScrollRect/Content");
        roomPrefab = Resources.Load<GameObject>("UIPrefab/RoomPrefab");
        
        //按钮事件注册
        logoutBtn = listPageTran.Find("LogoutBtn").GetComponent<Button>();
        createBtn = listPageTran.Find("CreateBtn").GetComponent<Button>();
        reflashBtn = listPageTran.Find("RefreshBtn").GetComponent<Button>();
        
    }
    

    public override void OnClosing() {
        // 销毁
        Destroy(roomUICameraGo);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.GetAchieve, RecvGetAchieve);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.GetRoomList, RecvGetRoomList);
    }
    #endregion

    #region 事件注册
    /// <summary>
    /// 点击登出
    /// </summary>
    void OnLogoutClick() {
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.Logout);
        message.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(message, OnLogoutBack);
    }

    /// <summary>
    /// 点击创建房间
    /// </summary>
    void OnCreateClick() {
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.CreateRoom);
        message.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(message, OnCreateBack);
    }

    /// <summary>
    /// 点击刷新
    /// </summary>
    void OnReflashClick() {
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.GetRoomList);
        message.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(message);
    }

    /// <summary>
    /// 点击加入房间
    /// </summary>
    /// <param name="name"></param>
    void OnJoinClick(string name) {
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.EnterRoom);
        message.data = BitConverter.GetBytes(int.Parse(name));
        NetMgr.GetInstance().tcpSock.Send(message, OnJoinBtnBack);
        Debug.Log(" 请求进入房间 "+name);
    }

    #endregion

    #region 协议监听
    /// <summary>
    /// 收到GetAchieve协议
    /// </summary>
    /// <param name="protocol"></param>
    public void RecvGetAchieve(GameMessage message) {
        
        PlayerInfo playerInfo = ProtoTransfer.Deserialize<PlayerInfo>(message.data);
        int winNum = playerInfo.win;
        int defeatNum = playerInfo.defeat;

        //处理
        idText.text = GameMgr._instance.id;
        winText.text = winNum.ToString();
        defeatText.text = defeatNum.ToString();
    }

    /// <summary>
    /// 收到GetRoomList协议
    /// </summary>
    /// <param name="protocol"></param>
    public void RecvGetRoomList(GameMessage message) {
        //清理
        ClearRoomUnit();
        List<RoomInfo> roominfos = ProtoTransfer.Deserialize<List<RoomInfo>>(message.data);
        int count = roominfos.Count;
        for(int i = 0; i < count; i++) {
            //房间人数
            int num = roominfos[i].playercount;
            //房间状态
            int status = roominfos[i].roomstatus;
            GenerateRoomUnit(i,num,status);
        }
    }
    #endregion

    #region 协议回调
    /// <summary>
    /// 加入按钮回调
    /// </summary>
    /// <param name="protocol"></param>
    public void OnJoinBtnBack(GameMessage message) {
        
        int ret = BitConverter.ToInt32(message.data, 0);
        if (ret == 0) {
            PanelMgr._instance.OpenPanel<TipPanel>("","成功进入房间！");
            PanelMgr._instance.OpenPanel<RoomPanel>("");
            Close();
        } else {
            PanelMgr._instance.OpenPanel<TipPanel>("", "进入房间失败\n请稍后重试");
        }
    }

    /// <summary>
    /// 新建房间回调
    /// </summary>
    /// <param name="protocol"></param>
    public void OnCreateBack(GameMessage message) {
        //解析参数
        
        int ret = BitConverter.ToInt32(message.data,0);

        if(ret == 0) {
            PanelMgr._instance.OpenPanel<TipPanel>("","创建成功！");
            PanelMgr._instance.OpenPanel<RoomPanel>("");
            Close();
        } else {
            PanelMgr._instance.OpenPanel<TipPanel>("", "创建房间失败\n请稍后重试");
        }
    }

    /// <summary>
    /// 登出回调
    /// </summary>
    /// <param name="protocol"></param>
    public void OnLogoutBack(GameMessage message) {
        PanelMgr._instance.OpenPanel<TipPanel>("","登出成功！");
        PanelMgr._instance.OpenPanel<LoginPanel>("");
        NetMgr.GetInstance().tcpSock.Close();
        Close();
    }
    #endregion

    /// <summary>
    /// 清理房间列表实例
    /// </summary>
    public void ClearRoomUnit() {
        for(int i = 0; i < contentTran.childCount; i++) {
            Destroy(contentTran.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 创建一个房间单元
    /// </summary>
    /// <param name="i">房间序号</param>
    /// <param name="num">房间内的玩家数</param>
    /// <param name="status">房间状态</param>
    public void GenerateRoomUnit(int i,int num,int status) {
        //添加房间
        GameObject go = Instantiate(roomPrefab,contentTran);

        //房间信息
        Transform tran = go.transform;
        Text nameText = tran.Find("NameText/Value").GetComponent<Text>();
        Text countText = tran.Find("CountText/Value").GetComponent<Text>();
        Text statusText = tran.Find("StatusText/Value").GetComponent<Text>();

        nameText.text = (i + 1).ToString();
        countText.text = num.ToString();
        if(status == 1) {
            statusText.color = Color.black;
            statusText.text = "准备中";
        } else {
            statusText.color = Color.red;
            statusText.text = "游戏中";
        }

        //按键事件
        Button joinBtn = tran.Find("JoinBtn").GetComponent<Button>();
        joinBtn.name = i.ToString();        //改变按钮的名称，便于区分
        joinBtn.onClick.AddListener(delegate() {
            OnJoinClick(joinBtn.name);
        });
    }
}
