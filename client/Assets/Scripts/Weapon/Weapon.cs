using LSGameServ.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum WeaponType {
    Rifle,
    Handgun,
    Shotgun,
    Smg,
    Sniper
}

[System.Serializable]
public class ShootSettings {
    //武器类型
    public WeaponType weaponType = WeaponType.Rifle;
    //子弹最大容量
    public int maxMess = 300;
    //一个弹夹的容量
    public int maxAmmo = 30;

    //子弹属性
    public float bulletMass = 1;

    public float bulletSpeed;  //子弹速度
    public float bulletLife;    //子弹生命周期

    //弹丸数量
    public int pellets = 1;
    //散射范围
    public float spreadSize =1;
    //射击频率
    public float shootRate = 500;

    //射击后坐力
    public float min_verVecoil = 2f;
    public float max_verVecoil = 5f;
    public float min_horVecoil = -1f;
    public float max_horVecoil = 1f;

    //枪支最大伤害
    public float maxHurtValue = 50f;
}

public class Weapon : MonoBehaviour
{
    //当前子弹数量
    int currentMess;
    //当前弹夹剩余数量
    int currentAmmo;
    
    public Transform firePos;
    public Transform MuzzleFlashPos;
    public GameObject MuzzleFlash;

    [Header("shootSettings")]
    public ShootSettings shootSettings;

    //射击等待时间
    float shootWaitTime;
    bool isFire = false;
    bool isReload = false;
    bool isRun = false;

    float lastFireTime = 0;
    
    //播放的音频
    public AudioClip shootClip;
    public AudioClip[] ReloadClipArr;
    
    
    AudioSource ads;
    Fps_PlayerParamter paramter;
    Fps_Player player;
    //射击相关参数
    float verOffset;
    float horOffset;

    float aimStartPosZ;
    void Start() {
        player = transform.root.GetComponent<Fps_Player>();
        player.playerAnim.Reload1Cb += Reload1;
        player.playerAnim.Reload2Cb += Reload2;

        ads = GetComponent<AudioSource>();
        paramter = player.GetComponent<Fps_PlayerParamter>();

        shootWaitTime = 60 / shootSettings.shootRate;
        aimStartPosZ = transform.localPosition.z;

        currentMess = shootSettings.maxMess;
        currentAmmo = shootSettings.maxAmmo;

        if (player.playerCtrl.ctrlType == PlayerController.CtrlType.Player) {
            string ammoStr = GetAmmoStr();
            // 显示子弹
            EventCenter.Broadcast(EventID.UpdateBattleText, ammoStr, ((int)player.hp).ToString());
        }
    }

    void OnDisable() {
        isReload = false;
        isFire = true;
    }

    
    void Update() {
        if (paramter.inputMenu) return;
        if (player.playerCtrl.ctrlType != PlayerController.CtrlType.Player) return;
        Reload();
        if (!isReload) {
            isRun = paramter.inputSprint&&paramter.inputMoveVector.y>0;
            Fire();
        } 
        
        
    }
    
    void Fire() {
        isFire = paramter.inputFire;
        
        GameObject instanceEffect = null;

        if (isFire
            //&&!isRun
            && lastFireTime + shootWaitTime < Time.time
            && currentAmmo > 0) {

            player.playerAnim.curAnimator.SetTrigger("Shoot");
            //播放音频
            ads.PlayOneShot(shootClip);

            //显示闪光
            instanceEffect = Instantiate(MuzzleFlash, MuzzleFlashPos.position, MuzzleFlashPos.rotation) as GameObject;
            instanceEffect.SetActive(true);
            
            //减少子弹数量
            currentAmmo--;
            // 更新子弹显示
            
            if (player.playerCtrl.ctrlType == PlayerController.CtrlType.Player) {
                string ammoStr = GetAmmoStr();
                // 显示子弹
                EventCenter.Broadcast(EventID.UpdateBattleText, ammoStr, ((int)player.hp).ToString());
            }

            //生成子弹实例
            Vector3 firePoint = firePos.position;
            if (shootSettings.weaponType != WeaponType.Shotgun) {
                CreateRigidBodyBullet(firePoint, Camera.main.transform.rotation);
            }
            
            // 发送同步信息
            SendShootInfo(firePoint, Camera.main.transform.rotation.eulerAngles);

            // 设置准星上调程度
            verOffset = Random.Range(shootSettings.min_verVecoil, shootSettings.max_verVecoil);
            horOffset = Random.Range(shootSettings.min_horVecoil, shootSettings.max_horVecoil);

            player.fPCamera.ShootOffSet(horOffset, verOffset, shootWaitTime);

            lastFireTime = Time.time;
        } else{
            //隐藏闪光
            if (instanceEffect)
                instanceEffect.SetActive(false);

        } 
        
    }

