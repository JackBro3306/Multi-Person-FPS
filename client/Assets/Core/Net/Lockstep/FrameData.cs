using LSGameServ.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameData  {
    private uint mPlayFrameIndex = 1;
    private Dictionary<uint, List<BattleCommand>> mFrameCatchDic;

    public FrameData() {
        mFrameCatchDic = new Dictionary<uint, List<BattleCommand>>();
        mPlayFrameIndex = 1;
    }

    public uint MPlayFrameIndex {
        get {
            return mPlayFrameIndex;
        }
    }
    // 添加网络帧
    public void AddOneFrame(uint frameindex,List<BattleCommand> list) {
        lock (mFrameCatchDic) {
            if (frameindex >= mPlayFrameIndex) {
                mFrameCatchDic[frameindex] = list;
                int speed = (int)(frameindex - mPlayFrameIndex);
                if (speed == 0)
                    speed = 1;
                //NetMgr.GetInstance().SetFaseForward(speed);
            }
        }
    }

    // 获得网络帧
    public bool LockFrameTurn(ref List<BattleCommand> list) {
        lock (mFrameCatchDic) {
            if (mFrameCatchDic.TryGetValue(mPlayFrameIndex, out list)) {
                mFrameCatchDic.Remove(mPlayFrameIndex);
                mPlayFrameIndex++;
                return true;
            } else
                return false;
        }
    }
}
