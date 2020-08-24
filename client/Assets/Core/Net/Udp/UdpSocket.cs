using LSGameServ.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class UdpSocket  {
    private IPEndPoint ipEndPoint = null;
    private UdpClient mUdpClient = null;
    // udp端口
    private const int mPort = 7001;

    // 发送消息队列
    Queue<byte[]> sendMsgInfoQueue = new Queue<byte[]>();
    bool sending = false;
    bool isOpen = false;

    object lockobj = new object();

    public UdpSocket() {
        ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"),mPort);
        mUdpClient = new UdpClient(0);
        Open();
    }

    public void Open() {
        isOpen = true;
        mUdpClient.BeginReceive(new AsyncCallback(ReceiveCb),null);
    }

    // 发送一个消息给服务，请求连接
    public void Connect() {

    }

    // 接收回调函数
    private void ReceiveCb(IAsyncResult result) {
        if (!isOpen) return;
        IPEndPoint point = null;
        byte[] receiveBytes = mUdpClient.EndReceive(result,ref point);
        // 处理数据
        ProcessData(receiveBytes);
        mUdpClient.BeginReceive(new AsyncCallback(ReceiveCb),null);
    }

    // 处理数据
    private void ProcessData(byte[] recvBytes) {
        
        using (MemoryStream stream = new MemoryStream(recvBytes)) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                uint frameindex = reader.ReadUInt32();
                var cmdList = new List<BattleCommand>();
                while (true) {
                    try {
                        int length = reader.ReadInt32();
                        byte[] temp_bytes = reader.ReadBytes(length-4);
                        BattleCommand cmd = ProtoTransfer.Deserialize<BattleCommand>(temp_bytes);
                        cmdList.Add(cmd);
                    } catch {
                        //NetMgr.GetInstance().AddOneFrame(frameindex, cmdList);
                        break;
                    }
                }
            }
        }
    }

    // 发送
    public void Send(BattleCommand battlecmd) {
        byte[] bytes =  ProtoTransfer.Serialize(battlecmd);
        Send(bytes);
    }

    public void Send(byte[] bytes) {
        if (!isOpen) return;
        int length = bytes.Length + 4;
        byte[] lengthbyte = BitConverter.GetBytes(length);
        byte[] sendBytes = new byte[length];
        Array.Copy(lengthbyte,0,sendBytes,0,lengthbyte.Length);
        Array.Copy(bytes,0,sendBytes,lengthbyte.Length,bytes.Length);

        lock (lockobj) {
            if (!sending) {
                sending = true;
                mUdpClient.BeginSend(sendBytes,sendBytes.Length,ipEndPoint,new AsyncCallback(SendCb),null);
            } else {
                sendMsgInfoQueue.Enqueue(sendBytes);
            }
        }
    }

    private void SendCb(IAsyncResult result) {
        if (!isOpen) return;
        lock (lockobj) {
            if(sendMsgInfoQueue.Count > 0) {
                byte[] bytes = sendMsgInfoQueue.Dequeue();
                mUdpClient.BeginSend(bytes,bytes.Length,ipEndPoint,new AsyncCallback(SendCb),mUdpClient);
            } else {
                sending = false;
            }
        }
    }
    public void Close() {
        isOpen = false;
        sendMsgInfoQueue.Clear();
    }
}
