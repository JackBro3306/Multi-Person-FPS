using LSGameServ.Net;
using LSGameServ.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ.DB {
    public class PlayerTempData {
        public enum Status {
            None,
            Room,
            Fight,
        }

        public Status status;

        public Room room;
        public int team = 1;
        public int swopId = 1;
        public bool isOwner = false;

        /// <summary>
        /// 战场相关
        /// </summary>
        public long lastUpdateTime;     //上次更新位置的时间

        
        public float posX;              //玩家坐标
        public float posY;
        public float posZ;

        public long lastShootTime;      //上次射击的时间
        public float hp = 100;

        public PlayerTempData() {
            status = Status.None;
        }
    }
}
