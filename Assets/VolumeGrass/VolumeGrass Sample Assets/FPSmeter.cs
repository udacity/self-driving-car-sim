using UnityEngine;
using System.Collections;

public class FPSmeter : MonoBehaviour {

	// A FPS counter.
	// It calculates frames/second over each updateInterval,
	// so the display does not keep changing wildly.
	
	public float updateInterval = 0.5f;
	private float lastInterval; // Last interval end time
	private int frames = 0; // Frames over current interval
	public static float fps; // Current FPS
	
	void Start() {
		lastInterval = Time.realtimeSinceStartup;
		frames = 0;
	}
	
	void Update() {
		++frames;
		float timeNow = Time.realtimeSinceStartup;
		if( timeNow > lastInterval + updateInterval ) {
			fps = frames / (timeNow - lastInterval);
			frames = 0;
			lastInterval = timeNow;
		}
		return;
	}
}
