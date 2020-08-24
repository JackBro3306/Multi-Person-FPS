using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelMgr : MonoBehaviour {

	//单例
    public static PanelMgr _instance;
    //面板
    private GameObject canvas;
    //面板，什么类型的面板，key类型，value面板类
    public Dictionary<string, PanelBase> dict;

    //层级
    private Dictionary<PanelLayer, Transform> layerDict;

    public void Awake() {
        _instance = this;
        InitLayer();
        dict = new Dictionary<string, PanelBase>();
    }
	
	//初始化层
    private void InitLayer() {
        //获得画布
        canvas = GameObject.Find("Canvas");
        if (canvas == null)
            Debug.LogError("panelMgr.InitLayer fail ,canvas is null");
        //各个层级
        layerDict = new Dictionary<PanelLayer, Transform>();
        //遍历层级，找到各个层级的父物体
        foreach(PanelLayer pl in Enum.GetValues(typeof(PanelLayer))){
            string name = pl.ToString();
            Transform transform = canvas.transform.Find(name);
            layerDict.Add(pl,transform);
        }
    }

    //打开面板
    //限定T必须继承自PanelBase
    public void OpenPanel<T>(string skinPath,params object[] args)where T : PanelBase {
        //已经打开
        string name = typeof(T).ToString();
        if (dict.ContainsKey(name))
            return;

        //面板脚本
        PanelBase panel = canvas.AddComponent<T>();
        //将面板添加场景中
        panel.Init(args);

        dict.Add(name, panel);
        //加载皮肤
        skinPath = (skinPath != "" ? skinPath : panel.skinPath);
        //根据皮肤路径动态生成皮肤
        GameObject skin = Resources.Load<GameObject>(skinPath);
        if (skin == null)
            Debug.LogError("panelMgr.OpenPanel fail,skin is null,skinPath = "+skinPath);

        panel.skin = Instantiate(skin);
        //坐标
        Transform skinTrans = panel.skin.transform;
        PanelLayer layer = panel.layer;
        //根据层级获得父物体
        Transform parent = layerDict[layer];
        skinTrans.SetParent(parent,false);
        //panel的生命周期
        panel.OnShowing();
        panel.OnShowed();
    }

    //关闭面板
    public void ClosePanel<T>() {
        string name = typeof(T).ToString();
        ClosePanel(name);
    }

    public void ClosePanel(string name) {
        PanelBase panel = null;
        if (dict.ContainsKey(name)) {
            panel = dict[name];
        }
        if (panel == null)
            return;
        panel.OnClosing();
        dict.Remove(name);
        panel.OnClosed();

        Destroy(panel.skin);
        Destroy(panel);
    }

}

//分层类型
public enum PanelLayer {
    //面板
    Panel,
    //提示
    Tips,
}
