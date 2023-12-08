using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourceLookAtListener : MonoBehaviour
{
    public GameObject listenerObject;
    public GameObject refPositionObject;

    // Update is called once per frame
    void Update()
    {

        transform.position = refPositionObject.transform.position;

        transform.forward = (listenerObject.transform.position - transform.position).normalized;
    }
}
