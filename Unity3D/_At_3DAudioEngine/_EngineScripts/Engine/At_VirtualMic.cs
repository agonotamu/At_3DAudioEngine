using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class At_VirtualMic : MonoBehaviour
{
    public int id;
    public float m_maxDistanceForDelay;

#if UNITY_STANDALONE
    private void Awake()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }
#endif

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        At_OutputState os = At_AudioEngineUtils.getOutputState(SceneManager.GetActiveScene().name);

        const float numStepDrawCircle = 20;
        float angle = 2 * Mathf.PI / numStepDrawCircle;

        Gizmos.color = new Color(1, 0, 0, 0.1f);
        if (os != null)
        {
            for (int i = 0; i < numStepDrawCircle; i++)
            {
                Vector3 center = transform.position + new Vector3(os.maxDistanceForDelay * Mathf.Cos(i * angle), 0, os.maxDistanceForDelay * Mathf.Sin(i * angle));
                Vector3 nextCenter = transform.position + new Vector3(os.maxDistanceForDelay * Mathf.Cos((i + 1) * angle), 0, os.maxDistanceForDelay * Mathf.Sin((i + 1) * angle)); ;
                //Debug.DrawLine(center, nextCenter, Color.green);            
                Gizmos.DrawLine(center, nextCenter);
            }

            SceneView.RepaintAll();
        }
        

    }
#endif
}