    void Reload() {
        if (paramter.inputReload
            && currentMess > 0
            && currentAmmo < shootSettings.maxAmmo && !isReload) {

            player.playerAnim.curAnimator.SetTrigger("Reload");
            // 发送换弹
            SendReload();

            isReload = true;
            
        }

        AnimatorStateInfo animInfo = player.playerAnim.curAnimator.GetCurrentAnimatorStateInfo(1);

        //判断是否换弹完成
        if (animInfo.IsName("Reload")&& shootSettings.weaponType != WeaponType.Shotgun ) {

            if (animInfo.normalizedTime >= 0.9f) {
                isReload = false;
            }

        } 

    }

    public void Reload1() {
        
        ads.PlayOneShot(ReloadClipArr[0]);

    }

    public void Reload2() {
        ads.PlayOneShot(ReloadClipArr[1]);
        InstallAmmo();
    }
    
    void InstallAmmo() {
        if (currentMess - shootSettings.maxAmmo >= 0) {
            //判断弹夹中是否还有子弹
            if (currentAmmo > 0) {
                currentMess -= currentAmmo;
                currentAmmo = shootSettings.maxAmmo;
            } else {
                currentAmmo = shootSettings.maxAmmo;
                currentMess -= shootSettings.maxAmmo;
            }

        } else {
            currentAmmo = currentMess;
        }
        
        if(player.playerCtrl.ctrlType == PlayerController.CtrlType.Player) {
            string ammoStr = GetAmmoStr();
            // 显示子弹
            EventCenter.Broadcast(EventID.UpdateBattleText, ammoStr, ((int)player.hp).ToString());
        }
       
    }

    void CreateRigidBodyBullet(Vector3 firePoint,Quaternion rotation) {
        //从对象池中获得子弹
        GameObject go = BulletRigidPoolMgr.Singleton.NextAviableBulletGo();

        if (go == null) return;
        BulletRigid bullet = go.GetComponent<BulletRigid>();
        
        bullet.Init(shootSettings, player.gameObject);

        bullet.Active(firePoint, rotation);
    }

    public string GetAmmoStr() {
        return currentAmmo + "/" + currentMess;
    }
    #region 网络部分
    // 发送子弹同步信息
    public void SendShootInfo(Vector3 pos,Vector3 rot) {
        GameMessage message = new GameMessage();
        message.type = System.BitConverter.GetBytes((int)Protocol.Shooting);
        ShootInfo shootInfo = new ShootInfo();
        shootInfo.posX = pos.x;
        shootInfo.posY = pos.y;
        shootInfo.posZ = pos.z;

        shootInfo.rotateX = rot.x;
        shootInfo.rotateY = rot.y;
        shootInfo.rotateZ = rot.z;

        message.data = ProtoTransfer.Serialize(shootInfo);
        NetMgr.GetInstance().tcpSock.Send(message);

    }

    // 发送换弹信息
    public void SendReload() {
        GameMessage message = new GameMessage();
        message.type = System.BitConverter.GetBytes((int)Protocol.Reload);
        message.data = System.Text.Encoding.UTF8.GetBytes(GameMgr._instance.id);
        NetMgr.GetInstance().tcpSock.Send(message);
    }

    // 同步子弹
    public void NetShoot(Vector3 pos,Vector3 rot) {
        player.playerAnim.curAnimator.SetTrigger("Shoot");
        
        //显示闪光
        GameObject instanceEffect = Instantiate(MuzzleFlash, MuzzleFlashPos.position, MuzzleFlashPos.rotation) as GameObject;
        instanceEffect.SetActive(true);
        
        //生成子弹实例
        if (shootSettings.weaponType != WeaponType.Shotgun) {
            CreateRigidBodyBullet(pos, Quaternion.Euler(rot));
        }
    }

    public void NetReload() {
        player.playerAnim.curAnimator.SetTrigger("Reload");
    }

    #endregion
}

