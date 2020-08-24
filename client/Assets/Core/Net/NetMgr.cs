using LSGameServ.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetMgr  {
    #region 单例
    /// <summary>
    /// 静态私有成员变量，存储唯一实例
    /// </summary>
    private static NetMgr _instance = null;
    /// <summary>
    /// 私有构造函数，保证唯一性
    /// </summary>
    private NetMgr() {

    }
    /// <summary>
    /// 共有静态方法，返回一个唯一的实例
    /// </summary>
    /// <returns></returns>
    public static NetMgr GetInstance() {
        if(_instance == null) {
            _instance = new NetMgr();
        }
        return _instance;
    }
    #endregion

    #region Tcp
    public TcpSocket tcpSock = new TcpSocket();
    public string tcpHost = "127.0.0.1";
    #endregion

    #region Udp
    //public UdpSocket udpSocket = new UdpSocket();
    //private Lockstep mLockStep = new Lockstep();
    //private FrameData mFrameData = new FrameData();
    #endregion

    public void Update() {
        tcpSock.Update();
        //mLockStep.Update();
    }
    #region udp
    //public void AddOneFrame(uint frameindex,List<BattleCommand> list) {
    //    mFrameData.AddOneFrame(frameindex,list);
    //}

    //public bool LockFrameTurn(ref List<BattleCommand> list) {
    //    return mFrameData.LockFrameTurn(ref list);
    //}

    //public void SetFaseForward(int tValue) {
    //    mLockStep.SetFaseForward(tValue);
    //}

    //public void UpdateEvent() {

    //}
    #endregion
    // 心跳包
    public GameMessage GetHeartBeatProtocol() {
        //具体的发送内容根据服务端设定进行改动
        GameMessage message = new GameMessage();
        message.type = BitConverter.GetBytes((int)Protocol.HurtBeat);
        message.data = BitConverter.GetBytes(1);
        return message;
    }
}
