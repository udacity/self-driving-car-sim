using UnityEngine;
using System.Collections;

public class SetupForGrassRendering : MonoBehaviour {
	
	//
	// you may want to increase this value to render depth for objects placed farther
	// (or decrease it to clip more geometry closer to observer)
	//
	private float FarClip=40f;

	//
	// IMPORTANT: if you enable motion blur controlling remember that it take effect only when MBLUR define is present in shader code
	//
	// call this function if your camera is going to be "teleported" into another position
	// to avoid motion blur after
	//
	public void ResetCamPos() {
		inited=false;
	}
	// if previous camera position is farther than teleportTreshold ResetCamPos() will be called automaticaly
	private float teleportTreshold=10.0f;
	// if previous camera position is closer than standTreshold no motion blur update will be done (camera is moving too slowly)
	private float standTreshold=0.0f;
	
	//-------------------------------------------------------------------------------
	
	private Camera myCam;
	private Shader shad;
	private RenderTexture myRenderTexture=null;
	public LayerMask cullingMask;
	public bool useCustomDepthShader=false;
	public VolumeGrass[] motionBlurGrassObjects;
	public bool motionBlur=false;
	public float motionBlurMultiplier=0.1f;
	public static float ZBufferParamA,ZBufferParamB,ZBufferFarClip;
	public bool renderDepth=true; // if depth rendering is not enabled, turn it on (used in ZTest projector)
	private Vector3 lPos;
	private float teleportTresholdSqr;
	private bool inited=false;
	
	void Awake() {
		if (GetComponent<Camera>()==null) {
			Debug.LogError("SetupForGrassRendering script (at "+gameObject.name+") - can't find camera component !");
			return;
		}
		
		if ((useCustomDepthShader) && (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))) {
			shad=Shader.Find("GrassRenderDepth"); // our simple depth rendering shader (rely on native z-depth buffer and don't render into color channels)
		} else {
			shad=Shader.Find("Hidden/Camera-DepthTexture"); // unity's render depth - probably slower as it renders everything into color buffer
		}
		if (!shad) {
			// we've got no shader ? Make simple one (handles native z-buffer for Opaque RenderType only)
			Material mat=new Material("Shader \"GrassRenderDepth\" {SubShader { Tags { \"RenderType\"=\"Opaque\"} \n Pass { ColorMask 0 }}}");

			Debug.Log (mat.color);
//			Shader"RenderDepth"{SubShader{Tags}} 

//			var mat_new = new Material(Shader."RenderDepth"));

			shad=mat.shader;
		}
		SetupTexture();
		GameObject go=new GameObject("GrassDepthCamera");
		go.AddComponent(typeof(Camera));
		go.transform.parent=transform;
		myCam=go.GetComponent<Camera>();
		SetupParams();
		
		teleportTresholdSqr = teleportTreshold * teleportTreshold;
	}
	private void SetupTexture() {
		if (myRenderTexture!=null) {
			myRenderTexture.Release();
		}
		myRenderTexture=new RenderTexture(Screen.width, Screen.height, 16);
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth)) {
			myRenderTexture.format=RenderTextureFormat.Depth;
		}
		myRenderTexture.Create();
		myRenderTexture.filterMode=FilterMode.Point;
		RenderTexture.active=myRenderTexture;
		myRenderTexture.SetGlobalShaderProperty("_GrassDepthTex");
	}
	private void SetupParams() {
		GetComponent<Camera>().cullingMask=GetComponent<Camera>().cullingMask&(~(1<<GrassRenderingReservedLayer.layer_num));
		myCam.CopyFrom(GetComponent<Camera>());
		myCam.targetTexture=myRenderTexture;
		myCam.cullingMask=cullingMask|(1<<GrassRenderingReservedLayer.layer_num);
		myCam.depth=GetComponent<Camera>().depth-1;
		myCam.SetReplacementShader(shad,"RenderType");
		myCam.renderingPath = RenderingPath.Forward;
		myCam.clearFlags = CameraClearFlags.SolidColor;
		myCam.backgroundColor = Color.white;
		
		float zc0, zc1;
		//
		// in fact - we use ONLY 0..1 range as depth texture representation (even in OpenGL)
		// refer to Aras The Mightful thread here:
		// http://forum.unity3d.com/threads/39332-_ZBufferParams-values?highlight=LinearEyeDepth
		// 
		//if (Application.platform==RuntimePlatform.WindowsEditor || Application.platform==RuntimePlatform.WindowsPlayer || Application.platform==RuntimePlatform.WindowsWebPlayer) {
			// D3D depth linearization factors
			zc0 = 1.0f - FarClip / GetComponent<Camera>().nearClipPlane;
			zc1 = FarClip / GetComponent<Camera>().nearClipPlane;
		//} else {
		//	zc0 = (1.0f - FarClip / camera.nearClipPlane) / 2.0f;
		//	zc1 = (1.0f + FarClip / camera.nearClipPlane) / 2.0f;
		//}
		ZBufferParamA=zc0/FarClip;
		ZBufferParamB=zc1/FarClip;
		ZBufferFarClip=FarClip;
		myCam.farClipPlane=FarClip;
	}
	
	void OnEnable() {
		if (renderDepth && SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth)) { 
			GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;	
		}
	}
	
	void Start() {
		OnEnable(); // to be sure we've got Depth Texture
	}

	void Update() {
		if ((Screen.width!=myRenderTexture.width) || (Screen.height!=myRenderTexture.height)) {
			SetupTexture();
			SetupParams();
		}
		if (!motionBlur) return;
		//Debug.Log(camera.depthTextureMode);
		Vector3 nPos=transform.position;
		if (inited) {
			Vector3 delta=nPos-lPos;
			float deltaSqrM=delta.sqrMagnitude;
			if (deltaSqrM<standTreshold) {
				lPos=nPos;
				return;
			}
			if (deltaSqrM<teleportTresholdSqr) {
				float len=Mathf.Sqrt(delta.x*delta.x+delta.z*delta.z)*motionBlurMultiplier;
				if (len>1) len=1;
				if (Vector3.Dot(transform.forward,delta)<0) {
					delta=-delta;
				}
				delta.Normalize();
				if (delta.y<0.05f) delta.y=0.05f;
				Vector4 deltaM=new Vector4(-delta.x, -delta.z, delta.y, 0);
				for(int i=0; i<motionBlurGrassObjects.Length; i++) {
					VolumeGrass obj=motionBlurGrassObjects[i];
					if (obj.GetComponent<Renderer>() && obj.GetComponent<Renderer>().material) {
						obj.GetComponent<Renderer>().material.SetVector("_mblur_dir", deltaM);
						obj.GetComponent<Renderer>().material.SetFloat("_mblur_val", len);
					}
				}
				lPos+=(nPos-lPos)*0.25f;
			} else {
				lPos=nPos;
			}
		}
		inited=true;
		lPos=nPos;
	}
	
	public void autoFillMBlurArray() {
		motionBlurGrassObjects=(VolumeGrass[])GameObject.FindSceneObjectsOfType(typeof(VolumeGrass));
	}
	
	/*
	void OnGUI() {
		GUILayout.BeginArea(new Rect (400,5,400,20));
		//GUILayout.Label(myRenderTexture.width.ToString("f2")+"    "+myRenderTexture.height.ToString("f2"));
		GUILayout.Label(myCam.rect.ToString()+"");
		//Screen.width
		GUILayout.EndArea();
	}
	*/
}
