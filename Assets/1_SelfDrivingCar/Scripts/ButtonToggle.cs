using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Vehicles.Car
{
public class ButtonToggle : MonoBehaviour {

	public Text myText;
	public Text sf;
	public Text srf;
	public Text slf;
	public Text srm;
	public Text slm;
	public Text srb;
	public Text slb;
	public Text sb;
	public Text gps;
	public GameObject car;
	//the state, true is off, false is on
	private bool state;


	// Use this for initialization
	void Start () {
		state = true;
		myText.text = "Sensor: View";
		sf.text = "";
		srf.text = "";
		slf.text = "";
		srm.text = "";
		slm.text = "";
		srb.text = "";
		slb.text = "";
		sb.text = "";
		gps.text = "";

	}

	void Update(){
		if(!state)
		{
			CarController m_Car = (CarController) car.GetComponent(typeof(CarController));
			List<float> sensors = m_Car.getSensors ();
			List<int> mgps = m_Car.getGPS ();

			if (sensors.Count != 0) {
				float front = (float) sensors [0];
				sf.text = "Front: "+front.ToString ("N2");
				float rightf = (float) sensors [1];
				srf.text = "Right_F: "+rightf .ToString ("N2");
				float leftf = (float) sensors [2];
				slf.text = "Left_F: "+rightf .ToString ("N2");
				float rightm = (float) sensors [3];
				srm.text = "Right_M: "+rightm .ToString ("N2");
				float leftm = (float) sensors [4];
				slm.text = "Left_M: "+leftm .ToString ("N2");
				float rightb = (float) sensors [5];
				srb.text = "Right_B: "+rightb .ToString ("N2");
				float leftb = sensors [6];
				slb.text = "Left_B: "+leftb .ToString ("N2");
				float back = sensors [7];
				sb.text = "Back: "+back .ToString ("N2");

				gps.text = "GPS: "+mgps [0].ToString ("N0") + " , " + mgps [1].ToString ("N0");
			}
		}
	}
	
	// Update is called once per frame
	public void Toggle(){
		state = !state;
		if(state){
			myText.text = "Sensor: View";
			sf.text = "";
			srf.text = "";
			slf.text = "";
			srm.text = "";
			slm.text = "";
			srb.text = "";
			slb.text = "";
			sb.text = "";
			gps.text = "";
		}
		else{
			myText.text = "Sensor: Hide";
		}
	}
}
}
