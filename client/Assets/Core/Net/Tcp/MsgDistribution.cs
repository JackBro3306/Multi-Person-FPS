using LSGameServ.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//消息分发
public class MsgDistribution  {

    //每帧处理消息的数量
    public int num = 15;
    //消息列表
    public List<GameMessage> msgList = new List<GameMessage>();
    //委托类型
    public delegate void Delegate(GameMessage msg);

    //事件监听表
    private Dictionary<Protocol, Delegate> eventDict = new Dictionary<Protocol, Delegate>();
    //一次性监听表，使用后清除对应的key和value
    private Dictionary<Protocol, Delegate> onceDict = new Dictionary<Protocol, Delegate>();

    //Update
    public void Update() {
        for (int i=0;i<num;i++) {
            if(msgList.Count > 0) {
                DispatchMsgEvent(msgList[0]);
                lock (msgList)
                    msgList.RemoveAt(0);
            } else {
                break;
            }
        }
    }

    //消息分发
    public void DispatchMsgEvent(GameMessage message) {
        Protocol msgType = (Protocol)message.type[0];
        
        Debug.Log("分发处理消息 "+msgType);
        if (eventDict.ContainsKey(msgType)) {
            eventDict[msgType](message);
        }

        if (onceDict.ContainsKey(msgType)) {
            onceDict[msgType](message);
            onceDict[msgType] = null;
            onceDict.Remove(msgType);
        }
    }

    //添加监听事件
    public void AddListener(Protocol msgType,Delegate cb) {
        if (eventDict.ContainsKey(msgType))
            eventDict[msgType] += cb;
        else
            eventDict[msgType] = cb;
    }

    //添加单次监听事件
    public void AddOnceListener(Protocol msgType,Delegate cb) {
        if (onceDict.ContainsKey(msgType))
            onceDict[msgType] += cb;
        else
            onceDict[msgType] = cb;
    }

    //删除监听事件
    public void DelListener(Protocol msgType,Delegate cb) {
        if (eventDict.ContainsKey(msgType)) {
            eventDict[msgType] -= cb;
            if (eventDict[msgType] == null)
                eventDict.Remove(msgType);
        }
    }

    //删除单次监听事件
    public void DelOnceListener(Protocol msgType,Delegate cb) {
        if (onceDict.ContainsKey(msgType)) {
            onceDict[msgType] -= cb;
            if (onceDict[msgType] == null)
                onceDict.Remove(msgType);
        }
    }
}
