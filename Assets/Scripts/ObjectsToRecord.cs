using UnityEngine;
using System.Collections.Generic;

public class ObjectsToRecord : MonoBehaviour {
	private const int buffersize = 1000;
	private Queue<MyKeyFrame> keyFrameQueue;
	public List <MyKeyFrame> debugList;

	void Start () {
		keyFrameQueue = new Queue<MyKeyFrame>();
		debugList = new List<MyKeyFrame> (keyFrameQueue);
	}


//	void Update () {
//		if (GameManager.instance.replayState == ReplayState.Record) {
//			//Record ();
//		} else if(GameManager.instance.replayState == ReplayState.PlayBack) {
//			//PlayBack ();
//		}
//	}

	public void PlayBack(){
		if (keyFrameQueue.Count > 0) {
			var thisMyKeyFrame = keyFrameQueue.Dequeue ();
			debugList.RemoveAt (0);
			this.transform.localPosition = thisMyKeyFrame.Position;
			this.transform.localRotation = thisMyKeyFrame.rotation;
		} else {
			GameManager.instance.replayState = ReplayState.None;
		}
	}

	public void Record(){
		int frame = Time.frameCount % buffersize;
		float time = Time.time;
//		Debug.Log (keyFrameQueue.Count + gameObject.name);
		keyFrameQueue.Enqueue (new MyKeyFrame (time, this.transform.localPosition, this.transform.localRotation));
		debugList.Add ((new MyKeyFrame (time, this.transform.localPosition, this.transform.localRotation)));
	}
}
[System.Serializable]
public struct MyKeyFrame {
	public float frametime;
	public Vector3 Position;
	public Quaternion rotation;

	public MyKeyFrame (float atime,Vector3 apos, Quaternion arotation){
		frametime = atime;
		Position = apos;
		rotation = arotation;
	}
}