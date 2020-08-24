using LSGameServ.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.Server {
    class PlayerMgr {
        /// <summary>
        /// 单例
        /// </summary>
        public static PlayerMgr instance;

        public PlayerMgr() {

            instance = this;
        }

        /// <summary>
        /// 场景中的角色列表
        /// </summary>
        List<Player> playerList = new List<Player>();
        
        /// <summary>
        /// 添加玩家
        /// </summary>
        /// <param name="id">玩家id</param>
        public void AddPlayer(Player player) {
            //线程加锁
            lock (playerList) {
                playerList.Add(player);
            }
        }
        /// <summary>
        /// 获得玩家
        /// </summary>
        public Player GetPlayer(string id) {
            lock (playerList) {
                foreach(var p in playerList) {
                    if (p.id == id)
                        return p;
                }
                return null;
            }
        }
        /// <summary>
        /// 删除玩家
        /// </summary>
        public void DelPlayer(Player player) {
            //线程加锁
            lock (playerList) {
                if (playerList.Contains(player)) {
                    playerList.Remove(player);
                }
            }
            
        }
        
    }

}
