using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
/// <summary>
/// 提示面板，打开时需要传递提示内容，以及面板关闭后执行的操作
/// </summary>
public class TipPanel : PanelBase {
    //页面关闭的委托
    public delegate void OnTipClosed();

    private Text tipText;
    private Button comfireBtn;
    private CanvasGroup canvasGroup;
    private string tipStr;
   
    //提示面板关闭后执行的操作
    private event OnTipClosed clickEvent;
    #region 生命周期
    public override void Init(params object[] args) {
        base.Init(args);
        //设置路径
        skinPath = "UIPanel/TipPanel";
        //设置层级
        layer = PanelLayer.Tips;
        
    }
    public override void OnShowing() {
        base.OnShowing();
        InitUI();
        //获得提示文字
        tipStr = args[0] as string;
        tipText.text = tipStr;
        if (args.Length >= 2) {
            if (args[1] != null)
                clickEvent = (OnTipClosed)args[1];
        }
        skin.transform.localScale = new Vector3(0,0,0);
        // 动画
        skin.transform.DOScale(new Vector3(1, 1, 0), 0.6f).SetEase(Ease.OutBounce).OnComplete(()=> {
            // 确定按钮事件
            comfireBtn.onClick.AddListener(delegate () {
                if (clickEvent != null)
                    clickEvent();
                Close();

            });
        });
        
    }
    void InitUI() {
        Transform skinTran = skin.transform;
        tipText = skinTran.Find("TipText").GetComponent<Text>();
        comfireBtn = skinTran.Find("ComfireBtn").GetComponent<Button>();
        canvasGroup = skinTran.GetComponent<CanvasGroup>();
    }
    
    #endregion
}
