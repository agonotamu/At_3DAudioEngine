using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class At_VirtualSpeaker : MonoBehaviour
{
    public int id;
    public float distance;

#if UNITY_STANDALONE
    private void Awake()
    {
        GetComponent<MeshRenderer>().enabled = false;        
    }
#endif
}
