using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using LSGameServ.Protobuf;
using DG.Tweening;

public class WinPanel : PanelBase
{
    private Image winImage;
    private Image failImage;
    private Button closeBtn;
    private bool isWin;

    #region 生命周期
    public override void Init(params object[] args)
    {
        base.Init(args);
        //设置路径
        skinPath = "UIPanel/WinPanel";
        //设置层级
        layer = PanelLayer.Panel;

    }

    public override void OnShowing() {
        base.OnShowing();
        InitUI();
        //参数 args[1]代表获胜的阵营
        if (args.Length == 1) {
            int camp = (int)args[0];
            isWin = (camp == 1);
        }
        skin.transform.DOScale(new Vector3(1,1,1),0.3f).OnComplete(()=> {
            closeBtn.onClick.AddListener(OnCloseClick);
            // 设置鼠标可见，并解锁
            // 设置鼠标不可见，并且锁定在游戏中
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

        });

        //根据参数显示图片和文字
        if (isWin) {
            failImage.enabled = false;
            
        } else {
            winImage.enabled = false;
            
        }
    }
    void InitUI() {
        Transform skinTran = skin.transform;
        //关闭按钮
        closeBtn = skinTran.Find("CloseBtn").GetComponent<Button>();
        //图片和文字
        winImage = skinTran.Find("WinImage").GetComponent<Image>();
        failImage = skinTran.Find("FailImage").GetComponent<Image>();
        
    }
    
    #endregion

    public void OnCloseClick()
    {
        MutiBattle._instance.ClearBattle();
        //发送
        GameMessage msg = new GameMessage();
        msg.type = System.BitConverter.GetBytes((int)Protocol.FightComplete);
        msg.data = System.BitConverter.GetBytes(1);
        //发送CompleteFight协议，并注册返回监听
        NetMgr.GetInstance().tcpSock.Send(msg, OnCloseBack);
        
    }

    public void OnCloseBack(GameMessage message) {

        int ret = System.BitConverter.ToInt32(message.data,0);
        if (ret == 0) {
            PanelMgr._instance.ClosePanel<BattleMainPanel>();
            PanelMgr._instance.OpenPanel<RoomPanel>("");
            Close();
        }
        
    }
}


