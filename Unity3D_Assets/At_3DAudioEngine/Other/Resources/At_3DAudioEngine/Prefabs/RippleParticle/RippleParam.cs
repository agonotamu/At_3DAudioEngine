using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RippleParam : MonoBehaviour
{
    ParticleSystem ps;
    ParticleSystem.EmissionModule emissionModule;
    ParticleSystem.SizeOverLifetimeModule sizeOverLifetimeModule;
    public int id;
    float previousDelay = 0;
    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        emissionModule = ps.emission;
        sizeOverLifetimeModule = ps.sizeOverLifetime;

    }

    public void setRate(float rate)
    {
        emissionModule.rateOverTime = rate;        
    }
    public void setMaxSize(float size)
    {
        sizeOverLifetimeModule.sizeMultiplier = size;
    }

    public void startWithUpdatedGainAndDelay(float delay, float volume)
    {
        if (previousDelay != delay)
        {
            ps.startDelay = delay / 1000f;
            ps.Play();
            previousDelay = delay;
        }


    }

    // Update is called once per frame
    void Update()
    {
        
    }


   

}
