using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityTools {
    
    // 获得多个坐标点的中心点
    public static Vector3 GetCenter(List<Transform> list) {
        Vector3 temp = Vector3.zero;
        foreach(var t in list) {
            temp += t.position;
        }
        return temp / list.Count;
        
    }

}
