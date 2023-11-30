using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SoundWaveShaderManager : MonoBehaviour
{

    public ComputeShader _computeShader;
    private RenderTexture _target;
    
    public At_MasterOutput master;
    public At_Player player;
    public float _waveFrequency;

    public Vector4[] _wavePositions;
    public GameObject[] speakers;
    public float[] _wfsAmps;
    public float[] _wfsDelays;


    Vector4[] _wfsAmps_v4;
    Vector4[] _wfsDelays_v4;

    bool isMasterOutputInitialized = false;

    private void Awake()
    {
        
    }
    public void Init()
    {
        setShaderParameter();

        _wavePositions = new Vector4[master.outputChannelCount];
        speakers = new GameObject[master.outputChannelCount];
        _wfsAmps = new float[master.outputChannelCount];
        _wfsDelays = new float[master.outputChannelCount];


        _wfsAmps_v4 = new Vector4[master.outputChannelCount];
        _wfsDelays_v4 = new Vector4[master.outputChannelCount];

        At_VirtualSpeaker[] vss = GameObject.FindObjectsOfType<At_VirtualSpeaker>();
        vss = vss.OrderBy(x => x.id).ToArray();

        for (int i = 0; i < vss.Length; i++)
        {
            speakers[i] = vss[i].gameObject;
        }

        isMasterOutputInitialized = true;
    }

    private void Update()
    {
        if (isMasterOutputInitialized)
        {
            for (int i = 0; i < master.outputChannelCount; i++)
            {
                _wavePositions[i].x = speakers[i].transform.position.x;
                _wavePositions[i].z = speakers[i].transform.position.z;
                _wfsAmps_v4[i].x = player.volumeArray[i]; //_wfsAmps[i];
                _wfsAmps[i] = _wfsAmps_v4[i].x;
                //_wfsAmps_v4[i].x = _wfsAmps[i];
                _wfsDelays_v4[i].x = player.delayArray[i];//_wfsDelays[i];
                _wfsDelays[i] = _wfsDelays_v4[i].x;
            }
        }
    }


    private void setShaderParameter()
    {

        if (isMasterOutputInitialized)
        {
            _computeShader.SetFloat("_outputChannelCount", master.outputChannelCount);
            _computeShader.SetFloat("_displayPlaneSizeX", 10 * transform.localScale.x);
            _computeShader.SetFloat("_displayPlaneSizeZ", 10 * transform.localScale.z);
            _computeShader.SetFloat("_displayPlanePositionX", transform.position.x);
            _computeShader.SetFloat("_displayPlanePositionZ", transform.position.z);
            _computeShader.SetFloat("_waveFrequency", _waveFrequency);
            _computeShader.SetVectorArray("_wavePositions", _wavePositions);
            _computeShader.SetVectorArray("_wfsAmps", _wfsAmps_v4);
            _computeShader.SetVectorArray("_wfsDelays", _wfsDelays_v4);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (isMasterOutputInitialized)
        {
            setShaderParameter();
            Render(destination);
        }
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        _computeShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        _computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        Graphics.Blit(_target, destination);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

}
