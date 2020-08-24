using LSGameServ.Protobuf;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

public class TcpSocket  {
    //常量
    const int BUFFER_SIZE = 1024;
    
    //Socket
    private Socket socket;
    //Buff
    private byte[] readBuff = new byte[BUFFER_SIZE];
    private int buffCount = 0;
    //粘包分包
    private Int32 msgLength = 0;
    private byte[] lenBytes = new byte[sizeof(Int32)];
    
    //心跳时间
    public float lastTickTime = 0;
    public float heartBeatTime = 30;
    //消息分发
    public MsgDistribution msgDist = new MsgDistribution();
    //状态
    public enum Status {
        None,
        Connected,
    };
    public Status status = Status.None;

    //连接服务器
    public bool Connect(string host,int port) {
        try {
            //socket
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            //Connect
            socket.Connect(host, port);
            //BeginReceive
            socket.BeginReceive(readBuff,buffCount,BUFFER_SIZE - buffCount,SocketFlags.None,ReceiveCb,readBuff);
            Debug.Log("连接成功");
            //状态
            status = Status.Connected;
            return true;
        } catch (Exception e) {
            Debug.Log(" 连接失败："+e.Message);
            
            return false;
        }
    }

    public void Update() {
        //消息
        msgDist.Update();
        //心跳
        if (status == Status.Connected) {
            if (Time.time - lastTickTime > heartBeatTime) {
                GameMessage message = NetMgr.GetInstance().GetHeartBeatProtocol();
                Send(message);
                lastTickTime = Time.time;
            }
        }
    }

    //接收回调
    private void ReceiveCb(IAsyncResult ar) {
        try {
            int count = socket.EndReceive(ar);
            buffCount = buffCount + count;
            ProcessData();
            socket.BeginReceive(readBuff, buffCount, BUFFER_SIZE - buffCount, SocketFlags.None, ReceiveCb, readBuff);

        } catch (Exception e) {
            Debug.Log("ReceiveCb失败："+e.Message);
            status = Status.None;
        }
    }

    //消息处理
    private void ProcessData() {
        //粘包分包处理
        if (buffCount < sizeof(Int32))
            return;
        //包体长度
        Array.Copy(readBuff,lenBytes,sizeof(Int32));
        msgLength = BitConverter.ToInt32(lenBytes,0);
        if (buffCount < msgLength + sizeof(Int32)) {
            return;
        }

        //协议解码
        GameMessage protocol = ProtoTransfer.Deserialize(readBuff, sizeof(Int32), msgLength);
        
        lock (msgDist.msgList) {
            msgDist.msgList.Add(protocol);
        }
        //消除已处理的消息
        int count = buffCount - msgLength - sizeof(Int32);
        Array.Copy(readBuff,sizeof(Int32) + msgLength,readBuff,0,count);
        buffCount = count;
        if (buffCount > 0) {
            ProcessData();
        }
    }

    public bool Send(GameMessage message) {
        if (status != Status.Connected) {
            Debug.LogError(" [COnnection] 需要先连接再发送数据");
            return true;
        }

        byte[] b = ProtoTransfer.Serialize(message);

        return Send(b);
    }

    public bool Send(byte[] b) {

        byte[] length = BitConverter.GetBytes(b.Length);

        byte[] sendbuff = length.Concat(b).ToArray();
        socket.Send(sendbuff);
        
        return true;
    }

    public bool Send(GameMessage message,Protocol msgType,MsgDistribution.Delegate cb) {
        if (status != Status.Connected)
            return false;
        msgDist.AddOnceListener(msgType, cb);
        return Send(message);
    }
    
    public bool Send(GameMessage message,MsgDistribution.Delegate cb) {
        Protocol msgType =(Protocol)message.type[0];
        return Send(message, msgType, cb); 
    }
    
    //关闭连接
    public bool Close() {
        try {
            socket.Close();
            return true;
        } catch (Exception e) {
            Debug.Log(" 关闭失败："+e.Message );
            return false;
        }
    }
}
