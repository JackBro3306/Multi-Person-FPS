using System;
using System.Collections;
using System.Collections.Generic;

#region 事件委托

public delegate void CallBack();
public delegate void CallBack<T>(T arg);
public delegate void CallBack<T1, T2>(T1 arg1,T2 arg2);

#endregion

/// <summary>
/// 事件中心
/// </summary>
public class EventCenter {

    /// <summary>
    /// 事件表
    /// </summary>
    private static Dictionary<EventID, Delegate> m_eventTable = new Dictionary<EventID, Delegate>();
    
    /// <summary>
    /// 添加事件前的安全校验
    /// </summary>
    private static void EventAdding(EventID eventType, Delegate callback) {

        // 判断是否存在该事件码
        if (!m_eventTable.ContainsKey(eventType)) {
            m_eventTable.Add(eventType, null);
        }
        Delegate d = m_eventTable[eventType];

        // 尝试为事件{eventType}添加不同类型的委托
        if (d != null && d.GetType() != callback.GetType()) {
            throw new Exception(string.Format("添加事件监听失败：尝试为事件{0}添加不同类型的委托，当前事件对应的委托类型：{1}，试图添加委托类型：{2}"
                                               , eventType, d.GetType(), callback.GetType()));
        }
    }

    /// <summary>
    /// 移除事件前的安全校验
    /// </summary>
    private static void EventRemoving(EventID eventType, Delegate callback) {
        Delegate d = m_eventTable[eventType];
        if (d == null) {
            throw new Exception(string.Format("移除事件监听失败：事件{0}添加对应的委托事件", eventType));
        } else if (d.GetType() != callback.GetType()) {
            throw new Exception(string.Format("移除事件监听失败：尝试为事件{0}移除不同类型的委托，当前委托类型：{1}，试图移除的委托类型为：{2}"
                , eventType, d.GetType(), callback.GetType()));
        }
    }

    /// <summary>
    /// 移除事件后
    /// </summary>
    private static void EventRemoved(EventID eventType) {
        if (m_eventTable[eventType] == null) {
            m_eventTable.Remove(eventType);
        }
    }

    #region 无参
    /// <summary>
    /// 注册监听
    /// </summary>
    public static void AddEventListener(EventID eventType,CallBack callback) {
        EventAdding(eventType,callback);
        m_eventTable[eventType] = (CallBack)m_eventTable[eventType] + callback;
    }

    /// <summary>
    /// 移除监听
    /// </summary>
    public static void RemoveEventListener(EventID eventType,CallBack callback) {
        if (m_eventTable.ContainsKey(eventType)) {

            EventRemoving(eventType, callback);
            m_eventTable[eventType] = (CallBack)m_eventTable[eventType] - callback;
            EventRemoved(eventType);
        }
    }

    /// <summary>
    /// 广播
    /// </summary>
    public static void Broadcast(EventID eventType) {
        Delegate d = null;
        if (m_eventTable.TryGetValue(eventType, out d)) {
            CallBack callback = d as CallBack;
            if (callback != null) {
                callback();
            } else {
                throw new Exception(string.Format("广播事件失败：事件{0}对应的委托具有不同的类型",eventType));
            }
        }
    }
    #endregion

    #region 一个参数
    /// <summary>
    /// 注册监听
    /// </summary>
    public static void AddEventListener<T>(EventID eventType, CallBack<T> callback) {
        EventAdding(eventType, callback);
        m_eventTable[eventType] = (CallBack<T>)m_eventTable[eventType] + callback;
    }

    /// <summary>
    /// 移除监听
    /// </summary>
    public static void RemoveEventListener<T>(EventID eventType, CallBack<T> callback) {
        if (m_eventTable.ContainsKey(eventType)) {

            EventRemoving(eventType, callback);
            m_eventTable[eventType] = (CallBack<T>)m_eventTable[eventType] - callback;
            EventRemoved(eventType);
        }
    }

    /// <summary>
    /// 广播
    /// </summary>
    public static void Broadcast<T>(EventID eventType,T arg) {
        Delegate d = null;
        if (m_eventTable.TryGetValue(eventType, out d)) {
            CallBack<T> callback = d as CallBack<T>;
            if (callback != null) {
                callback(arg);
            } else {
                throw new Exception(string.Format("广播事件失败：事件{0}对应的委托具有不同的类型", eventType));
            }
        }
    }
    #endregion

    #region 两个个参数
    /// <summary>
    /// 注册监听
    /// </summary>
    public static void AddEventListener<T1,T2>(EventID eventType, CallBack<T1,T2> callback) {
        EventAdding(eventType, callback);
        m_eventTable[eventType] = (CallBack<T1,T2>)m_eventTable[eventType] + callback;
    }

    /// <summary>
    /// 移除监听
    /// </summary>
    public static void RemoveEventListener<T1, T2>(EventID eventType, CallBack<T1, T2> callback) {
        if (m_eventTable.ContainsKey(eventType)) {

            EventRemoving(eventType, callback);
            m_eventTable[eventType] = (CallBack<T1, T2>)m_eventTable[eventType] - callback;
            EventRemoved(eventType);
        }
    }

    /// <summary>
    /// 广播
    /// </summary>
    public static void Broadcast<T1, T2>(EventID eventType, T1 arg1,T2 arg2) {
        Delegate d = null;
        if (m_eventTable.TryGetValue(eventType, out d)) {
            CallBack<T1, T2> callback = d as CallBack<T1, T2>;
            if (callback != null) {
                callback(arg1,arg2);
            } else {
                throw new Exception(string.Format("广播事件失败：事件{0}对应的委托具有不同的类型", eventType));
            }
        }
    }
    #endregion

}
