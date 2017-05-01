using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lidar_measurement : MonoBehaviour {

	private float measure_x;
	private float measure_y;
	private long timestamp;
	private float gnt_xpos;
	private float gnt_ypos;
	private float gnt_vx;
	private float gnt_vy;
	private char measurment_type = 'L';


	// Use this for initialization
	void Start () {
		

	}
	public void Set(float mx, float my, long t, float gx, float gy, float gvx, float gvy)
	{
		measure_x = mx;
		measure_y = my;
		timestamp = t;
		gnt_xpos = gx;
		gnt_ypos = gy;
		gnt_vx = gvx;
		gnt_vy = gvy;
	}

	public string packet()
	{

		return "L\\t"+measure_x.ToString("N4")+"\\t"+measure_y.ToString("N4")+"\\t"+timestamp.ToString()+"\\t"+
		gnt_xpos.ToString("N4")+"\\t"+gnt_ypos.ToString("N4")+"\\t"+gnt_vx.ToString("N4")+"\\t"+gnt_vy.ToString("N4");
	}
		
		
}
