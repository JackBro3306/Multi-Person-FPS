using DG.Tweening;
using LSGameServ.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : PanelBase {
    private InputField idInput;
    private InputField pwInput;
    private Button loginBtn;
    private Button regBtn;
    private Transform containerTran;

    #region 生命周期
    public override void Init(params object[] args) {
        base.Init(args);
        //设置路径
        skinPath = "UIPanel/LoginPanel";
        //设置层级
        layer = PanelLayer.Panel;
        
    }

    public override void OnShowing() {
        base.OnShowing();
        InitUI();
        containerTran.localPosition += new Vector3(400,0,0);
        containerTran.DOLocalMoveX(400, 0.5f).OnComplete(() => {
            loginBtn.onClick.AddListener(OnLoginClick);
            regBtn.onClick.AddListener(OnRegClick);
        });
    }
    #endregion

    void InitUI() {
        Transform skinTran = skin.transform;
        containerTran = skinTran.Find("Container");
        idInput = containerTran.Find("IDInput").GetComponent<InputField>();
        pwInput = containerTran.Find("PWInput").GetComponent<InputField>();
        

        loginBtn = containerTran.Find("LoginBtn").GetComponent<Button>();
        regBtn = containerTran.Find("RegBtn").GetComponent<Button>();
        
    }

    void OnLoginClick() {
        //用户名密码为空
        if(string.IsNullOrEmpty(idInput.text)|| string.IsNullOrEmpty(pwInput.text)) {
            PanelMgr._instance.OpenPanel<TipPanel>("", "用户名或密码不能为空");
            return;
        }

        //进行登陆
        //如果尚未连接，则发起连接
        if (NetMgr.GetInstance().tcpSock.status != TcpSocket.Status.Connected) {
            
            int port = 7000;
            if (!NetMgr.GetInstance().tcpSock.Connect(NetMgr.GetInstance().tcpHost, port)) {
                //生成提示面板
                //连接失败
                PanelMgr._instance.OpenPanel<TipPanel>("", "服务器连接失败\n请稍后再试");
                return;
            }
        }

        //发送
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.Login);
        ReqLogin reqlogin = new ReqLogin();
        reqlogin.account = idInput.text;
        reqlogin.password = pwInput.text;
        message.data = ProtoTransfer.Serialize<ReqLogin>(reqlogin);

        //发送Login协议，并注册返回监听
        NetMgr.GetInstance().tcpSock.Send(message, OnLoginBack);
    }

    public void OnLoginBack(GameMessage message) {
        
        int ret = BitConverter.ToInt32(message.data,0);
        if(ret == 0) {
            //登陆成功
            TipPanel.OnTipClosed loginCallBack = OnLoginSuccess;
            PanelMgr._instance.OpenPanel<TipPanel>("", "登陆成功", loginCallBack);

        } else {
            //登陆失败
            PanelMgr._instance.OpenPanel<TipPanel>("", "登陆失败\n请检查用户名密码");

        }
    }
    
    public void OnLoginSuccess() {

        //开始游戏
        PanelMgr._instance.OpenPanel<RoomListPanel>("");
        GameMgr._instance.id = idInput.text;
        Close();
    }

    void OnRegClick() {
        PanelMgr._instance.OpenPanel<RegPanel>("");
        containerTran.DOLocalMoveX(800, 0.5f).OnComplete(() => {
            Close();
        });
        
    }
}
