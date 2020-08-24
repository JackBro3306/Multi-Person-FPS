using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSGameServ.DB;
using LSGameServ.EventDispatcher;
using LSGameServ.Net;
using LSGameServ.Protobuf;
using LSGameServ.Server;

namespace LSGameServ.MsgHandle {
    /*
     * 处理连接协议
     * */
    public class HandleConnMsg : IRecevieHandle {
        
        // 注册监听事件
        public void RegistListener() {
            EventCenter.AddEventListener<Session,GameMessage>(Protocol.HurtBeat,MsgHeartBeat);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.Regist, MsgRegister);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.Login, MsgLogin);
            EventCenter.AddEventListener<Session, GameMessage>(Protocol.Logout, MsgLogout);
        }

        public void UnRegistListener() {
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.HurtBeat, MsgHeartBeat);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.Regist, MsgRegister);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.Login, MsgLogin);
            EventCenter.RemoveEventListener<Session, GameMessage>(Protocol.Logout, MsgLogout);
        }

        /*
         * 心跳
         * 协议参数：无
         * 
         * */
        public void MsgHeartBeat(Session session, GameMessage message) {
            
            session.lastTickTime = Sys.GetTimeStamp();
            Debug.Log("[更新心跳时间]"+ session.GetAddress(),ConsoleColor.Green);
        }

        /*
         * 注册
         * 协议参数：str用户名，str密码
         * 返回协议：-1表示失败 0表示成功
         * 
         * */
        public void MsgRegister(Session session, GameMessage message) {
            //获得数值
            ReqLogin req = ProtoTransfer.Deserialize<ReqLogin>(message.data);
            
            string strFormat = "[收到注册协议]" + session.GetAddress();
            Debug.Log(strFormat +" 用户名："+ req.account+ " 密码："+ req.password);

            //构建返回协议
            int result = -1;
            
            //注册
            if (DataMgr.instance.Register(req.account, req.password)) {
                result = 0;
            }

            byte[] bytes = BitConverter.GetBytes(result);
            //创建角色
            DataMgr.instance.CreatePlayer(req.account);
            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int)Protocol.Regist);
            retMsg.data = BitConverter.GetBytes(result);
            
            //返回协议给客户端
            session.SendTcp(retMsg);
        }

        /*
         * 登陆
         * 协议参数：str用户名，str密码
         * 返回协议：-1表示失败 0表示成功
         * 
         * */
         public void MsgLogin(Session session, GameMessage message) {
            ReqLogin reqLogin = ProtoTransfer.Deserialize<ReqLogin>(message);

            string id = reqLogin.account;
            string pw = reqLogin.password;
            Debug.Log(string.Format("[收到登陆协议]{0} 用户名：{1} 密码：{2}" , session.GetAddress(), id, pw));
            //构建返回协议
            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int)Protocol.Login);
            //验证
            if (!DataMgr.instance.CheckPassword(id, pw)) {
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
                return;
            }
            //是否已经登陆
            GameMessage logoutMsg = new GameMessage();
            logoutMsg.type = BitConverter.GetBytes((int)Protocol.Logout);

            if (!Player.KickOff(id, logoutMsg)) {
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
                return;
            }
            //获得玩家数据
            PlayerData playerData = DataMgr.instance.GetPlayerData(id);
            if(playerData == null) {
                retMsg.data = BitConverter.GetBytes(-1);
                session.SendTcp(retMsg);
                return;
            }
            session.player =new  Player(id, session);
            session.player.data = playerData;
            //事件触发
            GameServer.instance.playerEvent.OnLogin(session.player);
            //返回
            retMsg.data = BitConverter.GetBytes(0);
            session.SendTcp(retMsg);

        }

        /*
         * 下线
         * 协议参数：无
         * 返回协议：0正常下线
         * 
         * */
        public void MsgLogout(Session session, GameMessage message) {
            GameMessage retMsg = new GameMessage();
            retMsg.type = BitConverter.GetBytes((int)Protocol.Logout);

            retMsg.data = BitConverter.GetBytes(0);
            if (session.player == null) {
                session.SendTcp(retMsg);
                session.Close();
            } else {
                session.SendTcp(retMsg);
                session.player.Logout();
            }
        }
    }
}
