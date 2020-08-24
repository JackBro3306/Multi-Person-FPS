using LSGameServ.DB;
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
    public class HandleRoomMsg : IRecevieHandle {

        public void RegistListener() {
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.GetRoomList, MsgGetRoomList);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.CreateRoom, MsgCreateRoom);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.EnterRoom, MsgEnterRoom);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.GetRoomInfo, MsgGetRoomInfo);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.LeaveRoom, MsgLeaveRoom);

        }

        public void UnRegistListener() {
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.GetRoomList, MsgGetRoomList);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.CreateRoom, MsgCreateRoom);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.EnterRoom, MsgEnterRoom);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.GetRoomInfo, MsgGetRoomInfo);
        }

        /// <summary>
        /// 获得房间列表
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgGetRoomList(Session session, GameMessage message) {
            session.SendTcp(RoomMgr._instance.GetRoomList());
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protoBase"></param>
        public void MsgCreateRoom(Session session, GameMessage message) {
            
            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int) Protocol.CreateRoom);
            //条件检测，玩家处于房间中或是战斗中，则创建房间失败
            if (session.player.tempData.status != PlayerTempData.Status.None) {
                Debug.Log(string.Format("MsgCreateRoom Fail {0}", session.player.id),ConsoleColor.Red);
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
                return;
            }

            RoomMgr._instance.CreateRoom(session.player);
            retMsg.data = BitConverter.GetBytes(0);
            session.SendTcp(retMsg);
            Debug.Log(string.Format("MsgCreateRoom Success {0}" , session.player.id),ConsoleColor.Green);
        }

        /// <summary>
        /// 进入房间
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgEnterRoom(Session session, GameMessage message) {
            //获取参数
            
            int index = BitConverter.ToInt32(message.data,0);
            Debug.Log(string.Format("[ 收到MsgEnterRoom ] {0} {1}",session.player.id,  index));

            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int)Protocol.EnterRoom);
            //判断房间是否存在
            if (index < 0 || index >= RoomMgr._instance.roomList.Count) {
                Debug.Log(string.Format("MsgEnterRoom index err {0}", session.player.id),ConsoleColor.Red);
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
                return;
            }

            Room room = RoomMgr._instance.roomList[index];
            //判断房间状态
            if (room!=null&&room.status != Room.Status.Prepare) {
                Debug.Log(string.Format("MsgEnterRoom status err {0}", session.player.id), ConsoleColor.Red);
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
                return;
            }

            //添加玩家
            if (room.AddPlayer(session.player)) {
                room.BroadcastTcp(room.GetRoomInfo());
                retMsg.data = BitConverter.GetBytes(0);
                session.SendTcp(retMsg);
            } else {
                Debug.Log(string.Format("MsgEnterRoom maxPlayer err {0}", session.player.id), ConsoleColor.Red);
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
            }
        }

        /// <summary>
        /// 获得房间信息
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgGetRoomInfo(Session session, GameMessage message) {
            //玩家不在房间中，无法获取房间信息
            if (session.player.tempData.status != PlayerTempData.Status.Room) {
                Debug.Log(string.Format("MsgGetRoomInfo status err {0}", session.player.id),ConsoleColor.Red);
                return;
            }

            Room room = session.player.tempData.room;
            session.SendTcp(room.GetRoomInfo());
        }

        
        public void MsgLeaveRoom(Session session, GameMessage message) {
            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int)Protocol.LeaveRoom);
            //如果玩家不在房间内，表示离开房间失败
            if (session.player.tempData.status != PlayerTempData.Status.Room) {
                Debug.Log(string.Format("MsgLeaveRoom status err {0}" , session.player.id),ConsoleColor.Red);
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
                return;
            }

            //处理
            retMsg.data = BitConverter.GetBytes(0);
            session.SendTcp(retMsg);
            Room room = session.player.tempData.room;
            RoomMgr._instance.LeaveRoom(session.player);
            //广播
            if (room != null)
                room.BroadcastTcp(room.GetRoomInfo());
        }

    }
}
