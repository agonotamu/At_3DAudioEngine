using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pd_controlFm : MonoBehaviour
{
    LibPdInstance lpi;
    public float fm_ratio;
    public float fm_index;
    public float fm_pitch;
    bool pdOn = false;
    // Start is called before the first frame update
    void Start()
    {
        lpi = GetComponent<LibPdInstance>();
        
    }

    // Update is called once per frame
    void Update()
    {
        lpi.SendFloat("fm_ratio", fm_ratio);
        lpi.SendFloat("fm_index", fm_index);
        lpi.SendFloat("fm_pitch", fm_pitch);
    }
    public void startStop()
    {
        pdOn = !pdOn;

        if (pdOn)
        {
            lpi.SendBang("triggerOn");
        }
        else
        {
            lpi.SendBang("triggerOff");
        }
        
    }
    
}
