using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletRigid : MonoBehaviour
{
    public float speed = 5f;

    public float life = 3f;
   
    //武器属性
    private ShootSettings shootSettings;
    
    Rigidbody rigi;

    bool isAlife = false;

    float lifeTimer;
    //射击者
    GameObject shoter;
    // Start is called before the first frame update
    void Awake()
    {
        rigi = GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlife) {

            if(lifeTimer > life) {
                DisActive();
                lifeTimer = 0;
            }

            lifeTimer += Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision pOther) {
        
        
        string tag = pOther.collider.tag;
        if (tag == Tags.bullet|| pOther.gameObject == shoter) return;

        ContactPoint cantact = pOther.contacts[0];

        //判断击中材质类型
        GameObject impact = BulletRigidPoolMgr.Singleton.GetImpact("ConcreteImpact");
        switch (tag) {
            case Tags.player:
            case Tags.enemy:
            case Tags.unitHead:
            case Tags.unitBody:
            case Tags.unitLimbs:
                impact = BulletRigidPoolMgr.Singleton.GetImpact("BloodImpact");
                break;
        }

        //生成弹孔
        GameObject go = Instantiate(impact);

        go.transform.position = cantact.point;
        go.transform.localRotation = Quaternion.LookRotation(cantact.normal);
        go.transform.parent = pOther.collider.transform;

        Destroy(go, 10f);

        DisActive();
        
        //unit.ApplyDamage(injury);
        Fps_Player player = pOther.gameObject.GetComponent<Fps_Player>();
        /**
         * 多个客户端同时执行该方法，
         * 只有在受击者的用户名与该客户端的用户名相同时才发送伤害信息
         * */
        if(player !=null && player.name == GameMgr._instance.id) {
            float att = GetApplyInjury(player.tag);
            // 发送伤害信息
            player.SendHit(shoter.name, att);
        }

    }


    public void Init(ShootSettings settings,GameObject go) {
        shoter = go;
        SetPropertices(settings);
    }

    void SetPropertices(ShootSettings settings) {
        life = settings.bulletLife;
        rigi.mass = settings.bulletMass;
        speed = settings.bulletSpeed;
        this.shootSettings = settings;
    }

    public void Active(Vector3 pos, Quaternion rotation) {
        transform.position = pos;
        transform.rotation = rotation;
        
        transform.parent = null;
        
        rigi.velocity = transform.forward * speed *Time.deltaTime*100;

        isAlife = true;
    }

    public void DisActive() {
        
        rigi.velocity = Vector3.zero;
        if (!BulletRigidPoolMgr.Singleton) {
            // 弹夹不存在，直接销毁自身
            Destroy(gameObject);
            return;
        }
        transform.parent = BulletRigidPoolMgr.Singleton.transform;

        BulletRigidPoolMgr.Singleton.ReturnBulletGoToPool(gameObject);

        isAlife = false;
    }


    //计算伤害，头部200%伤害
    //身体百分百
    //肢体百分之五十
    float GetApplyInjury(string tag) {

        //计算伤害
        float injuryValue = shootSettings.maxHurtValue * (1 - lifeTimer / life);

        switch (tag) {
            case Tags.unitHead:
                injuryValue *= 2f;
                break;
            case Tags.unitBody:
                
                break;
            case Tags.unitLimbs:
                injuryValue *= 0.5f;
                break;
        }

        return injuryValue;
    }
}
