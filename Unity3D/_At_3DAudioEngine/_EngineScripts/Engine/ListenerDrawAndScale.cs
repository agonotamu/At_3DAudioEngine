using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListenerDrawAndScale : MonoBehaviour
{
    public float scale = 1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = new Vector3(scale, scale, scale);        
        for (int i = 0; i < transform.childCount; i++)
        {
            Debug.DrawLine(transform.position, transform.GetChild(i).position, Color.green);
            
        }
    }
}
