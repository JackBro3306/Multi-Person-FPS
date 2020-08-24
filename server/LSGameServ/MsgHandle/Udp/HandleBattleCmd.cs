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
    class HandleBattleCmd : IRecevieHandle {
        public void RegistListener() {
            EventCenter.AddEventListener<Player, BattleCommand>(Protocol.CmdReady,CmdReady);
        }

        public void UnRegistListener() {
            EventCenter.RemoveEventListener<Player, BattleCommand>(Protocol.CmdReady, CmdReady);
        }

        /// <summary>
        /// 玩家准备
        /// </summary>
        /// <param name="room"></param>
        /// <param name="message"></param>
        public void CmdReady(Player player, BattleCommand command) {
            Room room = player.tempData.room;
            room.BindUdpConnect(player.id,player.udpPoint);
        }
    }
}
