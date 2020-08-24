using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Root : MonoBehaviour {

	
	void Start () {
        //打开标题页面
        //PanelMgr._instance.OpenPanel<TitlePanel>("");
        // 设置连接的服务器地址
        if (Application.platform != RuntimePlatform.WindowsEditor)
            GetConfigIp();
        PanelMgr._instance.OpenPanel<LoginPanel>("");
	}
	
	
	void Update () {
        NetMgr.GetInstance().Update();
    }

    void GetConfigIp() {
        NetMgr.GetInstance().tcpHost = UnityFileRW.LoadFile("ipconfig.txt");
    }
}
