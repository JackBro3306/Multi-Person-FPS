using LSGameServ.DB;
using LSGameServ.Net;
using LSGameServ.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LSGameServ.Server {
    public class Room {
        
        /// <summary>
        /// 房间状态
        /// </summary>
        public enum Status {
            Prepare = 1,
            Fight = 2,
        }

        /// <summary>
        /// 开始战斗结果
        /// </summary>
        public enum Start2Fight {
            Success = 0,        //成功
            NotOwner = 1,       // 不是房主
            Fighting = 2,       // 房间正在战斗中
            NotExitBattle = 3,  // 还有玩家未退出战斗
            NotEnoughPlayer = 4,    //人数不足
        }
        /// <summary>
        /// 房间状态
        /// </summary>
        public Status status = Status.Prepare;
        /// <summary>
        /// 最大玩家数
        /// </summary>
        public int maxPlayers = 6;
        /// <summary>
        /// 房间内的玩家列表
        /// </summary>
        public Dictionary<string, Player> playerDic = new Dictionary<string, Player>();
        /// <summary>
        /// 比分
        /// </summary>
        private int scoreT1 = 0;
        private int scoreT2 = 0;
        private int winScore = 30;
        /// <summary>
        /// 对局时间
        /// </summary>
        const int battleLimitTime = 1000 * 60*5;
        public System.Timers.Timer battleTimer = new System.Timers.Timer(battleLimitTime);

        #region udp
        
        private bool start = false;
        private uint frameIndex = 1;
        private byte[] mbytes = new byte[1024 * 1024];
        private Timer runTimer = null;
        /// <summary>
        /// 消息队列
        /// </summary>
        private Queue<byte[]> messageQueue;
        /// <summary>
        /// 玩家的连接信息
        /// </summary>
        private Dictionary<string, IPEndPoint> mConnectDic;
        #endregion
        /// <summary>
        /// 初始化房间
        /// </summary>
        void Init() {
            messageQueue.Clear();
            frameIndex = 1;
        }

        /// <summary>
        /// 添加玩家
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool AddPlayer(Player player) {
            //线程加锁
            lock (playerDic) {
                //房间满人
                if (playerDic.Count >= maxPlayers) return false;
                PlayerTempData tempData = player.tempData;
                tempData.room = this;
                //根据房间人数分配队伍
                tempData.team = SwitchTeam();
                tempData.status = PlayerTempData.Status.Room;

                if (playerDic.Count == 0)
                    tempData.isOwner = true;
                string id = player.id;
                if (playerDic.ContainsKey(id)) return false;

                playerDic.Add(id, player);
            }

            return true;
        }

        /// <summary>
        /// 分配队伍
        /// </summary>
        /// <returns></returns>
        public int SwitchTeam() {
            int count1 = 0;
            int count2 = 0;
            foreach(Player player in playerDic.Values) {
                if (player.tempData.team == 1) count1++;
                if (player.tempData.team == 2) count2++;
            }

            //返回人数较少的阵营
            return count1 <= count2 ? 1 : 2;
        }

        /// <summary>
        /// 删除玩家
        /// </summary>
        /// <param name="id"></param>
        public void DelPlayer(string id) {
            lock (playerDic) {
                if (!playerDic.ContainsKey(id)) return;
                bool isOwner = playerDic[id].tempData.isOwner;
                PlayerTempData tempData = playerDic[id].tempData;
                playerDic[id].tempData.status = PlayerTempData.Status.None;
                
                playerDic.Remove(id);
                //如果房主离开房间，重新选取房主
                if (isOwner) {
                    // 重置房主
                    tempData.isOwner = false;

                    UpdateOwner();
                }
            }
        }

        /// <summary>
        /// 选取房主
        /// </summary>
        public void UpdateOwner() {
            lock (playerDic) {
                if (playerDic.Count <= 0) return;

                //把所有玩家的isOwner设为false
                foreach (Player player in playerDic.Values) {
                    player.tempData.isOwner = false;
                }

                //将列表中的第一位玩家设为房主
                Player p = playerDic.Values.First();
                p.tempData.isOwner = true;
            }
        }

        /// <summary>
        /// Tcp协议广播
        /// </summary>
        /// <param name="protocol"></param>
        public void BroadcastTcp(GameMessage message) {
            lock (playerDic) {
                Dictionary<string,Player>.Enumerator enumerator = playerDic.GetEnumerator();
                while (enumerator.MoveNext()) {
                    enumerator.Current.Value.session.SendTcp(message);
                }
                enumerator.Dispose();
            }
        }

        /// <summary>
        /// 获得房间信息
        /// </summary>
        /// <returns></returns>
        public GameMessage GetRoomInfo() {
            GameMessage message = new GameMessage();
            message.type = BitConverter.GetBytes((int)Protocol.GetRoomInfo);
            List<RoomPlayer> roomPlayers = new List<RoomPlayer>();
            
            //每个玩家的信息
            foreach (Player p in playerDic.Values) {
                RoomPlayer roomPlayer = new RoomPlayer();
                PlayerInfo playerinfo = new PlayerInfo();
                playerinfo.id=p.id;
                playerinfo.win = p.data.win;
                playerinfo.defeat = p.data.defeat;
                roomPlayer.playerinfo = playerinfo;

                roomPlayer.team = p.tempData.team;
                int isOwner = p.tempData.isOwner ? 1 : 0;
                roomPlayer.isOwner = isOwner;

                roomPlayers.Add(roomPlayer);
            }
            
            message.data = ProtoTransfer.Serialize<List<RoomPlayer>>(roomPlayers);

            return message;
        }

        /// <summary>
        /// 房间能否开战
        /// </summary>
        /// <returns></returns>
        public Start2Fight CanStart() {
            if (status != Status.Prepare) return Start2Fight.Fighting;

            int count1 = 0;
            int count2 = 0;

            foreach (Player player in playerDic.Values) {
                // 当玩家未退出到房间时，不能开战
                if (player.tempData.status == PlayerTempData.Status.Fight) return Start2Fight.NotExitBattle;
                if (player.tempData.team == 1) count1++;
                if (player.tempData.team == 2) count2++;
                
            }

            if (count1 < 1 || count2 < 1) return Start2Fight.NotEnoughPlayer;

            
            return Start2Fight.Success;
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartFight() {
            scoreT1 = 0;
            scoreT2 = 0;
            status = Status.Fight;
            GameMessage msg = new GameMessage();
            msg.type = BitConverter.GetBytes((int)Protocol.Fight);

            var  battleinfos =new List<BattlePlayer>();

            // 开始计时
            battleTimer.Elapsed += (sender, e) => {
                int winTeam = scoreT1 >= scoreT2 ? 1 : 2;
                UpdateWin(winTeam);
            };
            battleTimer.AutoReset = false;
            battleTimer.Enabled = true;

            int teamPos1 = 1;
            int teamPos2 = 1;
            lock (playerDic) {
                
                foreach (Player p in playerDic.Values) {
                    BattlePlayer battlePlayer = new BattlePlayer();
                    
                    p.tempData.hp = 100;
                    battlePlayer.id = p.id;
                    battlePlayer.team = p.tempData.team;
                    if (p.tempData.team == 1) {
                        p.tempData.swopId = teamPos1;
                        battlePlayer.swopId = teamPos1++;
                    } else {
                        p.tempData.swopId = teamPos2;
                        battlePlayer.swopId = teamPos2++;
                    }
                    battlePlayer.playerHp = (int)p.tempData.hp;
                    p.tempData.status = PlayerTempData.Status.Fight;
                    
                    battleinfos.Add(battlePlayer);
                }

                // 对局时间

                byte[] battlebytes = BitConverter.GetBytes(battleLimitTime);
                byte[] tempBytes = ProtoTransfer.Serialize(battleinfos);
                byte[] sendbytes = new byte[tempBytes.Length+ battlebytes.Length];
                Array.Copy(battlebytes,0, sendbytes,0, battlebytes.Length);
                Array.Copy(tempBytes,0, sendbytes, battlebytes.Length,tempBytes.Length);
                msg.data = sendbytes;
                //广播Fight协议
                BroadcastTcp(msg);

               // GameStar();
            }
        }
        #region udp
        /// <summary>
        /// 绑定玩家的udp连接地址
        /// </summary>
        public void BindUdpConnect(string userid,IPEndPoint point) {
            mConnectDic[userid] = point;
        }
        /// <summary>
        /// 对局开始
        /// </summary>
        void GameStar() {
            Init();
            start = true;

            // 20毫秒发送一次逻辑帧
            // 客户端10毫秒执行一次逻辑帧操作
            // 10毫秒执行一次渲染帧操作
            runTimer = new Timer(TimerFunc, null, 0, 20);
        }
        void TimerFunc(object obj) {
            if (!start) return;
            lock (messageQueue) {
                uint frame = frameIndex;
                byte[] framebytes = BitConverter.GetBytes(frame);
                Array.Copy(framebytes,0,mbytes,0,framebytes.Length);
                int index = framebytes.Length;
                while (messageQueue.Count > 0) {
                    byte[] tempbytes = messageQueue.Dequeue();
                    Array.Copy(tempbytes,0,mbytes,index,tempbytes.Length);
                    index += tempbytes.Length;
                }
                
                //发送到各个客户端
                foreach (var item in mConnectDic) {
                    IPEndPoint point = item.Value;
                    byte[] sendbytes = new byte[index];
                    Array.Copy(mbytes,0,sendbytes,0,index);
                    MsgInfo msg = new MsgInfo(point,sendbytes,sendbytes.Length);
                    // 发送两个冗余包，防止丢包
                    GameServer.instance.udp.Send(msg);
                    GameServer.instance.udp.Send(msg);
                    GameServer.instance.udp.Send(msg);
                }

                frameIndex++;
            }
        }
        /// <summary>
        /// 接收udp数据
        /// </summary>
        public void ReceiveUdp(byte[] bytes) {
            if (!start) return;
            lock (messageQueue)
                messageQueue.Enqueue(bytes);
        }
        /// <summary>
        /// 对局结束
        /// </summary>
        public void GameComplete() {
            runTimer.Dispose();
            start = false;
        }
        #endregion
        /// <summary>
        /// 根据人数判断胜负
        /// </summary>
        /// <returns></returns>
        private int IsWinByPlayerCount() {
            if (status != Status.Fight) return 0;

            int count1 = 0;
            int count2 = 0;

            foreach (Player player in playerDic.Values) {
                PlayerTempData pt = player.tempData;
                if (pt.team == 1 && pt.hp > 0) count1++;
                if (pt.team == 2 && pt.hp > 0) count2++;

            }

            if (count1 <= 0) return 2;
            if (count2 <= 0) return 1;

            return 0;
        }

        /// <summary>
        /// 根据分数判断胜负
        /// </summary>
        /// <returns></returns>
        public int IsWinByScore() {
            if (scoreT1 >= winScore) return 1;
            if (scoreT2 >= winScore) return 2;
            return 0;
        }

        /// <summary>
        /// 处理战斗结果
        /// </summary>
        public void UpdateWin(int isWin) {

            if (isWin == 0) return;
            //改变状态 数值处理
            lock (playerDic) {
                //改变房间状态
                status = Status.Prepare;
                foreach (Player player in playerDic.Values) {

                    if (player.tempData.team == isWin)
                        player.data.win++;
                    else
                        player.data.defeat++;
                }
                GameMessage msg = new GameMessage();
                // 广播结果协议
                msg.type = BitConverter.GetBytes((int)Protocol.GameResult);
                msg.data = BitConverter.GetBytes(isWin);
                BroadcastTcp(msg);
                
            }
        }

        /// <summary>
        /// 更新比分
        /// </summary>
        public void UpdateScore(Player player) {
            if (player.tempData.team == 1)
                scoreT1++;

            else if (player.tempData.team == 2)
                scoreT2++;

            // 广播更新比分的消息
            GameMessage broadMsg = new GameMessage();
            broadMsg.type = BitConverter.GetBytes((int)Protocol.UpdateScore);
            BattleScore battleScore = new BattleScore();
            battleScore.scoreT1 = scoreT1;
            battleScore.scoreT2 = scoreT2;
            broadMsg.data = ProtoTransfer.Serialize(battleScore);
            BroadcastTcp(broadMsg);

        }

        // 中途退出战斗
        public void ExitFight(Player player) {
            //摧毁坦克
            if (playerDic[player.id] != null)
                playerDic[player.id].tempData.hp = -1;

            //广播消息
            GameMessage broadMsg = new GameMessage();
            broadMsg.type = BitConverter.GetBytes((int)Protocol.Hit);
            HitInfo retInfo = new HitInfo();
            retInfo.attId = player.id;
            retInfo.defId = player.id;
            retInfo.damage = 999;
            broadMsg.data = ProtoTransfer.Serialize(retInfo);

            BroadcastTcp(broadMsg);
            //增加失败次数
            if (IsWinByPlayerCount() == 0)
                player.data.defeat++;

            int isWin = IsWinByPlayerCount();
            
            UpdateWin(isWin);
        }

        // 复活玩家
        public void ReLifePlayer(Player player) {

            System.Timers.Timer relifeTimer = new System.Timers.Timer(5000);

            // 5秒后执行
            relifeTimer.Elapsed += (sender, e) => {

                if (playerDic[player.id] != null) {
                    GameMessage broadMsg = new GameMessage();
                    broadMsg.type = BitConverter.GetBytes((int)Protocol.RelifePlayer);

                    player.tempData.hp = 100;
                    BattlePlayer battlePlayer = new BattlePlayer();
                    battlePlayer.id = player.id;
                    battlePlayer.team=player.tempData.team;
                    battlePlayer.swopId = player.tempData.swopId;
                    battlePlayer.playerHp = ((int)player.tempData.hp);

                    broadMsg.data = ProtoTransfer.Serialize(battlePlayer);
                    BroadcastTcp(broadMsg);
                }
            };

            relifeTimer.AutoReset = false;
            relifeTimer.Enabled = true;

        }
    }
}
