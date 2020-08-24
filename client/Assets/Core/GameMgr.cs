using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr : MonoBehaviour {
    public static GameMgr _instance;
    public string id = "Tank";
	void Awake() {
        _instance = this;
    }
}
