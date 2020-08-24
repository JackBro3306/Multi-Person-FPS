using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectDestroy : MonoBehaviour
{
    public float destoryTime = 1f;

    void Start()
    {
        Destroy(gameObject,destoryTime);
    }

    
}
