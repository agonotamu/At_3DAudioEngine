using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class At_VirtualMic : MonoBehaviour
{
    public int id;


#if UNITY_STANDALONE
    private void Awake()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }
#endif

}
