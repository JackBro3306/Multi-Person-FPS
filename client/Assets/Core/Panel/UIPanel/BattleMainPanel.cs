using LSGameServ.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleMainPanel : PanelBase {

    private Text blueScoreTxt;
    private Text redScoreTxt;
    private Text resetTimeTxt;

    private Text hpTxt;
    private Text ammoTxt;
    private Image killImg;
    /// <summary>
    /// 战斗时间限制
    /// </summary>
    private int battleLimitTime = 0;
    private float timeSpend = 0.0f;
    private bool openTimer = false;

    #region 生命周期
    public override void Init(params object[] args) {
        base.Init(args);
        //设置路径
        skinPath = "UIPanel/BattleMainPanel";
        //设置层级
        layer = PanelLayer.Panel;

        battleLimitTime = (int)args[0];
        openTimer = true;
    }

    public override void OnShowing() {
        base.OnShowing();
        InitUI();
        // 设置鼠标不可见，并且锁定在游戏中
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 注册事件
        EventCenter.AddEventListener<string,string>(EventID.UpdateBattleText, UpdateBattleText);
        EventCenter.AddEventListener(EventID.ShowKillImg, ShowKillImg);
    }

    public override void OnClosed() {
        base.OnClosed();
        EventCenter.RemoveEventListener<string, string>(EventID.UpdateBattleText, UpdateBattleText);
        EventCenter.RemoveEventListener(EventID.ShowKillImg, ShowKillImg);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.UpdateScore, RecvUpdateBattleScore);
        NetMgr.GetInstance().tcpSock.msgDist.DelListener(Protocol.GameResult, RecvResult);
    }
    #endregion

    void InitUI() {
        Transform skinTran = skin.transform;
        blueScoreTxt = skinTran.Find("LeftImg/ScoreText").GetComponent<Text>();
        redScoreTxt = skinTran.Find("RightImg/ScoreText").GetComponent<Text>();
        resetTimeTxt = skinTran.Find("ResetTimeText").GetComponent<Text>();
        hpTxt = skinTran.Find("BattleInfo/HpText").GetComponent<Text>();
        ammoTxt = skinTran.Find("BattleInfo/AmmoText").GetComponent<Text>();
        killImg = skinTran.Find("KillImage").GetComponent<Image>();
        killImg.gameObject.SetActive(false);

        blueScoreTxt.text = 0.ToString();
        redScoreTxt.text = 0.ToString();

        //协议回调监听
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.UpdateScore, RecvUpdateBattleScore);
        NetMgr.GetInstance().tcpSock.msgDist.AddListener(Protocol.GameResult, RecvResult);

    }

    new void Update() {
        if (openTimer) {

            timeSpend += Time.deltaTime;

            int millisecond = battleLimitTime - (int)(timeSpend * 1000);

            int h = millisecond / 3600000;
            int m = (millisecond - h * 3600000) / 60000;
            int s = (millisecond - h * 3600000 - m * 60000)/1000;

            resetTimeTxt.text = string.Format("{0:D2}:{1:D2}", m, s);
        }
        
    }

    void UpdateBattleText(string hpStr, string ammoStr) {
        hpTxt.text = hpStr;
        ammoTxt.text = ammoStr;
    }

    void ShowKillImg() {
        if (!killImg.gameObject.activeSelf) {
            killImg.gameObject.SetActive(true);
            StartCoroutine(HideKillImg());
        }
            
        
    }
    
    IEnumerator HideKillImg() {
        yield return new WaitForSeconds(0.8f);
        killImg.gameObject.SetActive(false);
    }

    // 更新比分回调
    public void RecvUpdateBattleScore(GameMessage message) {
        BattleScore battleScore = ProtoTransfer.Deserialize<BattleScore>(message.data) ;
        int scoreT1 =battleScore.scoreT1;
        int scoreT2 = battleScore.scoreT2;
        redScoreTxt.text = scoreT1.ToString();
        blueScoreTxt.text = scoreT2.ToString();
    }


    // 处理战斗结果
    public void RecvResult(GameMessage message) {
        // 停止计时
        openTimer = false;
    }
    
}