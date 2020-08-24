using DG.Tweening;
using LSGameServ.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RegPanel : PanelBase {
    private InputField idInput;
    private InputField pwInput;
    private InputField repInput;
    private Button regBtn;
    private Button closeBtn;
    private Transform container;

    #region 生命周期
    public override void Init(params object[] args) {
        base.Init(args);
        //设置路径
        skinPath = "UIPanel/RegPanel";
        //设置层级
        layer = PanelLayer.Panel;
    }

    public override void OnShowing() {
        base.OnShowing();
        InitUI();

        container.localPosition += new Vector3(400, 0, 0);
        container.DOLocalMoveX(400, 0.5f).OnComplete(() => {
            regBtn.onClick.AddListener(OnRegClick);
            closeBtn.onClick.AddListener(OnCloseClick);
        });
    }
    #endregion



    void InitUI() {
        Transform skinTran = skin.transform;
        container = skinTran.Find("Container");
        idInput = container.Find("IDInput").GetComponent<InputField>();
        pwInput = container.Find("PWInput").GetComponent<InputField>();
        repInput = container.Find("RepInput").GetComponent<InputField>(); ;

        regBtn = container.Find("RegBtn").GetComponent<Button>();
        closeBtn = container.Find("CloseBtn").GetComponent<Button>();
        

    }

    void OnRegClick() {
        //用户名密码为空
        if (string.IsNullOrEmpty(idInput.text) || string.IsNullOrEmpty(pwInput.text)) {
            PanelMgr._instance.OpenPanel<TipPanel>("", "用户名或密码不能为空");
            return;
        }
        if (repInput.text != pwInput.text) {
            PanelMgr._instance.OpenPanel<TipPanel>("", "两次输入密码不同！");
            return;
        }

        //进行注册
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
        message.type = BitConverter.GetBytes((int)Protocol.Regist);
        ReqLogin reqlogin = new ReqLogin();
        reqlogin.account = idInput.text;
        reqlogin.password = pwInput.text;
        message.data = ProtoTransfer.Serialize<ReqLogin>(reqlogin);
        //发送Login协议，并注册返回监听
        NetMgr.GetInstance().tcpSock.Send(message, OnRegBack);

    }

    void OnCloseClick() {
        OpenLoginPanel();
    }

    public void OnRegBack(GameMessage message) {
        
        int ret = BitConverter.ToInt32(message.data,0);
        if (ret == 0) {
            //注册成功
            TipPanel.OnTipClosed regCallBack = OnRegSuccess;
            PanelMgr._instance.OpenPanel<TipPanel>("", "注册成功", regCallBack);

        } else {
            //注册失败
            PanelMgr._instance.OpenPanel<TipPanel>("", "注册失败，请更换用户名！");

        }
    }

    public void OnRegSuccess() {
        OpenLoginPanel();
    }

    void OpenLoginPanel() {
        //打开登陆界面
        PanelMgr._instance.OpenPanel<LoginPanel>("");
        container.DOLocalMoveX(800, 0.5f).OnComplete(() => {
            Close();
        });
    }
}
