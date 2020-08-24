using LSGameServ.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fps_Player : MonoBehaviour {
    #region 人物属性
    public float hp = 200;
    #endregion

    #region 与人物相关的脚本
    [HideInInspector]
    public PlayerController playerCtrl;
    [HideInInspector]
    public PlayerAnimation playerAnim;
    [HideInInspector]
    public FPCamera fPCamera;
    [HideInInspector]
    public Weapon weapon;

    private CharacterController controller;
    private CapsuleCollider hitCollider;
    #endregion
    void Awake () {
        playerCtrl = GetComponent<PlayerController>();
        playerAnim = GetComponent<PlayerAnimation>();
        fPCamera = Camera.main.GetComponent<FPCamera>();

        BulletRigidPoolMgr.Singleton.FindOnHandWeapon();
        weapon = BulletRigidPoolMgr.Singleton.weapon;

        controller = GetComponent<CharacterController>();
        hitCollider = GetComponent<CapsuleCollider>();
    }
    
    private void Dead() {
        //改变操作模式
        playerCtrl.ctrlType = PlayerController.CtrlType.None;
        // 调整摄像机位置
        fPCamera.DeathView();
        // 播放死亡动画
        playerAnim.Death();
        // 失效碰撞器
        controller.enabled = false;
        hitCollider.enabled = false;
    }

    public void StartDrawKill() {
        EventCenter.Broadcast(EventID.ShowKillImg);
    }

    #region 网络相关
    /// <summary>
    /// 发送伤害信息
    /// </summary>
    /// <param name="id"></param>
    /// <param name="damage"></param>
    public void SendHit(string id, float damage) {
        GameMessage msg = new GameMessage();
        msg.type = System.BitConverter.GetBytes((int)Protocol.Hit);
        HitInfo hitInfo = new HitInfo();
        hitInfo.attId = id;
        hitInfo.damage = (int)Mathf.Round(damage);
        msg.data = ProtoTransfer.Serialize(hitInfo);
        NetMgr.GetInstance().tcpSock.Send(msg);
    }

    /// <summary>
    /// 伤害同步处理
    /// </summary>
    /// <param name="att"></param>
    /// <param name="attackTank"></param>
    public void NetBeAttacked(float att, GameObject attackTank) {
        //扣除生命值
        if (hp <= 0) return;

        hp -= att;
        hp = hp <= 0 ? 0 : hp;
        string ammoStr = weapon.GetAmmoStr();
        if (playerCtrl.ctrlType == PlayerController.CtrlType.Player) {
            // 显示子弹
            EventCenter.Broadcast(EventID.UpdateBattleText, ammoStr, ((int)hp).ToString());
        }
        //tank被击毁
        if (hp <= 0) {
            Dead();
            // 显示击杀提示
            if (attackTank) {
                Fps_Player player = attackTank.GetComponent<Fps_Player>();
                if (player != null && player.playerCtrl.ctrlType == PlayerController.CtrlType.Player) {
                    player.StartDrawKill();
                }
            }
        }
        
    }
    #endregion
}
