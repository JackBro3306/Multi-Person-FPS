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
    /// 连接类
    /// </summary>
    public class Session {
        public const int BUFFER_SIZE = 1024;

        public Socket socket;
        
        // 是否被使用
        public bool isUse = false;
        #region 粘包分包
        // 读缓冲区
        public byte[] readBuff = new byte[BUFFER_SIZE];
        public int buffCount = 0;
        //粘包分包
        public byte[] lenBytes = new byte[sizeof(UInt32)];
        public Int32 msgLength = 0;
        #endregion
        // 心跳时间
        public long lastTickTime = long.MinValue;
        // 对应的player
        public Player player;


        public Session() {
            //初始化读缓冲区
            readBuff = new byte[BUFFER_SIZE];
        }

        public void Init(Socket socket) {
            
            this.socket = socket;
            isUse = true;
            buffCount = 0;
            // 心跳处理
            lastTickTime = Sys.GetTimeStamp();
        }

        // 剩余的buff
        public int BuffRemain() {
            return BUFFER_SIZE - buffCount;
        }

        // 获取客户端地址
        public string GetAddress() {
            if (!isUse) {
                return "无法获取地址";
            }
            return socket.RemoteEndPoint.ToString();
        }
        
        // 发送
        public void SendTcp(GameMessage message) {
            GameServer.instance.tcp.Send(this,message);
        }
        // 关闭
        public void Close() {
            if (!isUse) return;
            // 关闭连接前需要保存玩家数据
            if(player != null) {
                // 玩家退出处理
                player.Logout();
                return;
            }

            Debug.Log("[断开连接]"+GetAddress(),ConsoleColor.Green);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            isUse = false;
        }

        
    }
}
