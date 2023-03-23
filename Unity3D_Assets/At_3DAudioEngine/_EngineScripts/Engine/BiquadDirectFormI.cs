using System.Collections;
using System.Collections.Generic;


public class BiquadDirectFormI
{


	// delay line
	float m_x2; // x[n-2]
	float m_y2; // y[n-2]
	float m_x1; // x[n-1]
	float m_y1; // y[n-1]

	// coefficients
	float c_b0, c_b1, c_b2; // FIR
	float c_a1, c_a2; // IIR

	// constructor with the coefficients b0,b1,b2 for the FIR part
	// and a1,a2 for the IIR part. a0 is always one.	
	public BiquadDirectFormI(float b0, float b1, float b2, float a1, float a2)
	{
		// FIR coefficients
		c_b0 = b0;
		c_b1 = b1;
		c_b2 = b2;
		// IIR coefficients
		c_a1 = a1;
		c_a2 = a2;
		reset();
}


	public void reset()
	{
		m_x1 = 0;
		m_x2 = 0;
		m_y1 = 0;
		m_y2 = 0;
	}

	// filtering operation: one sample in and one out
	public float filter(float x)
	{
		// if the input sample is NaN
		if(x != x) x = 0.0f;
        
		// calculate the output
		float y = c_b0 * x + c_b1 * m_x1 + c_b2 * m_x2 - c_a1 * m_y1 - c_a2 * m_y2;
		// update the delay lines
		m_x2 = m_x1;
		m_y2 = m_y1;
		m_x1 = x;
		m_y1 = y;

		return y;
	}


}
