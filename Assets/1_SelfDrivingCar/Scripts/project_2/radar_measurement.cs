using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class radar_measurement : MonoBehaviour {

	private float measure_rho;
	private float measure_theta;
	private float measure_rhod;
	private long timestamp;
	private float gnt_xpos;
	private float gnt_ypos;
	private float gnt_vx;
	private float gnt_vy;

	// Use this for initialization
	void Start () {


	}

	// Use this for initialization
	public void Set (float mr, float mt, float mrd, long t, float gx, float gy, float gvx, float gvy) {
		measure_rho = mr;
		measure_theta = mt;
		measure_rhod = mrd; 
		timestamp = t;
		gnt_xpos = gx;
		gnt_ypos = gy;
		gnt_vx = gvx;
		gnt_vy = gvy;

	}

	public string packet()
	{
		return "R\\t" + measure_rho.ToString ("N4") + "\\t" + measure_theta.ToString ("N4") + "\\t" + measure_rhod.ToString ("N4") + "\\t" + timestamp.ToString () + "\\t" +
			gnt_xpos.ToString ("N4") + "\\t" + gnt_ypos.ToString ("N4") + "\\t" + gnt_vx.ToString ("N4") + "\\t" + gnt_vy.ToString ("N4");
	}

}
