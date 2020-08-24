using LSGameServ.EventDispatcher;
using LSGameServ.Net;
using LSGameServ.Protobuf;
using LSGameServ.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.MsgHandle {
    /**
     * 处理角色协议
     * */
    public partial class HandlePlayerMsg : IRecevieHandle {

        // 注册监听事件
        public void RegistListener() {
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.GetAchieve, MsgGetAchieve);
        }

        public void UnRegistListener() {
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.GetAchieve, MsgGetAchieve);
        }

        /// <summary>
        /// 获得玩家信息
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protoBase"></param>
        public void MsgGetAchieve(Session session, GameMessage message) {
            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int)Protocol.GetAchieve);
            PlayerInfo playerinfo = new PlayerInfo();
            playerinfo.id = session.player.id;
            playerinfo.win = session.player.data.win;
            playerinfo.defeat = session.player.data.defeat;
            retMsg.data = ProtoTransfer.Serialize<PlayerInfo>(playerinfo);

            session.SendTcp(retMsg);

            Debug.Log(string.Format("MsgGetScore {0}：{1}", session.player.id, session.player.data.win));
        }
    }
}
