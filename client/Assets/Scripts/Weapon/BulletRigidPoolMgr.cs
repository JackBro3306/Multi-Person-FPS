using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletRigidPoolMgr : MonoBehaviour
{
    #region singleton
    private static BulletRigidPoolMgr singleton;
    public static BulletRigidPoolMgr Singleton {
        get {
            if (singleton == null) {
                Debug.LogWarning("not init BulletRigidPoolMgr");
                return null;
            }
            return singleton;
        }

    }

    void Awake() {
        singleton = this;
    }

    #endregion

    public GameObject bulletRigidGo;
    //弹痕
    public GameObject[] impactsGo;
    //弹痕字典
    private Dictionary<string, GameObject> impactsDict = new Dictionary<string, GameObject>();

    [HideInInspector]
    public Queue<GameObject> bulletQu = new Queue<GameObject>();
    [HideInInspector]
    public Weapon weapon;
    

    void Start()
    {
        InitInpact();

        FindOnHandWeapon();
        int bulletNum = weapon.shootSettings.maxAmmo * weapon.shootSettings.pellets;

        CreateBullet(0, bulletNum);
    }

    void InitInpact() {
        foreach(GameObject go in impactsGo) {
            impactsDict.Add(go.name, go);
        }
    }

    public void FindOnHandWeapon() {

        Weapon[] weapons = FindObjectsOfType<Weapon>();

        foreach (Weapon we in weapons) {
            if (we.isActiveAndEnabled) {
                weapon = we;
                break;
            }
        }
    }

    void CreateBullet(int begin,int end) {
        
        //根据弹夹数量生成预制体
        for (int i = begin; i < end; i++) {
            GameObject go = Instantiate(bulletRigidGo, transform);
            go.SetActive(false);
            bulletQu.Enqueue(go);
        }
    }
    
    public GameObject NextAviableBulletGo() {
        GameObject go = null;
        if (bulletQu.Count > 0) {
            go = bulletQu.Dequeue();
            go.SetActive(true);
        }

        return go;
    }
    
    public void ReturnBulletGoToPool(GameObject bulletGo) {
        bulletGo.SetActive(false);
        bulletQu.Enqueue(bulletGo);
    }

    public void AddBullet() {
        FindOnHandWeapon();

        int bulletNum = weapon.shootSettings.maxAmmo * weapon.shootSettings.pellets;
        
        if (bulletQu.Count < bulletNum) {
            bulletNum = bulletNum - bulletQu.Count;
            
            CreateBullet(0, bulletNum);
            
        } 
    }

    //获得弹痕
    public GameObject GetImpact(string impactName) {
        GameObject go = null;

        impactsDict.TryGetValue(impactName,out go);
        return go;
    }

}
