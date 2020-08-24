using System;
using System.IO;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using LSGameServ.Net;
using LSGameServ.Server;

namespace LSGameServ.DB {
    public class DataMgr {
        MySqlConnection sqlConn;

        //单例模式
        public static DataMgr instance;
        public DataMgr() {
            instance = this;
            Connect();
        }

        //连接
        public void Connect() {
            //数据库
            string connStr = "Database=game;Data Source=127.0.0.1;";
            connStr += "User Id=root;Password=123456;port=3306";
            sqlConn = new MySqlConnection(connStr);

            try {
                sqlConn.Open();
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]Connect"+e.Message);
                return;
            }
        }

        //防止sql注入，判断安全字符串
        public bool IsSafeStr(string str) {
            
            return !Regex.IsMatch(str,@"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']");
        }

        //是否存在该用户
        private bool CanRegister(string id) {
            //防止sql注入
            if (!IsSafeStr(id))
                return false;
            //查询id是否存在
            string cmdStr = string.Format("select * from user where id='{0}';",id);
            MySqlCommand cmd = new MySqlCommand(cmdStr,sqlConn);

            try {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                //结果集是否为空
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return !hasRows;
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]CanRegister fail"+e.Message);
                return false;
            }
        }

        //注册
        public bool Register(string id,string pw) {
            //防止sql注入
            if (!IsSafeStr(id) || !IsSafeStr(pw)) {
                Console.WriteLine("[DataMgr]Register 使用非法字符");
                return false;
            }

            //判断是否能注册
            if (!CanRegister(id)) {
                Console.WriteLine("[DataMgr]Register !CanRegister");
                return false;
            }

            //写入数据库User表
            string cmdStr = string.Format("insert into user set id='{0}',pw='{1}';",id,pw);
            MySqlCommand cmd = new MySqlCommand(cmdStr,sqlConn);
            try {
                cmd.ExecuteNonQuery();
                return true;
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]Register"+e.Message);
                return false;
            }

        }

        //创建角色
        public bool CreatePlayer(string id) {
            //防止sql注入
            if (!IsSafeStr(id))
                return false;
            //序列化
            IFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            PlayerData playerData = new PlayerData();
            try {
                formatter.Serialize(stream, playerData);
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]CreatePlayer 序列化"+e.Message);
                return false;
            }

            //将序列化后的字节流转化为字节数组
            byte[] byteArr = stream.ToArray();

            //写入数据库
            /**
             * 由于不方便直接使用带byte[]参数的sql语句，程序使用cmd.Parameters给sql语句添加参数
             * string.Format("insert into player set id='{0}',data=@data;",id)中的@data代表参数名
             * 程序会从cmd的参数列表中找到名为@data的参数并填入sql语句中
             * cmd.Parameters.Add("@data",MySqlDbType.Blob);表示给cmd添加一个名为@data的二进制数据（Blob）参数
             * 并使用cmd.Parameters[0].Value = byteArr;进行赋值
             * 
             * */
            string cmdStr = string.Format("insert into player set id='{0}',data=@data;",id);
            MySqlCommand cmd = new MySqlCommand(cmdStr,sqlConn);
            cmd.Parameters.Add("@data",MySqlDbType.Blob);
            cmd.Parameters[0].Value = byteArr;

            try {
                cmd.ExecuteNonQuery();
                return true;
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]CreatePlayer 写入"+e.Message);
                return false;
            }
        }

        //登陆校验
        public bool CheckPassword(string id,string pw) {
            //防止sql注入
            if (!IsSafeStr(id) || !IsSafeStr(pw)) {
                Console.WriteLine("[DataMgr]Register 使用非法字符");
                return false;
            }
            //查询
            string cmdStr = string.Format("select * from user where id='{0}' and pw='{1}';",id,pw);
            MySqlCommand cmd = new MySqlCommand(cmdStr,sqlConn);
            try {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return hasRows;
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]CheckPassword "+e.Message);
                return false;
            }
        }

        //获取玩家数据
        public PlayerData GetPlayerData(string id) {
            PlayerData playerData = null;
            //防止sql注入
            if (!IsSafeStr(id))
                return playerData;
            //查询
            string cmdStr = string.Format("select * from player where id='{0}';",id);
            MySqlCommand cmd = new MySqlCommand(cmdStr,sqlConn);
            byte[] buffer = new byte[1];
            try {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                if (!dataReader.HasRows) {
                    dataReader.Close();
                    return playerData;
                }
                dataReader.Read();
                long len = dataReader.GetBytes(1,0,null,0,0);   //0是id，1是data
                buffer = new byte[len];
                dataReader.GetBytes(1,0,buffer,0,(int)len);
                dataReader.Close();
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]GetPlayerData 查询 "+e.Message);
                return playerData;
            }

            //反序列化
            MemoryStream stream = new MemoryStream(buffer);
            try {
                BinaryFormatter formatter = new BinaryFormatter();
                playerData = (PlayerData)formatter.Deserialize(stream);
                return playerData;
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]GetPlayerData 反序列化 "+e.Message);
                return playerData;
            }

        }

        //保存角色
        public bool SavePlayer(Player player) {
            string id = player.id;
            PlayerData playerData = player.data;
            //序列化
            IFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            try {
                formatter.Serialize(stream,playerData);
            }catch(Exception e) {
                Console.WriteLine("[DataMgr]SavePlayer 序列化 "+e.Message);
            }

            byte[] byteArr = stream.ToArray();
            //写入数据库
            string formatStr = "update player set data=@data where id='{0}';";
            string cmdStr = string.Format(formatStr,id);
            MySqlCommand cmd = new MySqlCommand(cmdStr,sqlConn);
            cmd.Parameters.Add("@data",MySqlDbType.Blob);
            cmd.Parameters[0].Value = byteArr;
            try {
                cmd.ExecuteNonQuery();
                return true;
            } catch (Exception e) {
                Console.WriteLine("[DataMgr]SavePlayer 写入"+e.Message);
                return false;
            }
        }

    }
}
