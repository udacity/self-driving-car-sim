using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Record : MonoBehaviour {

	private string m_saveLocation = "";
	public const string DirFrames = "IMG";
	public const string CSVFileName = "driving_log.csv";

	[SerializeField]
	private List <CarSample> cs;


	private int TotalSamples;
	private bool isSaving;
	private Vector3 saved_position;
	private Quaternion saved_rotation;


	[SerializeField]
	CalculateVelocity calculateVelocity;
	[SerializeField]
	Dot_Truck_Controller dotTruckCntroller;

	[SerializeField]
	Transform ObjectToRecord;
	[SerializeField]
	Camera [] cameraArr;
	[SerializeField]
	Camera  frontCamera;


	[SerializeField]
	ObjectsToRecord[] toRec;



	// Use this for initialization
	void Start () {
		Debug.Log (gameObject.name);
		toRec = FindObjectsOfType<ObjectsToRecord> ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private bool m_isRecording = false;
	public bool IsRecording {
		get
		{
			return m_isRecording;
		}

		set
		{
			m_isRecording = value;
			if(value == true)
			{ 
				Debug.Log ("Starting to record");
				//					carSamples = new Queue<CarSample>();
				cs = new List<CarSample>();
				StartCoroutine(Sample());             
			} 
			else
			{
				Debug.Log("Stopping record");
				StopCoroutine (Sample ());
				Debug.Log("Writing to disk");
				//save the cars coordinate parameters so we can reset it to this properly after capturing data
				saved_position = ObjectToRecord.position;
				saved_rotation = ObjectToRecord.rotation;
				//see how many samples we captured use this to show save percentage in UISystem script
//				TotalSamples = carSamples.Count;
				TotalSamples = cs.Count;
				isSaving = true;
				StartCoroutine(WriteSamplesToDisk());

			};
		}

	}


	public bool checkSaveLocation()
	{
		if (m_saveLocation != "") 
		{
			return true;
		}
		else
		{
			SimpleFileBrowser.ShowSaveDialog (OpenFolder, null, true, null, "Select Output Folder", "Select");
		}
		return false;
	}

	private void OpenFolder(string location)
	{
		m_saveLocation = location;
		Directory.CreateDirectory (Path.Combine(m_saveLocation, DirFrames));
	}

	public IEnumerator WriteSamplesToDisk()
	{
		yield return new WaitForSeconds(0.000f); //retrieve as fast as we can but still allow communication of main thread to screen and UISystem
		for (int i = 0; i < toRec.Length; i++) {
			toRec [i].PlayBack ();
		}
		if (cs.Count > 0) {
			//pull off a sample from the que
			//				CarSample sample = carSamples.Dequeue();
			CarSample sample = cs[0];
			cs.RemoveAt (0);


			//pysically moving the car to get the right camera position
//			ObjectToRecord.position = sample.position;
//			ObjectToRecord.rotation = sample.rotation;

			// Capture and Persist Image
			string centerPath = WriteImage (cameraArr[0], "center", sample.timeStamp);
			string leftPath = WriteImage (cameraArr[1], "left", sample.timeStamp);
			string rightPath = WriteImage (cameraArr[2], "right", sample.timeStamp);
//			string frontViewPath = WriteImage (frontCamera, "front", sample.timeStamp);

			//
			string row = string.Format ("{0},{1},{2},{3},{4}\n", centerPath, leftPath , rightPath, sample.steeringAngle, sample.speed);
//			string row = string.Format ("{0},{1},{2}\n", frontViewPath, sample.steeringAngle, sample.speed);

			File.AppendAllText (Path.Combine (m_saveLocation, CSVFileName), row);
		}
		if (cs.Count > 0) {
			//request if there are more samples to pull
			StartCoroutine(WriteSamplesToDisk()); 
		}
		else 
		{
			//all samples have been pulled
			StopCoroutine(WriteSamplesToDisk());
			isSaving = false;

			//need to reset the car back to its position before ending recording, otherwise sometimes the car ended up in strange areas
//			ObjectToRecord.position = saved_position;
//			ObjectToRecord.rotation = saved_rotation;
			// Stop the object 
			if (GameManager.instance.replayState != ReplayState.None)
				GameManager.instance.replayState = ReplayState.None;

			ObjectToRecord.GetComponent<Rigidbody>().isKinematic = true;
			ObjectToRecord.GetComponent<Rigidbody>().isKinematic = false;



		}
	}

	public float getSavePercent()
	{
		return (float)(TotalSamples-cs.Count)/TotalSamples;
	}

	public bool getSaveStatus()
	{
		return isSaving;
	}


	public IEnumerator Sample()
	{
		// Start the Coroutine to Capture Data Every Second.
		// Persist that Information to a CSV and Perist the Camera Frame
		if (GameManager.instance.replayState != ReplayState.Record)
			GameManager.instance.replayState = ReplayState.Record;
		yield return new WaitForSeconds(0.0666666666666667f);
//		yield return new WaitForSeconds(0.0000f);
		//

		for (int i = 0; i < toRec.Length; i++) {
			toRec [i].Record ();
		}

		if (m_saveLocation != "")
		{
			CarSample sample = new CarSample();

			sample.timeStamp = System.DateTime.Now.ToString ("yyyy_MM_dd_HH_mm_ss_fff");
			sample.steeringAngle = dotTruckCntroller.steeringAngle;
//			sample.throttle = 0.0f ; // AccelInput; 
//			sample.brake = 0.0f ; // BrakeInput;
			sample.speed = calculateVelocity.outputSpeed;
//			Debug.Log ("Sample Speed : "+sample.speed);

			sample.position = ObjectToRecord.position;
			sample.rotation = ObjectToRecord.rotation;

			//                carSamples.Enqueue(sample);
			cs.Add (sample);

			sample = null;
			//may or may not be needed
		}
		//
		// Only reschedule if the button hasn't toggled
		if (IsRecording)
		{
			StartCoroutine(Sample());
		}

	}

	private string WriteImage (Camera camera, string prepend, string timestamp)
	{
		if (GameManager.instance.replayState != ReplayState.PlayBack)
			GameManager.instance.replayState = ReplayState.PlayBack;
		//needed to force camera update 
//		Debug.Log("WriteImage");
        camera.Render();
        RenderTexture targetTexture = camera.targetTexture;
        RenderTexture.active = targetTexture;
        Texture2D texture2D = new Texture2D (targetTexture.width, targetTexture.height, TextureFormat.RGB24, false);
        texture2D.ReadPixels (new Rect (0, 0, targetTexture.width, targetTexture.height), 0, 0);
        texture2D.Apply ();
        byte[] image = texture2D.EncodeToJPG ();
        UnityEngine.Object.DestroyImmediate (texture2D);
		string directory = Path.Combine(m_saveLocation, DirFrames);
		string path = Path.Combine(directory, prepend + "_" + timestamp + ".jpg");
	    File.WriteAllBytes (path, image);
	    image = null;
		return path;
	}
}
[System.Serializable]
public class CarSample
{
	public Quaternion rotation;
	public Vector3 position;
	public float steeringAngle;
//	public float throttle;
//	public float brake;
	public float speed;
	public string timeStamp;
}