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
using System.Threading;
using System.Threading.Tasks;

namespace LSGameServ.Net {
    public interface IRecevieHandle {
         void RegistListener();
         void UnRegistListener();
    }
    /// <summary>
    /// tcp协议处理
    /// </summary>
    public class TcpService {
        // 监听套接字
        public Socket listenfd;

        private IPEndPoint endPoint;
        // 端口
        private int mPort;

        // 消息处理
        private HandleConnMsg handleConnMsg;
        private HandlePlayerMsg handlePlayerMsg;
        private HandleRoomMsg handleRoomMsg;
        private HandleBattleMsg handleBattleMsg;

        public TcpService(int port){
            
            mPort = port;
            endPoint = new IPEndPoint(IPAddress.Any, port);

            // 消息分发
            handleConnMsg = new HandleConnMsg();
            handlePlayerMsg = new HandlePlayerMsg();
            handleRoomMsg = new HandleRoomMsg();
            handleBattleMsg = new HandleBattleMsg();
        }

        // 开启监听
        public bool Listen() {
            // 开启监听
            RegistListener();

            // 创建socket
            listenfd = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            listenfd.Bind(endPoint);
            listenfd.Listen(GameServer.instance.maxCount);
            //等待客户端连接
            listenfd.BeginAccept(AcceptCb, null);
            Debug.Log("[监听Tcp] " + endPoint.ToString(),ConsoleColor.Green);
            return true;
        }

        private void RegistListener() {
            handleConnMsg.RegistListener();
            handlePlayerMsg.RegistListener();
            handleRoomMsg.RegistListener();
            handleBattleMsg.RegistListener();
        }

        private void UnRegistListener() {
            handleConnMsg.UnRegistListener();
            handlePlayerMsg.UnRegistListener();
            handleRoomMsg.UnRegistListener();
            handleBattleMsg.UnRegistListener();
        }

        // 连接监听回调
        private void AcceptCb(IAsyncResult ar) {
            try {
                //使用EndAccept获得连接信息
                Socket socket = listenfd.EndAccept(ar);
                int index = GameServer.instance.NewIndex();
                
                if (index < 0) {
                    socket.Close();
                    Debug.Log("[警告]连接已满",ConsoleColor.Yellow);
                } else {
                    Session session = GameServer.instance.sessions[index];
                    session.Init(socket);
                    Debug.Log(string.Format("客户端连接[{0}] conn池ID：{1}",session.GetAddress(), index),ConsoleColor.Green);

                    //异步接收客户端数据
                    session.socket.BeginReceive(session.readBuff, session.buffCount, session.BuffRemain(), SocketFlags.None, ReceiveCb, session);
                }

                //再次调用BeginAccept实现循环，再次等待连接
                listenfd.BeginAccept(AcceptCb, null);
            } catch (Exception e) {
                Debug.Log("Accept失败：" + e.Message,ConsoleColor.Red);
            }
        }
        

        // 信息接收监听
        private void ReceiveCb(IAsyncResult ar) {
            Session session = (Session)ar.AsyncState;
            lock (session) {
                try {
                    //使用EndReceive获得接收信息，接收的字节数
                    int count = session.socket.EndReceive(ar);

                    //关闭信号
                    if (count <= 0) {
                        Debug.Log(string.Format("收到[{0}] 断开连接", session.GetAddress()),ConsoleColor.Red);
                        session.Close();
                        return;
                    }

                    session.buffCount += count;
                    ProcessData(session);

                    //继续接收,实现循环
                    session.socket.BeginReceive(session.readBuff, session.buffCount, session.BuffRemain(), SocketFlags.None, ReceiveCb, session);
                } catch (Exception e) {
                    Debug.Log(string.Format("收到[{0}] 断开连接 {1}", session.GetAddress(), e.Message),ConsoleColor.Red);
                    session.Close();
                }
            }

        }

        // 处理消息粘包
        private void ProcessData(Session session) {
            //其中消息长度为一个32位的int类型，转换成byte即占用4个字节的空间
            //小于长度字节
            if (session.buffCount < sizeof(Int32)) {
                return;
            }

            //消息长度，获得消息长度的byte数据（4个字节组成）
            Array.Copy(session.readBuff, session.lenBytes, sizeof(Int32));
            //将该四个字节转换成int
            session.msgLength = BitConverter.ToInt32(session.lenBytes, 0);
            if (session.buffCount < session.msgLength + sizeof(Int32)) {
                return;
            }

            //处理消息，从消息长度后取得消息内容
            //string str = System.Text.Encoding.UTF8.GetString(conn.readBuff,sizeof(Int32),conn.msgLength);
            GameMessage message = ProtoTransfer.Deserialize(session.readBuff, sizeof(Int32), session.msgLength);
            HandleMsg(session, message);

            //清除已处理的消息
            int count = session.buffCount - session.msgLength - sizeof(Int32);
            Array.Copy(session.readBuff, sizeof(Int32) + session.msgLength, session.readBuff, 0, count);
            session.buffCount = count;
            if (session.buffCount > 0) {
                ProcessData(session);
            }
        }

        // 处理消息
        private void HandleMsg(Session session, GameMessage message) {
            Protocol protocol = (Protocol)message.type[0];
            EventCenter.Broadcast(protocol,session,message);
        }

        // 发送
        public void Send(Session session,GameMessage message) {
            byte[] bytes = ProtoTransfer.Serialize(message);
            // 消息长度
            byte[] lengthByte = BitConverter.GetBytes(bytes.Length);

            // 拼接消息
            byte[] sendbuff = lengthByte.Concat(bytes).ToArray();

            try {
                session.socket.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, null, null);
            } catch (Exception e) {
                Debug.Log(string.Format("[发送消息]{0} : {1}", session.GetAddress() , e.Message),ConsoleColor.Red);
            }
        }

        // 关闭
        public void Close() {
            UnRegistListener();
        }

    }
}
