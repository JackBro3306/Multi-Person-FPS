using LSGameServ.MsgHandle;
using LSGameServ.Protobuf;
using LSGameServ.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.Net {
    /// <summary>
    /// 游戏服务器
    /// </summary>
    public class GameServer {

        // 连接池
        public Session[] sessions;

        // 最大连接数
        public int maxCount = 50;
       
        private TcpService mTcp;
        private UdpService mUdp;

        public TcpService tcp { get { return mTcp; } }
        public UdpService udp { get { return mUdp; } }
        
        #region 心跳
        // 主定时器
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        // 心跳时间
        public long heartBeatTime = 180;
        #endregion
        // 玩家事件
        public HandlePlayerEvent playerEvent = new HandlePlayerEvent();
        //单例
        public static GameServer instance;
        public GameServer() {
            instance = this;
        }


        public void Start(int tcp, int udp) {
            // 主定时器
            timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;
            timer.Enabled = true;

            // 连接池
            sessions = new Session[maxCount];
            for (int i = 0; i < maxCount; i++) {
                sessions[i] = new Session();
            }
            mTcp = new TcpService(tcp);
            mTcp.Listen();
            //mUdp = new UdpService( udp);
            //mUdp.Open();
        }

        //主定时器
        public void HandleMainTimer(object sender, System.Timers.ElapsedEventArgs e) {
            //处理心跳
            HeartBeatTime();
            timer.Start();
        }

        public void HeartBeatTime() {
            long timeNow = Sys.GetTimeStamp();
            for (int i = 0; i < sessions.Length; i++) {
                Session session = sessions[i];
                if (session == null) continue;
                if (!session.isUse) continue;
                if (session.lastTickTime < timeNow - heartBeatTime) {
                    Debug.Log(string.Format("[心跳引起断开连接] {0}" , session.GetAddress()),ConsoleColor.Red);
                    lock (session)
                        session.Close();
                }
            }
        }

        // 获取连接池，返回负数表示连接池已满
        public int NewIndex() {
            if (sessions == null) {
                return -1;
            }
            for (int i = 0; i < sessions.Length; i++) {
                if (sessions[i] == null) {
                    sessions[i] = new Session();
                    
                    return i;
                } else if (!sessions[i].isUse) {
                    
                    return i;
                }
            }

            
            return -1;
        }

        // 广播
        public void Broadcast(GameMessage message) {
            for (int i = 0; i < sessions.Length; i++) {
                if (!sessions[i].isUse)
                    continue;
                if (sessions[i].player == null)
                    continue;
                mTcp.Send(sessions[i], message);
            }
        }

        // 关闭服务器
        public void Close() {
            for(int i = 0; i < sessions.Length; i++) {
                Session session = sessions[i];
                if (session == null) continue;
                lock (session) {
                    session.Close();
                }
            }
            mTcp.Close();
            timer.Stop();
            timer.Dispose();
        }
    }
}
