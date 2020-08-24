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
    public class HandleBattleMsg : IRecevieHandle {

        public void RegistListener() {
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.StarFight, MsgStartFight);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.UpdateUnitInfo, MsgUpdateUnitInfo);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.Shooting, MsgShooting);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.Reload, MsgReload);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.Hit, MsgHit);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.FightComplete, MsgCompleteFight);
        }

        public void UnRegistListener() {
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.StarFight, MsgStartFight);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.UpdateUnitInfo, MsgUpdateUnitInfo);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.Shooting, MsgShooting);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.Reload, MsgReload);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.Hit, MsgHit);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.FightComplete, MsgCompleteFight);
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        public void MsgStartFight(Session session, GameMessage message) {
            Player player = session.player;
            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int)Protocol.StarFight);

            //判断当前玩家是否在房间内
            if (player.tempData.status != PlayerTempData.Status.Room) {
                Debug.Log("StartFight status err" + player.id,ConsoleColor.Yellow);
                retMsg.data = BitConverter.GetBytes((int)Room.Start2Fight.NotExitBattle);
                session.SendTcp(retMsg);
                return;
                
            }

            //判断是否为房主
            if (!player.tempData.isOwner) {
                Console.WriteLine("MsgStartFight owner err" + player.id);
                retMsg.data = BitConverter.GetBytes((int)Room.Start2Fight.NotOwner);
                session.SendTcp(retMsg);
                return;
            }

            // 判断能否开始战斗
            Room room = player.tempData.room;
            Room.Start2Fight start2Fight = room.CanStart();
            if (start2Fight != Room.Start2Fight.Success) {
                Console.WriteLine("MsgStartFight CanStart err" + player.id);
                retMsg.data = BitConverter.GetBytes((int)start2Fight);
                session.SendTcp(retMsg);
                return;
            }

            
            //开始战斗
            retMsg.data = BitConverter.GetBytes((int)start2Fight);
            session.SendTcp(retMsg);
            room.StartFight();
        }

        /// <summary>
        /// 同步玩家
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgUpdateUnitInfo(Session session, GameMessage message) {
            UnitInfo unitInfo = ProtoTransfer.Deserialize<UnitInfo>(message.data);

            Player player = session.player;

            //获取房间
            if (player.tempData.status != PlayerTempData.Status.Fight) return;

            Room room = session.player.tempData.room;
            //作弊校验略
            player.tempData.posX = unitInfo.posX;
            player.tempData.posY = unitInfo.posY;
            player.tempData.posZ = unitInfo.posZ;
            player.tempData.lastUpdateTime = Sys.GetTimeStamp();

            //广播
            unitInfo.id = player.id;
            message.data = ProtoTransfer.Serialize(unitInfo);
            room.BroadcastTcp(message);
        }

        /// <summary>
        /// 同步子弹
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgShooting(Session session, GameMessage message) {
            Player player = session.player;
           
            //获取房间
            if (player.tempData.status != PlayerTempData.Status.Fight) return;
            ShootInfo shootInfo = ProtoTransfer.Deserialize<ShootInfo>(message.data);
            shootInfo.id = player.id;
            message.data = ProtoTransfer.Serialize(shootInfo);
            player.tempData.room.BroadcastTcp(message);
        }

        /// <summary>
        /// 同步换弹操作
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgReload(Session session, GameMessage message) {

            Player player = session.player;

            //获取房间
            if (player.tempData.status != PlayerTempData.Status.Fight) return;
            message.data = System.Text.Encoding.UTF8.GetBytes(player.id);
            player.tempData.room.BroadcastTcp(message);
        }

        /// <summary>
        /// 伤害处理
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protoBase"></param>
        public void MsgHit(Session session, GameMessage message) {
            Player player = session.player;
            HitInfo hitInfo = ProtoTransfer.Deserialize<HitInfo>(message.data);
            //解析协议
            string attId = hitInfo.attId;
            int damage = hitInfo.damage;
            //作弊校验
            //long lastShootTime = player.tempData.lastShootTime;
            //if (Sys.GetTimeStamp() - lastShootTime < 0.1f) {
            //    Debug.Log("MsgHit开枪作弊 " + attId,ConsoleColor.Yellow);
            //    return;
            //}
            //player.tempData.lastShootTime = Sys.GetTimeStamp();
            //获取房间
            if (player.tempData.status != PlayerTempData.Status.Fight)
                return;
            Room room = player.tempData.room;
            //扣除生命值
            if (!room.playerDic.ContainsKey(attId)) {
                Debug.Log("MsgHit not Contains player " + attId,ConsoleColor.Red);
                return;
            }

            Player attPlayer = room.playerDic[attId];
            if (attPlayer == null)
                return;

            if (player.tempData.hp <= 0)
                return;
            player.tempData.hp -= damage;

            Debug.Log("[MsgHit] shooter: " + attId + "  hp:" + player.tempData.hp + " damage:" + damage,ConsoleColor.Green);
            
            //广播
            GameMessage broadMsg = new GameMessage();
            broadMsg.type = BitConverter.GetBytes((int)Protocol.Hit);
            HitInfo retInfo = new HitInfo();
            retInfo.attId = attId;
            retInfo.defId = player.id;
            retInfo.damage = damage;
            broadMsg.data = ProtoTransfer.Serialize(retInfo);
            room.BroadcastTcp(broadMsg);

            // 判断是否死亡
            if (player.tempData.hp <= 0) {
                // 更新比分
                room.UpdateScore(attPlayer);
                // 判断是否胜利
                int winTeam = room.IsWinByScore();
                room.UpdateWin(winTeam);

                if (winTeam == 0) {
                    // 隔一段时间复活玩家
                    room.ReLifePlayer(player);
                }

            }
        }

        // 完成战斗
        public void MsgCompleteFight(Session session, GameMessage message) {
            //解析协议
            Player player = session.player;
            player.tempData.status = PlayerTempData.Status.Room;

            // 回调
            GameMessage msgRet = new GameMessage();
            msgRet.type = BitConverter.GetBytes((int)Protocol.FightComplete);
            msgRet.data = BitConverter.GetBytes(0);

            session.SendTcp(msgRet);
        }
    }
}
