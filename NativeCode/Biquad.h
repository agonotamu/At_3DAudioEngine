#pragma once

#include <math.h>

class Biquad
{

private:

	const float kPI = 3.141592653589793f;
	const double kPI_double = 3.141592653589793;

	// delay line
	float m_x2 = 0; // x[n-2]
	float m_y2 = 0; // y[n-2]
	float m_x1 = 0; // x[n-1]
	float m_y1 = 0; // y[n-1]

	// coefficients
	float m_b0 = 0, m_b1 = 0, m_b2 = 0; // FIR
	float m_a1 = 0, m_a2 = 0; // IIR

public:
	void setNotchCoefficient(float frequency, float linearGain, float bandwith, float sampleRate)
	{
		float alpha;
		if (linearGain < 1) {
			alpha = (tan(kPI * bandwith / sampleRate) - linearGain) / (tan(kPI * bandwith / sampleRate) + linearGain);
		}
		else {
			alpha = (tan(kPI * bandwith / sampleRate) - 1) / (tan(kPI * bandwith / sampleRate) + 1);
		}
		float beta = -cos(2 * kPI * frequency / sampleRate);
		float h = linearGain - 1;
		
		m_b0 = 1 + (1 + alpha) * h / 2;
		m_b1 = beta * (1 - alpha);
		m_b2 = -alpha - (1 + alpha) * h / 2;
		m_a1 = m_b1;
		m_a2 = -alpha;

	}

	void setLowPassCoefficient_LinkwitzRiley(float frequency, float sampleRate) 
	{

		float wc = 2 * kPI * frequency;
		float k = wc / tan((wc / 2.0f) / sampleRate);
		float den = wc * wc + k * k + 2 * k * wc;
		m_b0 = wc / den;
		m_b1 = 2 * wc * wc / den;
		m_b2 = m_b0;
		m_a1 = (2 * wc * wc - 2 * k * k) / den;
		m_a2 = (wc * wc + k * k - 2 * k * wc) / den;
		
	}

	void setHigPassCoefficient_LinkwitzRiley(float frequency, float sampleRate) {
		
		float wc = 2 * kPI * frequency;
		float k = wc / tan((wc / 2.0f) / sampleRate);
		float den = wc * wc + k * k + 2 * k * wc;
		m_b0 = k * k / den;
		m_b1 = -2 * k * k / den;
		m_b2 = m_b0;
		m_a1 = (2 * wc * wc - 2 * k * k) / den;
		m_a2 = (wc * wc + k * k - 2 * k * wc) / den;

	}

	// filtering operation: one sample in and one out
	float filter(float x)
	{
		float y = m_b0 * x + m_b1 * m_x1 + m_b2 * m_x2 - m_a1 * m_y1 - m_a2 * m_y2;

		// update the delay lines
		m_x2 = m_x1;
		m_y2 = m_y1;
		m_x1 = x;
		m_y1 = y;

		return y;
	}
};
