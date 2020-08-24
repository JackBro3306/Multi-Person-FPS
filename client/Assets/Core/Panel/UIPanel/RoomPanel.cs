using DG.Tweening;
using LSGameServ.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanel : PanelBase {

    private List<Transform> t1Prefabs = new List<Transform>();
    private List<Transform> t2Prefabs = new List<Transform>();
    private Button closeBtn;
    private Button startBtn;

    #region 页面动画相关
    private Transform t1Tran;
    private Transform t2Tran;
    private Transform roomImgTran;
    private CanvasGroup roomCanvasGroup;
    #endregion


    /// <summary>
    /// 开始战斗结果
    /// </summary>
    public enum Start2Fight {
        Success = 0,        //成功
        NotOwner = 1,       // 不是房主
        Fighting = 2,       // 房间正在战斗中
        NotExitBattle = 3,  // 还有玩家未退出战斗
        NotEnoughPlayer = 4,    //人数不足
    }

    #region 生命周期

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="args"></param>
    public override void Init(params object[] args) {
        base.Init(args);
        //设置路径
        skinPath = "UIPanel/RoomPanel";
        //设置层级
        layer = PanelLayer.Panel;
    }

    public override void OnShowing() {
        base.OnShowing();
        InitUI();
        skin.transform.localScale = new Vector3(0,0,1);
        skin.transform.DOScale(new Vector3(1,1,1),0.5f);
        //协议回调监听
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.GetRoomInfo, RecvGetRoomInfo);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.Fight, RecvFight);

        //发送查询
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.GetRoomInfo);
        message.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(message);
    }
    
    void InitUI() {
        //玩家信息组件
        Transform skinTran = skin.transform;
        string name;
        for (int i = 0; i < 3; i++) {
            name = "PlaerPefabGroup/Team1/PlayerPrefab" + i;
            Transform prefab1 = skinTran.Find(name);
            t1Prefabs.Add(prefab1);
            name = "PlaerPefabGroup/Team2/PlayerPrefab" + i;
            Transform prefab2 = skinTran.Find(name);
            t2Prefabs.Add(prefab2);
        }

        t1Tran = skinTran.Find("PlaerPefabGroup/Team1");
        t2Tran = skinTran.Find("PlaerPefabGroup/Team2");
        roomImgTran = skinTran.Find("RoomImage");
        roomCanvasGroup = roomImgTran.GetComponent<CanvasGroup>();

        closeBtn = skinTran.Find("CloseBtn").GetComponent<Button>();
        startBtn = roomImgTran.Find("StartBtn").GetComponent<Button>();

        //按钮事件
        closeBtn.onClick.AddListener(OnCloseClick);
        startBtn.onClick.AddListener(OnStartClick);
    }

    
    public override void OnClosing() {
        base.OnClosing();
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.GetRoomInfo, RecvGetRoomInfo);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.Fight, RecvFight);
    }
    #endregion

    #region 按钮监听
    /// <summary>
    /// 点击关闭
    /// </summary>
    public void OnCloseClick() {
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.LeaveRoom);
        message.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(message, OnClickBack);
    }
    /// <summary>
    /// 点击开始
    /// </summary>
    public void OnStartClick() {
        GameMessage msg = new GameMessage();
        msg.type = BitConverter.GetBytes((int)Protocol.StarFight);
        msg.data = BitConverter.GetBytes(1);
        NetMgr.GetInstance().tcpSock.Send(msg, OnStartBack);
    }
    
    #endregion

    #region 协议回调监听
    /// <summary>
    /// 获得房间信息
    /// </summary>
    /// <param name="protocol"></param>
    public void RecvGetRoomInfo(GameMessage message) {
        List<RoomPlayer> roomPlayers = ProtoTransfer.Deserialize<List<RoomPlayer>>(message.data);
        //获得房间内的人数
        int count = roomPlayers.Count;

        int i = 0;
        int t1Index = 0;
        int t2Index = 0;
        for (i = 0; i < count; i++) {
            string id = roomPlayers[i].playerinfo.id;
            int team = roomPlayers[i].team;
            int winNum = roomPlayers[i].playerinfo.win;
            int defeatNum = roomPlayers[i].playerinfo.defeat;
            int isOwner = roomPlayers[i].isOwner;

            Transform tran;
            if (team == 1) {
                tran = t1Prefabs[t1Index++];
            } else {
                tran = t2Prefabs[t2Index++];
            }
            //信息显示
            
            Transform playerInfoTran = tran.Find("PlayerInfo");
            playerInfoTran.gameObject.SetActive(true);

            Text idText = playerInfoTran.Find("IdText/Value").GetComponent<Text>();
            Text campText = playerInfoTran.Find("CampText/Value").GetComponent<Text>();
            Text winText = playerInfoTran.Find("WinText/Value").GetComponent<Text>();
            Text defeatText = playerInfoTran.Find("DefeatText/Value").GetComponent<Text>();

            Text remarksText = tran.Find("RemarksText").GetComponent<Text>();

            idText.text = id;
            campText.text = (team == 1) ? "红" : "蓝";
            winText.text = winNum.ToString();
            defeatText.text = defeatNum.ToString();

            string str ="";
            if (id == GameMgr._instance.id) {
                
                str += "【我自己】";
            }
                
            if (isOwner == 1) {
                str += "【房主】";
            } 
                
            remarksText.text = str;
            
        }
        
        //处理没有玩家的格子
        for (int index = t1Index; index < 3; index++) {
            CreateEmpty(t1Prefabs[index]);
        }

        for (int index = t2Index; index < 3; index++) {
            CreateEmpty(t2Prefabs[index]);
        }
        
    }

    void CreateEmpty(Transform tran) {
        Transform playerInfoTran = tran.Find("PlayerInfo");
        playerInfoTran.gameObject.SetActive(false);

        Text remarksText = tran.Find("RemarksText").GetComponent<Text>();
        remarksText.text = "【等待玩家】";
    }
    
    /// <summary>
    /// 开始战斗协议回调
    /// </summary>
    /// <param name="protocol"></param>
    public void OnStartBack(GameMessage msg) {

        Start2Fight ret = (Start2Fight)msg.data[0];
        switch (ret) {
            case Start2Fight.NotOwner:
                PanelMgr._instance.OpenPanel<TipPanel>("", "开始游戏失败！\n只有队长可以开始战斗");
                break;
            case Start2Fight.Fighting:
                PanelMgr._instance.OpenPanel<TipPanel>("", "开始游戏失败！\n当前房间正在战斗中\n只有队长可以开始战斗");
                break;
            case Start2Fight.NotExitBattle:
                PanelMgr._instance.OpenPanel<TipPanel>("", "开始游戏失败！\n还有玩家未退出战斗");
                break;
            case Start2Fight.NotEnoughPlayer:
                PanelMgr._instance.OpenPanel<TipPanel>("", "开始游戏失败！\n两队至少都需要一名玩家");
                break;
        }

    }

    /// <summary>
    /// 开始战斗协议回调
    /// </summary>
    /// <param name="protocol"></param>
    public void RecvFight(GameMessage msg) {
        roomCanvasGroup.blocksRaycasts = false;
        roomCanvasGroup.DOFade(0, 1f);
        roomImgTran.DOScale(new Vector3(2, 2, 1), .5f);
        t1Tran.DOLocalMoveY(280, .5f);
        t2Tran.DOLocalMoveY(-300, .5f).OnComplete(() => {
            MutiBattle._instance.StartBattle(msg);
            Close();
        });
        // 关闭提示框
        PanelMgr._instance.ClosePanel<TipPanel>();
    }

    /// <summary>
    /// 离开房间协议回调
    /// </summary>
    /// <param name="protocol"></param>
    public void OnClickBack(GameMessage message) {
       
        int ret = BitConverter.ToInt32(message.data,0);

        //处理
        if(ret == 0) {
            PanelMgr._instance.OpenPanel<TipPanel>("", "退出成功");
            skin.transform.DOScale(new Vector3(0, 0, 1), 0.5f).OnComplete(()=> {
                PanelMgr._instance.OpenPanel<RoomListPanel>("");
                Close();
            });
            
        } else {
            PanelMgr._instance.OpenPanel<TipPanel>("", "退出失败\n请稍后再试");
        }
    }
    
    #endregion
}
