using LSGameServ.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnit {
    public Fps_Player player;
    public int camp;
}

public class MutiBattle : MonoBehaviour {

    // 单例
    public static MutiBattle _instance;
    private void Awake() {
        _instance = this;
    }

    // 玩家预设
    public GameObject[] playerPrefabGos;
    public Dictionary<string, BattleUnit> unitDic = new Dictionary<string, BattleUnit>();

    // 获取阵营
    public int GetCamp(GameObject playerObj) {
        foreach (BattleUnit mt in unitDic.Values) {
            if (mt.player.gameObject == playerObj)
                return mt.camp;
        }

        return 0;
    }

    // 是否同一阵营
    public bool IsSameCamp(GameObject player1,GameObject player2) {
        return GetCamp(player1) == GetCamp(player2) ;
    }

    // 清理战场
    public void ClearBattle() {
        unitDic.Clear();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i=0;i<players.Length;i++) {
            Destroy(players[i]);
        }
    }

    // 开始战斗
    public void StartBattle(GameMessage message) {
        Protocol msgType = (Protocol)message.type[0];
        if (msgType != Protocol.Fight) return;
        byte[] tempbytes = new byte[sizeof(Int32)];
        System.Array.Copy(message.data,0, tempbytes,0,sizeof(Int32));
        // 对局时间
        int battleLimitTime = System.BitConverter.ToInt32(tempbytes,0);

        // 显示主UI
        PanelMgr._instance.OpenPanel<BattleMainPanel>("", battleLimitTime);
        byte[] data = new byte[message.data.Length - tempbytes.Length];
        System.Array.Copy(message.data, tempbytes.Length, data, 0, data.Length);
        // 清理场景
        ClearBattle();
        List<BattlePlayer> battlePlayers = ProtoTransfer.Deserialize<List<BattlePlayer>>(data);
        foreach (var bp in battlePlayers) {
            string id = bp.id;
            int team = bp.team;
            int swopID = bp.swopId;
            int hp = bp.playerHp;
            GeneratePlayer(id,team,swopID,hp);
        }

        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.UpdateUnitInfo, RecvUpdateUnitInfo);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.Shooting, RecvShooting);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.Reload, RecvReload);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.Hit, RecvHit);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.GameResult, RecvResult);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.RelifePlayer, RecvRelifePlayer);
    }

    // 生成玩家
    public void GeneratePlayer(string id,int team,int swopID,int hp) {
        //获取出生点
        Transform sp = GameObject.Find("SwopPoints").transform;
        Transform swopTran;
        if (team == 1) {
            Transform teamSwop = sp.GetChild(0);
            swopTran = teamSwop.GetChild(swopID - 1);
        } else {
            Transform teamSwop = sp.GetChild(1);
            swopTran = teamSwop.GetChild(swopID - 1);
        }

        if (swopTran == null) {
            Debug.LogError("GeneratePlayer出生点错误！");
            return;
        }

        //预设
        if (playerPrefabGos.Length < 2) {
            Debug.LogError("玩家预设数量不够");
            return;
        }

        //生成玩家实例
        GameObject tankObj = (GameObject)Instantiate(playerPrefabGos[team - 1]);
        tankObj.name = id;
        tankObj.transform.position = swopTran.position;
        tankObj.transform.rotation = swopTran.rotation;

        //列表处理
        BattleUnit bu = new BattleUnit();
        bu.player = tankObj.GetComponent<Fps_Player>();
        bu.camp = team;
        bu.player.hp = hp;
        unitDic.Add(id, bu);
        //玩家处理
        if (id == GameMgr._instance.id) {
            bu.player.playerCtrl.ctrlType = PlayerController.CtrlType.Player;
            FPCamera fpCamera= Camera.main.GetComponent<FPCamera>();
            GameObject target = bu.player.gameObject;
            fpCamera.SetTarget(target);
        } else {
            bu.player.playerCtrl.ctrlType = PlayerController.CtrlType.Net;
            //初始化网络同步
            bu.player.playerCtrl.InitNetCtrl();
        }
    }

    // 处理位置同步
    public void RecvUpdateUnitInfo(GameMessage message) {
        //解析协议
        UnitInfo unitInfo = ProtoTransfer.Deserialize<UnitInfo>(message.data);
        string id = unitInfo.id;
        Vector3 nPos;
        Vector3 nRot;

        nPos.x = unitInfo.posX;
        nPos.y = unitInfo.posY;
        nPos.z = unitInfo.posZ;

        nRot.x = unitInfo.rotateX;
        nRot.y = unitInfo.rotateY;
        nRot.z = unitInfo.rotateZ;
        

        //处理
        Debug.Log("RecvUpdateUnitInfo" + id);
        if (!unitDic.ContainsKey(id)) {
            Debug.Log("RecvUpdateUnitInfo bt == null");
            return;
        }

        BattleUnit bt = unitDic[id];
        if (id == GameMgr._instance.id)
            return;

        bt.player.playerCtrl.NetForecastInfo(nPos, nRot);
        bt.player.playerCtrl.NetMoveState((PlayerState)unitInfo.moveState,unitInfo.input_h,unitInfo.input_v,(PlayerState)unitInfo.moveState);
    }

    // 处理子弹同步
    public void RecvShooting(GameMessage message) {
        ShootInfo shootInfo = ProtoTransfer.Deserialize<ShootInfo>(message.data);
        //解析协议
        string id = shootInfo.id;

        Vector3 pos;
        Vector3 rot;
        pos.x = shootInfo.posX;
        pos.y = shootInfo.posY;
        pos.z = shootInfo.posZ;

        rot.x = shootInfo.rotateX;
        rot.y = shootInfo.rotateY;
        rot.z = shootInfo.rotateZ;

        //处理
        if (!unitDic.ContainsKey(id)) {
            Debug.Log("RecvShooting bt == null");
            return;
        }
        BattleUnit bu = unitDic[id];
        if (id == GameMgr._instance.id) return;
        bu.player.weapon.NetShoot(pos, rot);
    }

    // 处理换弹
    public void RecvReload(GameMessage message) {
        string id = System.Text.Encoding.UTF8.GetString(message.data, 0, message.data.Length);

        //处理
        if (!unitDic.ContainsKey(id)) {
            Debug.Log("RecvReload bt == null");
            return;
        }
        BattleUnit bu = unitDic[id];
        if (id == GameMgr._instance.id) return;
        bu.player.weapon.NetReload();
    }

    
    // 同步伤害信息
    public void RecvHit(GameMessage message) {
        //解析协议
        HitInfo hitInfo = ProtoTransfer.Deserialize<HitInfo>(message.data);
        string attId = hitInfo.attId;
        string defId = hitInfo.defId;

        float hurt = hitInfo.damage;
        //获取BattleTank
        if (!unitDic.ContainsKey(attId)) {
            Debug.Log("RecvHit attBt == null " + attId);
            return;
        }

        BattleUnit attBu = unitDic[attId];
        if (!unitDic.ContainsKey(defId)) {
            Debug.Log("RecvHit defBt == null " + defId);
            return;
        }
        BattleUnit defBu = unitDic[defId];
        //被击中的玩家
        defBu.player.NetBeAttacked(hurt, attBu.player.gameObject);
    }

    /// <summary>
    /// 复活玩家协议回调
    /// </summary>
    /// <param name="protocol"></param>
    public void RecvRelifePlayer(GameMessage message) {
        BattlePlayer battlePlayer = ProtoTransfer.Deserialize<BattlePlayer>(message.data);
        string id = battlePlayer.id;
        int team = battlePlayer.team;
        int swopID = battlePlayer.swopId;
        int hp = battlePlayer.playerHp;

        if (!unitDic.ContainsKey(id)) {
            Debug.Log("RelifeTank == null " + id);
            return;
        }

        // 销毁当前玩家
        Destroy(unitDic[id].player.gameObject);

        unitDic.Remove(id);

        // 重新生成玩家
        GeneratePlayer(id, team, swopID, hp);
    }

    /// <summary>
    /// 处理战斗结果
    /// </summary>
    /// <param name="protocol"></param>
    public void RecvResult(GameMessage message) {
        
        int winTeam = System.BitConverter.ToInt32(message.data,0) ;
        //弹出胜负面板
        string id = GameMgr._instance.id;
        BattleUnit bu = unitDic[id];

        if (bu.camp == winTeam) {
            PanelMgr._instance.OpenPanel<WinPanel>("", 1);
        } else {
            PanelMgr._instance.OpenPanel<WinPanel>("", 0);
        }
        
        //取消监听
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.UpdateUnitInfo, RecvUpdateUnitInfo);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.Shooting, RecvShooting);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.Reload, RecvReload);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.Hit, RecvHit);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.GameResult, RecvResult);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.RelifePlayer, RecvRelifePlayer);

        // 清除玩家
        ClearBattle();
    }
}
