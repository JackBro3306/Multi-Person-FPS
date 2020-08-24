using LSGameServ.DB;
using LSGameServ.Net;
using LSGameServ.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.Server {
    public class Player {
        public string id;
        public Session session;
        public IPEndPoint udpPoint;
        public PlayerData data;

        // 临时数据
        public PlayerTempData tempData;


        //构造函数
        public Player(string id, Session session) {
            this.id = id;
            this.session = session;
            tempData = new PlayerTempData();
        }
        
        //踢下线
        public static bool KickOff(string id, GameMessage message) {
            Session[] session = GameServer.instance.sessions;
            for (int i = 0; i < session.Length; i++) {
                if (session[i] == null)
                    continue;
                if (!session[i].isUse)
                    continue;
                if (session[i].player == null)
                    continue;
                if (session[i].player.id == id) {
                    lock (session[i].player) {
                        if (message != null)
                            session[i].SendTcp(message);

                        return session[i].player.Logout();
                    }

                }
            }

            return true;
        }

        //下线
        public bool Logout() {
            //事件处理
            //消息分发的内容之一
            GameServer.instance.playerEvent.OnLogout(this);
            //保存
            if (!DataMgr.instance.SavePlayer(this))
                return false;
            //下线
            session.player = null;
            session.Close();
            return false;
        }
    }
}
