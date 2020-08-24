using LSGameServ.EventDispatcher;
using LSGameServ.MsgHandle;
using LSGameServ.Protobuf;
using LSGameServ.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.Net {
    /// <summary>
    /// udp协议处理
    /// </summary>
    public class UdpService {
        UdpClient recvClient;
        UdpClient sendClient;

        // 消息发送队列
        Queue<MsgInfo> msgQueue = new Queue<MsgInfo>();
        bool isSending = false;
        object sendlock = new object();
        // 消息处理
        HandleBattleCmd handleBattleCmd;

        public UdpService(int port) {
            recvClient = new UdpClient(port);
            sendClient = new UdpClient(port);
            recvClient.BeginReceive(ReceiveCb, null);

            handleBattleCmd = new HandleBattleCmd();
        }

        // 开启监听
        public bool Open() {
            // 开启监听
            RegistListener();
            Debug.Log("[开启udp] ", ConsoleColor.Green);
            return true;
        }

        private void RegistListener() {
            handleBattleCmd.RegistListener();
        }

        private void UnRegistListener() {
            handleBattleCmd.UnRegistListener();
        }

        /// <summary>
        /// 接收
        /// </summary>
        void ReceiveCb(IAsyncResult result) {
            IPEndPoint senderPoint = new IPEndPoint(IPAddress.Any,0);
            byte[] recvData = recvClient.EndReceive(result,ref senderPoint);
            ProcessData(recvData, senderPoint);
            recvClient.BeginReceive(ReceiveCb, null);
        }


        void ProcessData(byte[] bytes, IPEndPoint tSenderPoint) {
            byte[] content = new byte[bytes.Length - 4];
            Array.Copy(bytes,4, content, 0,content.Length);
            BattleCommand command = ProtoTransfer.Deserialize<BattleCommand>(content);
            HandleMessage(command, tSenderPoint);
        }

        void HandleMessage(BattleCommand command, IPEndPoint tSenderPoint) {
            string userid = command.userid;
            Player player = PlayerMgr.instance.GetPlayer(userid);
            if (player == null) return;
            if(player.udpPoint == null)
                player.udpPoint = tSenderPoint;
            Room room = player.tempData.room;
            if (room != null) {
                Protocol protocol = (Protocol)command.type[0];
                EventCenter.Broadcast(protocol,room, command);
                room.ReceiveUdp(command.data);
            }
        }
        
        /// <summary>
        /// 发送
        /// </summary>
        public void Send(MsgInfo msg) {
            lock (sendlock) {
                if (!isSending) {
                    isSending = true;
                    sendClient.BeginSend(msg.sendbytes,msg.length,SendCb,null);
                } else {
                    msgQueue.Enqueue(msg);
                }
            }
        }

        void SendCb(IAsyncResult result) {
            lock (sendlock) {
                int sendcount = sendClient.EndSend(result);
                if (msgQueue.Count>0) {
                    MsgInfo msg = msgQueue.Dequeue();
                    sendClient.BeginSend(msg.sendbytes,msg.length,msg.point,SendCb,null);
                } else {
                    isSending = false;
                }
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close() {
            UnRegistListener();
        }


    }

    /// <summary>
    /// udp数据报结构
    /// </summary>
    public struct MsgInfo {
        public int length;
        public IPEndPoint point;
        public byte[] sendbytes;
        
        public MsgInfo(IPEndPoint point,byte[] sendbytes, int length) {
            this.point = point;
            this.sendbytes = sendbytes;
            this.length = length;
        }
    }
}
