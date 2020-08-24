using LSGameServ.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lockstep  {
    
    // 当前逻辑帧执行速度
    private int mFastForwardSpeed = 1;
    private float mLogicTempTime = 0;
    private int GameFrameInTurn = 0;

    public void Update () {
        mLogicTempTime += Time.deltaTime;
        if(mLogicTempTime > LockStepConfig.mRenderFrameUpdateTime) {
            for(int i = 0; i < mFastForwardSpeed; i++) {
                GameTurn();
            }
            mLogicTempTime = 0;
        }

    }
    
    public void SetFaseForward(int tValue) {
        mFastForwardSpeed = tValue;
    }

    void GameTurn() {
        if(GameFrameInTurn == 0) {
            // 逻辑帧
            List<BattleCommand> list = null;
            //if(NetMgr.GetInstance().LockFrameTurn(ref list)) {
            //    if(list != null) {
            //        // 事件分发
                    
            //    }
            //    GameFrameInTurn++;


            //}
        } else {
            // 渲染帧
            //NetMgr.GetInstance().UpdateEvent();
            //if (GameFrameInTurn == LockStepConfig.mRenderFrameCount)
            //    GameFrameInTurn = 0;
            //else
            //    GameFrameInTurn++;
        }
    }
}
