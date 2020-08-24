using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSGameServ.DB;
using LSGameServ.Protobuf;

namespace LSGameServ.Server {
    /// <summary>
    /// 保存和管理房间
    /// </summary>
    public class RoomMgr {
        /// <summary>
        /// 单例
        /// </summary>
        public static RoomMgr _instance;
        
        public RoomMgr() {
            _instance = this;
        }

        /// <summary>
        /// 房间列表
        /// </summary>
        public List<Room> roomList = new List<Room>();

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="player"></param>
        public void CreateRoom(Player player) {
            Room room = new Room();
            lock (roomList) {
                roomList.Add(room);
                room.AddPlayer(player);
            }
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        /// <param name="player"></param>
        public void LeaveRoom(Player player) {
            PlayerTempData tempData = player.tempData;
            //如果玩家不在房间内
            if (tempData.status == PlayerTempData.Status.None) return;

            Room room = tempData.room;
            
            // 多个连接线程需要使用list，需要加锁
            lock (roomList) {
                room.DelPlayer(player.id);
                //若房间已空，则删除房间
                if (room.playerDic.Count == 0)
                    roomList.Remove(room);
            }
            tempData.room = null;
            tempData.status = PlayerTempData.Status.None;
        }
        
        /// <summary>
        /// 获得房间列表
        /// </summary>
        /// <returns></returns>
        public GameMessage GetRoomList() {
            GameMessage msg = new GameMessage();
            msg.type = BitConverter.GetBytes((int)Protocol.GetRoomList);
            
            int count = roomList.Count;
            //房间数量
            List<RoomInfo> roomInfos = new List<RoomInfo>();

            int cout = roomList.Count;
            for (int i=0;i<count;i++) {
                Room room = roomList[i];
                RoomInfo roominfo = new RoomInfo();
                roominfo.playercount = room.playerDic.Count;
                roominfo.roomstatus = (int)room.status;
                roomInfos.Add(roominfo);
            }
            
            msg.data = ProtoTransfer.Serialize<List<RoomInfo>>(roomInfos);

            return msg;
        }
    }
}
