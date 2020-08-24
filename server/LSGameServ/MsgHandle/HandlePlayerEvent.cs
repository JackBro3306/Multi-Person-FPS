using LSGameServ.DB;
using LSGameServ.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.MsgHandle {
    /**
     * 玩家事件类
     * */
    public class HandlePlayerEvent {
        //上线
        public void OnLogin(Player player) {
            //添加玩家到场景中
            PlayerMgr.instance.AddPlayer(player);
        }

        //下线
        public void OnLogout(Player player) {
            //在房间中
            if (player.tempData.status == PlayerTempData.Status.Room) {
                Room room = player.tempData.room;
                RoomMgr._instance.LeaveRoom(player);
                if (room != null)
                    room.BroadcastTcp(room.GetRoomInfo());
            }

            //战斗中
            if (player.tempData.status == PlayerTempData.Status.Fight) {
                Room room = player.tempData.room;
                room.ExitFight(player);
                RoomMgr._instance.LeaveRoom(player);
            }
            PlayerMgr.instance.DelPlayer(player);
        }
    }
}
