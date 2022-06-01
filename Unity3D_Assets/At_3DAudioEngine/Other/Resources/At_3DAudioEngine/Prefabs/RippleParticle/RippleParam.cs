using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RippleParam : MonoBehaviour
{
    ParticleSystem ps;
    ParticleSystem.EmissionModule emissionModule;
    ParticleSystem.SizeOverLifetimeModule sizeOverLifetimeModule;
    ParticleSystem.MainModule mainModule;
    ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule;
    public int id;
    float previousDelay = 0;
    float previousVolume = 0;
    float previousSize = 0;
    float previousRate = 0;
    float previousTimeReversal = 0;

    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        emissionModule = ps.emission;
        sizeOverLifetimeModule = ps.sizeOverLifetime;
        mainModule = ps.main;
        colorOverLifetimeModule = ps.colorOverLifetime;
    }

    public void setRate(float rate)
    {
        //emissionModule.rateOverTime = rate;        
    }
    public void setMaxSize(float size)
    {
        //sizeOverLifetimeModule.sizeMultiplier = size;
    }
    public void Stop()
    {
        ps.Stop();
    }

    public void Play()
    {
        ps.Play();
    }

    public void setParam(float delay, float volume, float size, float rate, float timeReversal)
    {
        if (previousDelay != delay || previousVolume != volume || previousSize != size || previousRate != rate || previousTimeReversal != timeReversal)
        {
            ps.Clear();
            mainModule.startDelay = delay / 1000f;
            emissionModule.rateOverTime = rate;
            sizeOverLifetimeModule.sizeMultiplier = size;
            Gradient grad = new Gradient();
            grad.SetKeys(new GradientColorKey[] { new GradientColorKey(new Color(1, 0, 0), 0.0f), new GradientColorKey(new Color(1, 0, 0), 1.0f) }, 
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(volume, 0.001f), new GradientAlphaKey(0.0f, 1.0f) });

            colorOverLifetimeModule.color = grad;
            ps.Play();
            previousDelay = delay;
            previousVolume = volume;
            previousSize = size;
            previousRate = rate;
            previousTimeReversal = timeReversal;
        }


    }

    // Update is called once per frame
    void Update()
    {
        
    }


   

}
