using LSGameServ.DB;
using LSGameServ.Net;
using LSGameServ.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSGameServ {
    class Program {
        static void Main(string[] args) {

            // 创建数据库管理
            DataMgr dataMgr = new DataMgr();
            // 创建玩家管理实例
            PlayerMgr playerMgr = new PlayerMgr();
            //创建房间管理器实例
            RoomMgr roomMgr = new RoomMgr();

            // 创建服务器实例
            GameServer serv = new GameServer();
            // 打开服务器
            serv.Start(7000,7001);

            while (true) {
                string str = Console.ReadLine();
                switch (str) {
                    case "quit":
                        serv.Close();
                        return;
                    case "print":
                        
                        break;
                }
            }
        }
    }
}
