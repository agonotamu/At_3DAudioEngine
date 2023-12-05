
#ifndef Biquad_h
#define Biquad_h

#include <iostream>

enum {
    bq_type_lowpass = 0,
    bq_type_highpass,
    bq_type_lowpass_LinkwitzRiley,
    bq_type_highpass_LinkwitzRiley,
    bq_type_bandpass,
    bq_type_notch,
    bq_type_peak,
    bq_type_lowshelf,
    bq_type_highshelf
};

class Biquad {
public:
    Biquad();
    Biquad(int type, float Fc, float Q, float peakGainDB, int sampleRate);
    ~Biquad();
    void setSampleRate(int sampleRate);
    void setType(int type);
    void setQ(float Q);
    void setFc(float Fc);
    void setPeakGain(float peakGainDB);
    void setBiquad(int type, float Fc, float Q, float peakGainDB);
    float process(float in);

protected:
    void calcBiquad(void);

    int type;
    int _sampleRate;
    float a0, a1, a2, b1, b2;
    float Fc, Q, peakGain;
    // delay line
    float x2; // x[n-2]
    float y2; // y[n-2]
    float x1; // x[n-1]
    float y1; // y[n-1]
};

inline float Biquad::process(float in) {
   
    float out = a0 * in + a1 * x1 + a2 * x2 - b1 * y1 - b2 * y2;

    // update the delay lines
    x2 = x1;
    y2 = y1;
    x1 = in;
    y1 = out;

    return out;
    
}

#endif // Biquad_h