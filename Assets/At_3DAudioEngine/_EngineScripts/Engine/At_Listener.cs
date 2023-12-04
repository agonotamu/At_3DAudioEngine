using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class At_Listener : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        updatePositionAndRotation();

    }

    void updatePositionAndRotation()
    {
        float[] position = new float[3];
        float[] rotation = new float[3];

        float eulerX = gameObject.transform.eulerAngles.x;
        float eulerY = gameObject.transform.eulerAngles.y;
        float eulerZ = gameObject.transform.eulerAngles.z;

        if (eulerY == 180 && eulerZ == 180)
        {
            eulerX = 180 - eulerX;
            eulerY = 0;
            eulerZ = 0;
        }
        position[0] = gameObject.transform.position.x;
        rotation[0] = eulerX;
        position[1] = gameObject.transform.position.y;
        rotation[1] = eulerY;
        position[2] = gameObject.transform.position.z;
        rotation[2] = eulerZ;

        AT_SPAT_WFS_setListenerPosition(position, rotation);
    }

    #region DllImport
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setListenerPosition(float[] position, float[] rotation);
    #endregion

}
