

#include <math.h>
#include "Biquad.h"

#define M_PI 3.141592653589793f

Biquad::Biquad() {
    type = bq_type_lowpass;
    a0 = 1.0;
    a1 = a2 = b1 = b2 = 0.0;
    Fc = 20000.0f;
    Q = 0.707f;
    peakGain = 0.0;
    x1 = x2 = y1 = y2 = 0.0;
    _sampleRate = 48000;
    calcBiquad();
}

Biquad::Biquad(int type, float Fc, float Q, float peakGainDB, int sampleRate) {
    _sampleRate = sampleRate;
    setBiquad(type, Fc, Q, peakGainDB);
    x1 = x2 = y1 = y2 = 0.0;
    calcBiquad();
}

Biquad::~Biquad() {
}


void Biquad::setSampleRate(int sampleRate) {
    _sampleRate = sampleRate;
}

void Biquad::setType(int type) {
    if (type != this->type) {
        std::cout << "set biquad type =" << type << "\n";
        this->type = type;
        calcBiquad();
    }
}

void Biquad::setQ(float Q) {
    if (Q != this->Q) {
        this->Q = Q;
        calcBiquad();
    }
}

void Biquad::setFc(float Fc) {
    if (Fc != this->Fc) {
        

        this->Fc = Fc;
        calcBiquad();
    }
}

void Biquad::setPeakGain(float peakGainDB) {
    if (peakGainDB != this->peakGain) {
        this->peakGain = peakGainDB;
        calcBiquad();
    }
}

void Biquad::setBiquad(int type, float Fc, float Q, float peakGainDB) {
    this->type = type;
    this->Q = Q;
    this->Fc = Fc;
    this->peakGain = peakGainDB;
    
}

void Biquad::calcBiquad(void) {
    
    float norm;
    float V = pow(10, fabs(peakGain) / 20.0);
    float K = tan(M_PI * (Fc/_sampleRate));

    float wc = 2 * M_PI * Fc;
    float k = wc / tan((wc / 2.0f) / _sampleRate);
    float den = wc * wc + k * k + 2 * k * wc;

    switch (this->type) {
    case bq_type_lowpass:
        norm = 1 / (1 + K / Q + K * K);
        a0 = K * K * norm;
        a1 = 2 * a0;
        a2 = a0;
        b1 = 2 * (K * K - 1) * norm;
        b2 = (1 - K / Q + K * K) * norm;
        std::cout << "low pass calc - Fc =" << Fc <<"\n";
        break;

    case bq_type_highpass:
        norm = 1 / (1 + K / Q + K * K);
        a0 = 1 * norm;
        a1 = -2 * a0;
        a2 = a0;
        b1 = 2 * (K * K - 1) * norm;
        b2 = (1 - K / Q + K * K) * norm;
        std::cout << "high pass calc - Fc =" << Fc <<"\n";
        break;
    case bq_type_lowpass_LinkwitzRiley:        
        a0 = wc / den;
        a1 = 2 * wc * wc / den;
        a2 = a0;
        b1 = (2 * wc * wc - 2 * k * k) / den;
        b2 = (wc * wc + k * k - 2 * k * wc) / den;
        std::cout << "low pass Riley calc - Fc =" << Fc << "\n";
        break;
    case bq_type_highpass_LinkwitzRiley:       
        a0 = k * k / den;
        a1 = -2 * k * k / den;
        a2 = a0;
        b1 = (2 * wc * wc - 2 * k * k) / den;
        b2 = (wc * wc + k * k - 2 * k * wc) / den;
        std::cout << "high pass Riley calc - Fc =" << Fc << "\n";
        break;
    case bq_type_bandpass:
        norm = 1 / (1 + K / Q + K * K);
        a0 = K / Q * norm;
        a1 = 0;
        a2 = -a0;
        b1 = 2 * (K * K - 1) * norm;
        b2 = (1 - K / Q + K * K) * norm;
        break;

    case bq_type_notch:
        norm = 1 / (1 + K / Q + K * K);
        a0 = (1 + K * K) * norm;
        a1 = 2 * (K * K - 1) * norm;
        a2 = a0;
        b1 = a1;
        b2 = (1 - K / Q + K * K) * norm;
        break;

    case bq_type_peak:
        if (peakGain >= 0) {    // boost
            norm = 1 / (1 + 1 / Q * K + K * K);
            a0 = (1 + V / Q * K + K * K) * norm;
            a1 = 2 * (K * K - 1) * norm;
            a2 = (1 - V / Q * K + K * K) * norm;
            b1 = a1;
            b2 = (1 - 1 / Q * K + K * K) * norm;
        }
        else {    // cut
            norm = 1 / (1 + V / Q * K + K * K);
            a0 = (1 + 1 / Q * K + K * K) * norm;
            a1 = 2 * (K * K - 1) * norm;
            a2 = (1 - 1 / Q * K + K * K) * norm;
            b1 = a1;
            b2 = (1 - V / Q * K + K * K) * norm;
        }
        break;
    case bq_type_lowshelf:
        if (peakGain >= 0) {    // boost
            norm = 1 / (1 + sqrt(2) * K + K * K);
            a0 = (1 + sqrt(2 * V) * K + V * K * K) * norm;
            a1 = 2 * (V * K * K - 1) * norm;
            a2 = (1 - sqrt(2 * V) * K + V * K * K) * norm;
            b1 = 2 * (K * K - 1) * norm;
            b2 = (1 - sqrt(2) * K + K * K) * norm;
        }
        else {    // cut
            norm = 1 / (1 + sqrt(2 * V) * K + V * K * K);
            a0 = (1 + sqrt(2) * K + K * K) * norm;
            a1 = 2 * (K * K - 1) * norm;
            a2 = (1 - sqrt(2) * K + K * K) * norm;
            b1 = 2 * (V * K * K - 1) * norm;
            b2 = (1 - sqrt(2 * V) * K + V * K * K) * norm;
        }
        break;
    case bq_type_highshelf:
        if (peakGain >= 0) {    // boost
            norm = 1 / (1 + sqrt(2) * K + K * K);
            a0 = (V + sqrt(2 * V) * K + K * K) * norm;
            a1 = 2 * (K * K - V) * norm;
            a2 = (V - sqrt(2 * V) * K + K * K) * norm;
            b1 = 2 * (K * K - 1) * norm;
            b2 = (1 - sqrt(2) * K + K * K) * norm;
        }
        else {    // cut
            norm = 1 / (V + sqrt(2 * V) * K + K * K);
            a0 = (1 + sqrt(2) * K + K * K) * norm;
            a1 = 2 * (K * K - 1) * norm;
            a2 = (1 - sqrt(2) * K + K * K) * norm;
            b1 = 2 * (K * K - V) * norm;
            b2 = (V - sqrt(2 * V) * K + K * K) * norm;
        }
        break;
    }

    return;
}