//////////////////////////////////////////////////////////////////////////////////////////////////////
//
// VolumeGrass class. Add this script component to a game object to be used as placeholder
// for your nice looking grass. View readme for further details.
//
// (C) Tomasz Stobierski 2011
//
//////////////////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using Poly2Tri;

public class VolumeGrass : MonoBehaviour {
	//
	// function for testing "old hardware"
	// you may want to define it by another criteria
	// when "old hardware" is detected and "Hide on uncomatible hardware" is checked
	// mesh is hidden
	//
	bool oldHardwareDefTest() {
		return (SystemInfo.graphicsShaderLevel<30);
	}
	
	//
	// function that check if we're on Mac and GPU other than geForce 8000 and above is detected (on PC _compatibility() returns true)
	//
	// that's important because of shader using:
	// on PC we assume to use "Grass Shader"
	// on Macs we assume to use "Grass Shader" if function below returns true
	//                          "Grass Shader ALT" if function below returns false (shader code without early exit optimizations)
	//
	// NOTE: using "Grass Shader" shader on incompatible Macs may lead to crash
	//
	bool _compatibility() {
		if (Application.platform!=RuntimePlatform.WindowsEditor && Application.platform!=RuntimePlatform.WindowsPlayer && Application.platform!=RuntimePlatform.WindowsWebPlayer) {
			string GPU=(SystemInfo.graphicsDeviceName).ToLower();
			string[] GPUids=GPU.Split(' ');
			if (GPU.Contains("nvidia")) {
				for(int i=0; i<GPUids.Length; i++) {
					int ver;
					string noM="-1";
					if (GPUids[i].Substring(GPUids[i].Length-1)=="m") {
						noM=GPUids[i].Substring(0,GPUids[i].Length-1);
					}
					if (Int32.TryParse(GPUids[i], out ver) || Int32.TryParse(noM, out ver)) {
						if ( ((ver>100) && (ver<1000)) || (ver>8000) ) {
							// will (probably) work
							return true;
						}
					}
				}
			}
			return false;
		} else {
			return true;
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//
	// if you care about your mental sanity you don't have to analyse code below :)
	// (unless you really need to tweak something)
	//
	
	public bool showNodeNumbers=true;
	public List<bool> optimize_subnodes=new List<bool>();
	public float colinear_treshold=170f;
	public int bezier_subdivisions=5;
	public int state =0;
	public string[] stateStrings = new string[] {"Edit", "Build"};
	public int act_lod=0;
	public string[] lodStrings = new string[4];
	public string[] lodStrings_empty = new string[] {"(empty)", "(empty)", "(empty)", "(empty)"};
	public string[] lodStrings_occupied = new string[] {"LOD 0", "LOD 1", "LOD 2", "LOD 3"};
	public int localGlobalState=0; 
	public string[] localGlobalStrings = new string[] {"Local", "Global"};
	public List<Vector3> control_points = new List<Vector3>();
	public List<int> subdivisions = new List<int>();
	public List<bool> side_walls = new List<bool>();
	public List<Vector3> bezier_pointsA = new List<Vector3>();
	public List<Vector3> bezier_pointsB = new List<Vector3>();
	public List<Vector3> tesselation_points = new List<Vector3>();
	public int active_idx=-1;
	public int which_active=0;
	public bool snap_on_move=true; 
	public bool snap_always=false;
	public bool snap_on_build=true; 
	private bool adjust_warning_flag=true; 
	public LayerMask ground_layerMask=0;
	public bool fullBackGeometry=false;
	
	public float UV_ratio=1.0f;
	public int slices_num=4;
	public float plane_num=7;
	public float mesh_height=0.25f; 
	public float add_height_offset=0; 
	public float[] max_y_error=new float[]{0.05f, 0.1f, 0.15f, 0.2f}; 
	public float[] min_edge_length=new float[]{0.6f, 1.5f, 3f, 5f}; 
	
	private Vector3 bound_min=new Vector3(0,0,0);
	private Vector3 bound_max=new Vector3(0,0,0);
	private Vector3 last_midp=new Vector3(0,0,0);
	private float nextRecenterTim=0;
	
	private Vector3 insert_dir=new Vector3(0,0,0);

	public Mesh[] LODs=new Mesh[4];
	public Mesh[] LODs_sidewalls=new Mesh[4];
	public int[] LOD_distances=new int[] {4000, 200, 400, 600};
	public int mapping_grid_size=100;
	public float[] TilingFactors=new float[4];
	
	private Vector2[,] grid=new Vector2[1,1];
	private Vector2[,] gridB;
	private float[,] _z;
	public float minx,maxx,minz,maxz;
	public bool reinitUV=true;
	public bool custom_UV_bounds=false;
	public float custom_minx, custom_maxx, custom_minz, custom_maxz;
	
	public Vector2 UV2range=new Vector2(1,1);
	
	[System.NonSerialized]
	public bool undo_flag=false;
	
	public bool checkerMatFlag=false;
	public bool show_tesselation_points=true;
	private Material checkerMat;
	public Material myMat;
	
	public bool hide_on_old_hardware=false;
	public bool useOGL=false;
	
	public bool auto_border_transitions=false;
	public bool paint_height=false;
	public float paint_size=5;
	public float paint_smoothness=0;
	public float paint_opacity=1;
	private Vector3[] volume_vertices;
	private Vector3[] volume_normals;
	private Color[] volume_colors;
	
	void Awake() {
		// fallback for texture2Dlod/conditionals Mac uncompatible cards
		if (GetComponent<Renderer>() && GetComponent<Renderer>().material) {
			if (Application.platform!=RuntimePlatform.WindowsEditor) useOGL=false;
			checkOGL();
		}
	}
	
	public void checkOGL() {
		int shader_maxLOD;
		if (Application.isPlaying) {
			if (GetComponent<Renderer>() && GetComponent<Renderer>().material) {
				shader_maxLOD=GetComponent<Renderer>().material.shader.maximumLOD;
				if ((!_compatibility()) || useOGL) {
					// assuming we're not on Win, we can't let shader run on potentially incompatible hardware (below GeForce8xxx)
					GetComponent<Renderer>().material.shader=Shader.Find("Grass Shader ALT"); // don't use optimized subshader
					// disable mipmaping (if present)
					//disableMIPs(renderer.material, "_BladesTex");
					//disableMIPs(renderer.material, "_BladesBackTex");
				} else {
					GetComponent<Renderer>().material.shader=Shader.Find("Grass Shader"); // use optimized version
				}
				GetComponent<Renderer>().material.shader=Shader.Find("Grass Shader"); // By Manas
				GetComponent<Renderer>().material.shader.maximumLOD=shader_maxLOD;
			}
		} else {
			if (GetComponent<Renderer>() && GetComponent<Renderer>().sharedMaterial) {
				shader_maxLOD=GetComponent<Renderer>().sharedMaterial.shader.maximumLOD;
				if ((!_compatibility()) || useOGL) {
					GetComponent<Renderer>().sharedMaterial.shader=Shader.Find("Grass Shader ALT");
				} else {
					GetComponent<Renderer>().sharedMaterial.shader=Shader.Find("Grass Shader"); // use optimized version
				}
				GetComponent<Renderer>().material.shader=Shader.Find("Grass Shader"); // By Manas
				GetComponent<Renderer>().sharedMaterial.shader.maximumLOD=shader_maxLOD;
			}
		}
	}
	/*
	void disableMIPs(Material mat, string tex_name) {
		Texture2D tex=(Texture2D)mat.GetTexture(tex_name);
		if (tex && (tex.mipmapCount>1)) {
			Color[] bmp_data;
			try {
				bmp_data=tex.GetPixels();
			} catch (UnityException e) {
				Debug.LogError("Your grassblades texture is not marked as readable ("+tex.name+") !");
				return;
			}
			Texture2D no_mip_tex=new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
			no_mip_tex.filterMode=FilterMode.Bilinear;
			no_mip_tex.anisoLevel=0;
			no_mip_tex.SetPixels(bmp_data);
			no_mip_tex.Apply();
			no_mip_tex.Compress(false);
			mat.SetTexture(tex_name, no_mip_tex);
		}
	}
	*/
	void Start() {
		Transform tr=transform.Find("sidewalls");
		if (oldHardwareDefTest()) {
			if (hide_on_old_hardware) {
				if (tr!=null) {
					tr.GetComponent<Renderer>().enabled=false;
				}
				if (GetComponent<Renderer>()) {
					GetComponent<Renderer>().enabled=false;
				}
			} else {
				Vector3[] vertices;
				Vector3[] normals;
				for(int lod=0; lod<4; lod++) {
					if (LODs[lod]) {
						vertices=LODs[lod].vertices;
						normals=LODs[lod].normals;
						for(int i=0; i<vertices.Length; i++) {
							vertices[i]-=normals[i]*mesh_height*0.9f;
						}
						LODs[lod].vertices=vertices;
					}
				}
			}
			return;
		}
		if (GetComponent<Renderer>() && GetComponent<Renderer>().material) {
			GetComponent<Renderer>().material.SetFloat("_ZBufferParamA", SetupForGrassRendering.ZBufferParamA);
			GetComponent<Renderer>().material.SetFloat("_ZBufferParamB", SetupForGrassRendering.ZBufferParamB);
			GetComponent<Renderer>().material.SetFloat("_ZBufferFarClip", SetupForGrassRendering.ZBufferFarClip-1);
			GetComponent<Renderer>().material.SetVector("_auxtex_tiling", new Vector4(1.0f/(maxx-minx)/UV_ratio, 1.0f/(maxz-minz)/UV_ratio, -minx*UV_ratio,-minz*UV_ratio));
			GetComponent<Renderer>().material.SetFloat("_tiling_factor", TilingFactors[act_lod]);
		}
		if (fullBackGeometry) {
			GameObject go=new GameObject("backcut");
			MeshFilter mf,my_mf;
			go.layer=GrassRenderingReservedLayer.layer_num;
			tr=go.transform;
			tr.position=transform.position;
			tr.rotation=transform.rotation;
			tr.localScale=transform.localScale;
			tr.parent=transform;
		    go.AddComponent(typeof(MeshRenderer));
	        mf = go.AddComponent(typeof(MeshFilter)) as MeshFilter;
			my_mf = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
			if (mf && my_mf) {
				Mesh msh=new Mesh();
				int[] my_triangles=my_mf.mesh.triangles;
				int[] triangles=new int[my_triangles.Length];
				for(int i=0; i<triangles.Length; i+=3) {
					triangles[i]=my_triangles[i+2];
					triangles[i+1]=my_triangles[i+1];
					triangles[i+2]=my_triangles[i];
				}
				msh.vertices=my_mf.mesh.vertices;
				msh.triangles=triangles;
				msh.RecalculateNormals();
			    mf.mesh = msh;
				go.GetComponent<Renderer>().material=new Material(Shader.Find("GrassRenderDepth"));
				go.GetComponent<Renderer>().receiveShadows=false;
				go.GetComponent<Renderer>().castShadows=false;
			}
			tr=transform.Find("sidewalls");
			if (tr!=null) {
				tr.gameObject.active=false;
			}
		}
	}
	
	public void setupLODAndShader(int lod, bool shared_flag) {
		MeshFilter filter=GetComponent(typeof(MeshFilter)) as MeshFilter;
		MeshFilter filter_sidewalls=null;
		
		Transform tr=transform.Find("sidewalls");
		if (tr!=null) {
			tr.gameObject.layer=GrassRenderingReservedLayer.layer_num;
			filter_sidewalls=tr.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		}
		if (shared_flag) {
			if (filter) filter.sharedMesh=LODs[lod];
			if (filter_sidewalls) filter_sidewalls.sharedMesh=LODs_sidewalls[lod];
			if ((GetComponent<Renderer>()!=null) && (GetComponent<Renderer>().sharedMaterial!=null)) {
				GetComponent<Renderer>().sharedMaterial.SetFloat("_tiling_factor", TilingFactors[lod]);
				GetComponent<Renderer>().sharedMaterial.SetFloat("PLANE_NUM", plane_num);
				GetComponent<Renderer>().sharedMaterial.SetFloat("GRASS_SLICE_NUM", 1.0f*slices_num);
				GetComponent<Renderer>().sharedMaterial.SetFloat("PLANE_NUM_INV", (1.0f/plane_num));
				GetComponent<Renderer>().sharedMaterial.SetFloat("GRASS_SLICE_NUM_INV", (1.0f/slices_num));
				GetComponent<Renderer>().sharedMaterial.SetFloat("GRASSDEPTH", (1.0f/slices_num));
				GetComponent<Renderer>().sharedMaterial.SetFloat("PREMULT", (plane_num/slices_num));
				GetComponent<Renderer>().sharedMaterial.SetFloat("UV_RATIO", UV_ratio);
				GetComponent<Renderer>().sharedMaterial.SetVector("_auxtex_tiling", new Vector4(1.0f/(maxx-minx)/UV_ratio, 1.0f/(maxz-minz)/UV_ratio, -minx*UV_ratio,-minz*UV_ratio));
			}
		} else {
			if (filter) filter.mesh=LODs[lod];
			if (filter_sidewalls) filter_sidewalls.mesh=LODs_sidewalls[lod];
			if ((GetComponent<Renderer>()!=null) && (GetComponent<Renderer>().material!=null)) {
				GetComponent<Renderer>().material.SetFloat("_tiling_factor", TilingFactors[lod]);
			}				
		}
		
		volume_vertices=null;
		volume_normals=null;
		volume_colors=null;
	}
	
#if UNITY_EDITOR
	public void AddControlPoint(Vector3 pos, int index) {
		pos=T_wl(pos);
		if (index<0) {
			control_points.Add(pos);
			bezier_pointsA.Add(pos);
			bezier_pointsB.Add(pos);
			subdivisions.Add(bezier_subdivisions);
			side_walls.Add(true);
			optimize_subnodes.Add(true);
		} else {
			Vector3 dir_vec=control_points[(index+1)%control_points.Count] - control_points[index];
			dir_vec.Normalize();
			if (Vector3.Distance(dir_vec, insert_dir)>0.001f) {
				Vector3 pos_bezierA=new Vector3(pos.x, pos.y, pos.z)+insert_dir;
				Vector3 pos_bezierB=new Vector3(pos.x, pos.y, pos.z)-insert_dir;
				bezier_pointsA.Insert(index+1,pos_bezierA);
				bezier_pointsB.Insert(index+1,pos_bezierB);
			} else {
				bezier_pointsA.Insert(index+1,pos);
				bezier_pointsB.Insert(index+1,pos);
			}
			control_points.Insert(index+1,pos);
			subdivisions.Insert(index+1, bezier_subdivisions);
			side_walls.Insert(index+1, true);
			optimize_subnodes.Insert(index+1, true);
		}
	}
	public void AddTesselationPoint(Vector3 pos) {
		tesselation_points.Add(T_wl(pos));
	}
	public void DeleteActiveControlPoint() {
		if (which_active==3) {
			if ((active_idx>=0) && (active_idx<tesselation_points.Count)) {
				tesselation_points.RemoveAt(active_idx);
				if (active_idx>=tesselation_points.Count) {
					active_idx=tesselation_points.Count-1;
				}
			}
		} else if ((active_idx>=0) && (active_idx<control_points.Count)) {
			if (which_active==0) {
				control_points.RemoveAt(active_idx);
				bezier_pointsA.RemoveAt(active_idx);
				bezier_pointsB.RemoveAt(active_idx);
				subdivisions.RemoveAt(active_idx);
				side_walls.RemoveAt(active_idx);
				optimize_subnodes.RemoveAt(active_idx);
			} else if (which_active==1) {
				bezier_pointsA[active_idx]=new Vector3(control_points[active_idx].x, control_points[active_idx].y, control_points[active_idx].z);
			} else if (which_active==2) {
				bezier_pointsB[active_idx]=new Vector3(control_points[active_idx].x, control_points[active_idx].y, control_points[active_idx].z);
			}
			which_active=0;
			if (active_idx>=control_points.Count) {
				active_idx=control_points.Count-1;
			}
		}
	}
	
	public void Recenter() {
		if (control_points.Count<1) return;
		if (Time.realtimeSinceStartup<nextRecenterTim) return;
		nextRecenterTim = Time.realtimeSinceStartup+0.1f;
		
		Vector3 midp=0.5f*(bound_min+bound_max);
		if (Vector3.Distance(midp, last_midp)<0.001f) return;
		last_midp=midp;
		Vector3 delta_midp=transform.position-midp;
		for(int i=0; i<control_points.Count; i++) {
			control_points[i]=T_wl(T_lw(control_points[i]) + delta_midp);
			bezier_pointsA[i]=T_wl(T_lw(bezier_pointsA[i]) + delta_midp);
			bezier_pointsB[i]=T_wl(T_lw(bezier_pointsB[i]) + delta_midp);
		}
		for(int i=0; i<tesselation_points.Count; i++) {
			tesselation_points[i]=T_wl(T_lw(tesselation_points[i]) + delta_midp);
		}
		transform.position=midp;
	}

	void OnDrawGizmos() {
		int i;
		
		Gizmos.DrawIcon(transform.position, "..\\VolumeGrass\\_Internal\\grass.png");
		if (control_points.Count<2) return;
		if (Selection.Contains(gameObject) && custom_UV_bounds) {
			Handles.color=Color.yellow;
			Vector3 p1,p2,p3,p4;
			p1=new Vector3(custom_minx, transform.position.y, custom_minz);
			p2=new Vector3(custom_maxx, transform.position.y, custom_minz);
			p3=new Vector3(custom_maxx, transform.position.y, custom_maxz);
			p4=new Vector3(custom_minx, transform.position.y, custom_maxz);
			Handles.DrawLine(p1, p2);
			Handles.DrawLine(p2, p3);
			Handles.DrawLine(p3, p4);
			Handles.DrawLine(p4, p1);
		}
		if (state==1) return;
		if (snap_always) SnapAll();
		
		InitBounds(T_lw(control_points[0]));
		Handles.color=Color.green;
		for(i=0; i<control_points.Count; i++) {
			if ((i==1) && (control_points.Count==2)) break;
			Vector3 point_start=T_lw(control_points[i]);
			Vector3 point_end=T_lw(control_points[(i+1)%control_points.Count]);
			Vector3 tangent_start=T_lw(bezier_pointsA[i]);
			Vector3 tangent_end=T_lw(bezier_pointsB[(i+1)%control_points.Count]);
			if (Vector3.Distance(point_start, tangent_start)<0.001f) {
				tangent_start=(point_start*2.0f+point_end*1.0f)/3.0f;
			}
			if (Vector3.Distance(point_end, tangent_end)<0.001f) {
				tangent_end=(point_end*2.0f+point_start*1.0f)/3.0f;
			}
			Vector3 pntA=point_start;
			UpdateBounds(pntA);
			Vector3 pntB;
			for(int j=1; j<=subdivisions[i]; j++) {
				float t=1.0f*j/(subdivisions[i]);
				pntB=get_Bezier_point(t, point_start, tangent_start, point_end, tangent_end);
				Handles.DrawLine(pntA, pntB);
				Handles.DrawSolidDisc(pntB, Camera.current.transform.position-pntB, HandleUtility.GetHandleSize(pntB)*0.02f);
				UpdateBounds(pntB);
				pntA=pntB;
			}
		}
		Recenter();
	}

	Vector3 get_Bezier_point(float t, Vector3 point_start, Vector3 tangent_start, Vector3 point_end, Vector3 tangent_end) {
		return (1-t)*(1-t)*(1-t)*point_start + 3*(1-t)*(1-t)*t*tangent_start + 3*(1-t)*t*t*tangent_end + t*t*t*point_end;
	}
	public Vector3 T_lw(Vector3 input) {
		return transform.TransformPoint(input);
	}
	public Vector3 T_wl(Vector3 input) {
		return transform.InverseTransformPoint(input);
	}
	void InitBounds(Vector3 point) {
		bound_min.x=bound_max.x=point.x;
		bound_min.y=bound_max.y=point.y;
		bound_min.z=bound_max.z=point.z;
	}
	void UpdateBounds(Vector3 point) {
		if (point.x<bound_min.x) {
			bound_min.x=point.x;
		} else if (point.x>bound_max.x) {
			bound_max.x=point.x;
		}
		if (point.y<bound_min.y) {
			bound_min.y=point.y;
		} else if (point.y>bound_max.y) {
			bound_max.y=point.y;
		}
		if (point.z<bound_min.z) {
			bound_min.z=point.z;
		} else if (point.z>bound_max.z) {
			bound_max.z=point.z;
		}
	}
	public bool ConstrainPoints(Vector3 vec) {
		if (which_active==0) {
			if ((Vector3.Distance(vec, control_points[active_idx])>0)) {
				Vector3 delta_bezierA=T_lw(bezier_pointsA[active_idx])-T_lw(control_points[active_idx]);
				Vector3 delta_bezierB=T_lw(bezier_pointsB[active_idx])-T_lw(control_points[active_idx]);
				control_points[active_idx]=vec;
				bezier_pointsA[active_idx]=T_wl(T_lw(control_points[active_idx])+delta_bezierA);
				bezier_pointsB[active_idx]=T_wl(T_lw(control_points[active_idx])+delta_bezierB);
				return true;
			}
		} else if (which_active==1) {
			if ((Vector3.Distance(vec, bezier_pointsA[active_idx])>0)) {
				bezier_pointsA[active_idx]=vec;
				return true;
			}
		} else if (which_active==2) {
			if ((Vector3.Distance(vec, bezier_pointsB[active_idx])>0)) {
				bezier_pointsB[active_idx]=vec;
				return true;
			}
		} else {
			if ((Vector3.Distance(vec, tesselation_points[active_idx])>0)) {
				tesselation_points[active_idx]=vec;
				return true;
			}
		}
		return false;
	}
	private void SnapAll() {
		for(int i=0; i<control_points.Count; i++) {
			Vector3 pnt=T_lw(control_points[i]);
			if (SnapWorldPoint(ref pnt, ground_layerMask)) {
				Vector3 delta_bezierA=T_lw(bezier_pointsA[i])-T_lw(control_points[i]);
				Vector3 delta_bezierB=T_lw(bezier_pointsB[i])-T_lw(control_points[i]);
				control_points[i]=T_wl(pnt);
				bezier_pointsA[i]=T_wl(T_lw(control_points[i])+delta_bezierA);
				bezier_pointsB[i]=T_wl(T_lw(control_points[i])+delta_bezierB);
			}
		}
		for(int i=0; i<bezier_pointsA.Count; i++) {
			Vector3 pnt;
			pnt=T_lw(bezier_pointsA[i]);
			if (SnapWorldPoint(ref pnt, ground_layerMask)) {
				bezier_pointsA[i]=T_wl(pnt);
			}
			pnt=T_lw(bezier_pointsB[i]);
			if (SnapWorldPoint(ref pnt, ground_layerMask)) {
				bezier_pointsB[i]=T_wl(pnt);
			}
		}
		for(int i=0; i<tesselation_points.Count; i++) {
			Vector3 pnt=T_lw(tesselation_points[i]);
			if (SnapWorldPoint(ref pnt, ground_layerMask)) {
				tesselation_points[i]=T_wl(pnt);
			}
		}
	}
    private bool SnapWorldPoint(ref Vector3 hit, LayerMask layerMask)
    {
		float planeLevel = 0;
        var groundPlane = new Plane(Vector3.up, new Vector3(0, planeLevel, 0));

        var ray = new Ray(hit+Vector3.up*10000,-Vector3.up);
        RaycastHit rayHit;
        float dist;
		Vector3 prev=new Vector3(hit.x, hit.y, hit.z);
		
        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, 1<<layerMask.value)) {
            hit = rayHit.point;
		} else if (groundPlane.Raycast(ray, out dist)) {
            hit = ray.origin + ray.direction.normalized * dist;
		}
		if (Vector3.Distance(hit, prev)>0.001f) return true; else return false;
    }
    private bool SnapWorldPointDir(ref Vector3 hit, Vector3 dir, LayerMask layerMask)
    {
		float planeLevel = 0;
        var groundPlane = new Plane(Vector3.up, new Vector3(0, planeLevel, 0));

        var ray = new Ray(hit+Vector3.up*10000,dir);
        RaycastHit rayHit;
        float dist;
		Vector3 prev=new Vector3(hit.x, hit.y, hit.z);
		
        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, 1<<layerMask.value)) {
            hit = rayHit.point;
		} else if (groundPlane.Raycast(ray, out dist)) {
            hit = ray.origin + ray.direction.normalized * dist;
		}
		if (Vector3.Distance(hit, prev)>0.001f) return true; else return false;
    }
    private Vector3 getNormal(Vector3 pos, LayerMask layerMask)
    {
        var ray = new Ray(pos+Vector3.up*10000,-Vector3.up);
        RaycastHit rayHit;
        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, 1<<layerMask.value)) {
            return rayHit.normal;
		} else {
            return Vector3.up;
		}
    }
	
	public void GetInsertPos(Vector2 click_pos, ref Vector3 insert_pos, ref int insert_idx, int id=-1, int jd=-1) {
		int i;
		int tmp_insert_idx=-1;
		Vector3 tmp_insert_dir=Vector3.zero;
		float min_dist=100;
		for(i=0; i<control_points.Count; i++) {
			Vector3 point_start=T_lw(control_points[i]);
			Vector3 point_end=T_lw(control_points[(i+1)%control_points.Count]);
			Vector3 tangent_start=T_lw(bezier_pointsA[i]);
			Vector3 tangent_end=T_lw(bezier_pointsB[(i+1)%control_points.Count]);
			if (Vector3.Distance(point_start, tangent_start)<0.001f) {
				tangent_start=(point_start*2.0f+point_end*1.0f)/3.0f;
			}
			if (Vector3.Distance(point_end, tangent_end)<0.001f) {
				tangent_end=(point_end*2.0f+point_start*1.0f)/3.0f;
			}
			Vector3 pntA=point_start;
			Vector2 pntA2D=HandleUtility.WorldToGUIPoint(point_start);
			Vector3 pntB;
			Vector2 pntB2D;
			float dist;
			for(int j=1; j<=subdivisions[i]; j++) {
				float t=1.0f*j/(subdivisions[i]);
				pntB=get_Bezier_point(t, point_start, tangent_start, point_end, tangent_end);
				pntB2D=HandleUtility.WorldToGUIPoint(pntB);
				Vector2 dir_vec=pntB2D - pntA2D;
				float d=dir_vec.magnitude;
				dir_vec.Normalize();
				d=Vector2.Dot(click_pos - pntA2D, dir_vec)/d;
				dist=HandleUtility.DistancePointLine(new Vector3(click_pos.x, click_pos.y, 0), new Vector3(pntA2D.x, pntA2D.y, 0), new Vector3(pntB2D.x, pntB2D.y, 0));
				if ((dist<min_dist) && (d>=0) && (d<=1)) {
					min_dist=dist;
					tmp_insert_dir=pntB-pntA;
					tmp_insert_idx=i;
					insert_pos=pntA+d*tmp_insert_dir;
				}
				pntA=pntB;
				pntA2D=pntB2D;
			}
		}
		if (min_dist<5) {
			insert_idx=tmp_insert_idx;
			insert_dir=tmp_insert_dir.normalized;
		} else {
			insert_idx=-1;
		}
	}
	
	public bool BuildMesh() {
		if (control_points.Count<3) {
			EditorUtility.DisplayDialog("Error...", "Less than 3 vertices    ", "Proceed", "");
			return false;
		}
		if ((!snap_on_build) && adjust_warning_flag) {
			adjust_warning_flag=false;
			EditorUtility.DisplayDialog("Warning", "Adjust to ground setting is off.\n\nResulting mesh will be built with    \noriginal set of vertices only and\nUVs can't be refined.", "Proceed", "");
		}

		Vector3 snap_pnt;
		bool refine_flag;
		
		UV_ratio=1.0f/(mesh_height*slices_num);
		TilingFactors[act_lod]=1.0f/UV_ratio;
		
        List<Vector2> vertices2D = new List<Vector2>();
		List<float> vertices_depth = new List<float>();
		List<int> node_indices = new List<int>();
		for(int i=0; i<control_points.Count; i++) {
			Vector3 point_start=T_lw(control_points[i]);
			Vector3 point_end=T_lw(control_points[(i+1)%control_points.Count]);
			Vector3 tangent_start=T_lw(bezier_pointsA[i]);
			Vector3 tangent_end=T_lw(bezier_pointsB[(i+1)%control_points.Count]);
			if (Vector3.Distance(point_start, tangent_start)<0.001f) {
				tangent_start=(point_start*2.0f+point_end*1.0f)/3.0f;
			}
			if (Vector3.Distance(point_end, tangent_end)<0.001f) {
				tangent_end=(point_end*2.0f+point_start*1.0f)/3.0f;
			}
			if (snap_on_build) {
				SnapWorldPoint(ref point_start, ground_layerMask);
				SnapWorldPoint(ref point_end, ground_layerMask);
			}
			vertices_depth.Add(point_start.y);
			vertices2D.Add(new Vector2(point_start.x, point_start.z));
			node_indices.Add(i);
			for(int j=1; j<subdivisions[i]; j++) {
				float t=1.0f*j/(subdivisions[i]);
				snap_pnt=get_Bezier_point(t, point_start, tangent_start, point_end, tangent_end);
				if (snap_on_build) {
					SnapWorldPoint(ref snap_pnt, ground_layerMask);
				}
				vertices_depth.Add(snap_pnt.y);
				vertices2D.Add(new Vector2(snap_pnt.x, snap_pnt.z));
				node_indices.Add(i);
			}
		}
		int idx_beg=0;
		for(int t=0; t<control_points.Count; t++) {
			int idx_end;
			if (optimize_subnodes[t]) {
				idx_end=idx_beg+subdivisions[t]-1;
			} else {
				idx_end=idx_beg; 
			}
			for(int i=idx_beg+1; i<=idx_end; i++) {
				int i_prev=(i+vertices2D.Count-1)%vertices2D.Count;
				int i_next=(i+1)%vertices2D.Count;
				Vector3 pntA=new Vector3(vertices2D[i_prev].x, vertices_depth[i_prev], vertices2D[i_prev].y);
				Vector3 pntB=new Vector3(vertices2D[i].x, vertices_depth[i], vertices2D[i].y);
				Vector3 pntC=new Vector3(vertices2D[i_next].x, vertices_depth[i_next], vertices2D[i_next].y);
				if (Vector3.Angle(pntA-pntB, pntC-pntB)>colinear_treshold) {
					vertices2D.RemoveAt(i);
					vertices_depth.RemoveAt(i);
					node_indices.RemoveAt(i);
					i--;
					idx_beg--;
					idx_end--;
				}
			}
			idx_beg+=subdivisions[t];
		}
		if (vertices2D.Count<3) {
			EditorUtility.DisplayDialog("Error...", "Less than 3 vertices after reduction    \nTry increasing colinear treshold angle  ", "Proceed", "");
			return false;
		}
		
		if (snap_on_build) {
			do {
				refine_flag=false;
				for(int i=0; i<vertices2D.Count; i++) {
					int i_next=(i+1)%vertices2D.Count;
					Vector3 pntA=new Vector3(vertices2D[i].x, vertices_depth[i], vertices2D[i].y);
					Vector3 pntB=new Vector3(vertices2D[i_next].x, vertices_depth[i_next], vertices2D[i_next].y);
					if (Vector3.Distance(pntA, pntB)>min_edge_length[act_lod]*2) {
						Vector3 center=0.5f*(pntA+pntB);
						Vector3 tmp_pnt=new Vector3(center.x, center.y, center.z);
						SnapWorldPoint(ref center, ground_layerMask);
						if (Mathf.Abs(center.y-tmp_pnt.y) > max_y_error[act_lod]) {
							vertices2D.Insert(i+1, new Vector2(center.x, center.z));
							vertices_depth.Insert(i+1, center.y);
							node_indices.Insert(i+1, node_indices[i]);
							i--;
							refine_flag=true;
						}
					}
				}
			} while(refine_flag);
		}

		if (vertices2D.Count<3) {
			EditorUtility.DisplayDialog("Error...", "Less than 3 vertices    ", "Proceed", "");
			return false;
		}

		
		
        Vector2[] vertices2D_r = vertices2D.ToArray();
		float[] vertices_depth_r = vertices_depth.ToArray();
		
		var points = new List<PolygonPoint>();
		for(int i=0; i<vertices2D_r.Length; i++) {
			points.Add( new PolygonPoint( vertices2D_r[i].x ,vertices2D_r[i].y , i ) );
		}
		Polygon poly = new Polygon(points);

		List<Vector3> add_points = new List<Vector3>();
        for (int i=0; i<tesselation_points.Count; i++) {
			snap_pnt = T_lw(tesselation_points[i]);
			if (snap_on_build) {
				SnapWorldPoint(ref snap_pnt, ground_layerMask);
			}
			add_points.Add(snap_pnt);
		}
		do {
			for(int i=0; i<add_points.Count; i++) {
				poly.AddSteinerPoint( new TriangulationPoint (add_points[i].x , add_points[i].z , vertices2D_r.Length+i) );
			}
			try {
				P2T.Triangulate(poly);
			} catch ( Exception ) {
				return false;
			}
			refine_flag=false;
			if (snap_on_build) {
		        for (int i=0; i<poly.Triangles.Count; i++) {
					DelaunayTriangle tri=poly.Triangles[i];
					Vector3 center=Vector3.zero;
					if (check_subdivide(tri.Points[0].index, tri.Points[1].index, vertices2D_r, vertices_depth_r, add_points, ref center)) {
						add_points.Add(new Vector3(center.x, center.y, center.z));
						refine_flag=true;					
					}
					if (check_subdivide(tri.Points[1].index, tri.Points[2].index, vertices2D_r, vertices_depth_r, add_points, ref center)) {
						add_points.Add(new Vector3(center.x, center.y, center.z));
						refine_flag=true;					
					}
					if (check_subdivide(tri.Points[2].index, tri.Points[0].index, vertices2D_r, vertices_depth_r, add_points, ref center)) {
						add_points.Add(new Vector3(center.x, center.y, center.z));
						refine_flag=true;					
					}

		        }
				poly.ClearSteinerPoints();
			}
		} while (refine_flag && (poly.Triangles.Count<40000));
		if (poly.Triangles.Count>=40000) {
			EditorUtility.DisplayDialog("Error...", "Triangles count hit the limit of 40000    ", "Proceed", "");
			return false;
		}
       
        Vector3[] vertices = new Vector3[vertices2D_r.Length + add_points.Count];
        for (int i=0; i<vertices2D_r.Length; i++) {
            vertices[i] = T_wl(new Vector3(vertices2D_r[i].x, vertices_depth_r[i], vertices2D_r[i].y));
        }
        for (int i=0; i<add_points.Count; i++) {
            vertices[vertices2D_r.Length+i] = T_wl(add_points[i]);
        }

		int[] triangles=new int[poly.Triangles.Count*3];
        for (int i=0,j=0; i<poly.Triangles.Count; i++) {
			DelaunayTriangle tri=poly.Triangles[i];
			triangles[j++]=tri.Points[2].index;
			triangles[j++]=tri.Points[1].index;
			triangles[j++]=tri.Points[0].index;
        }
		
        Mesh msh = new Mesh();
        msh.vertices = vertices;
        msh.triangles = triangles;
        msh.RecalculateNormals();
		Vector3[] normals=msh.normals;
		int side_vertices_count=0;
		for(int i=0; i<vertices2D_r.Length; i++) {
			if (side_walls[node_indices[i]]) side_vertices_count++;
		}
		Vector3[] vertices_bottom=new Vector3[vertices2D_r.Length];
		if (snap_on_build) {
			for(int i=0; i<vertices2D_r.Length; i++) {
				vertices_bottom[i]=T_wl(T_lw(vertices[i])+getNormal(T_lw(vertices[i]), ground_layerMask)*add_height_offset);
			}
		} else {
			for(int i=0; i<vertices2D_r.Length; i++) {
				vertices_bottom[i]=vertices[i]+normals[i]*add_height_offset;
			}
		}
		if (snap_on_build) {
			for(int i=0; i<vertices.Length; i++) {
				vertices[i]=T_wl(T_lw(vertices[i])+getNormal(T_lw(vertices[i]), ground_layerMask)*(mesh_height+add_height_offset));
			}
		} else {
			for(int i=0; i<vertices.Length; i++) {
				vertices[i]+=normals[i]*(mesh_height+add_height_offset);
			}
		}
		
		Vector3[] vertices_all=(Vector3[])ConcatenateArrays(vertices, vertices_bottom);
		int[] triangles_bottom=new int[3*2*side_vertices_count];
		for(int i=0,j=0; i<vertices2D_r.Length; i++) {
			if (side_walls[node_indices[i]]) {
				triangles_bottom[j++]=i;
				triangles_bottom[j++]=i+vertices.Length;
				triangles_bottom[j++]=((i+1)%vertices2D_r.Length) + vertices.Length;
				triangles_bottom[j++]=i;
				triangles_bottom[j++]=((i+1)%vertices2D_r.Length) + vertices.Length;
				triangles_bottom[j++]=(i+1)%vertices2D_r.Length;
			}
		}
		int[] triangles_all=(int[])ConcatenateArrays(triangles, triangles_bottom);
		
		Mesh msh_sidewalls=null;
		if (side_vertices_count>0) {
			msh_sidewalls=new Mesh();
			Vector3[] vertices_sidewalls=new Vector3[2*side_vertices_count];
			int[] triangles_sidewalls=new int[3*2*side_vertices_count];
			for(int i=0,j=0,k=0; i<vertices2D_r.Length; i++) {
				if (side_walls[node_indices[i]]) {
					vertices_sidewalls[j]=vertices[i]+normals[i]*0.02f;
					vertices_sidewalls[j+side_vertices_count]=vertices_bottom[i];
					triangles_sidewalls[k++]=j;
					triangles_sidewalls[k++]=((j+1)%side_vertices_count) + side_vertices_count;
					triangles_sidewalls[k++]=j+side_vertices_count;
					triangles_sidewalls[k++]=j;
					triangles_sidewalls[k++]=(j+1)%side_vertices_count;
					triangles_sidewalls[k++]=((j+1)%side_vertices_count) + side_vertices_count;
					j++;
				}
			}
			msh_sidewalls.vertices=vertices_sidewalls;
			msh_sidewalls.triangles=triangles_sidewalls;
			msh_sidewalls.RecalculateNormals();
		}
		
        msh.vertices = vertices_all;
        msh.triangles = triangles_all;
		Vector3[] normals_all=(Vector3[])ConcatenateArrays(normals, new Vector3[vertices2D_r.Length]);
		for(int i=0; i<vertices2D_r.Length; i++) {
			normals_all[i+vertices.Length]=new Vector3(normals[i].x, normals[i].y, normals[i].z);
		}
		msh.normals=normals_all;

		Vector2[] uvs=new Vector2[vertices_all.Length];
		Vector4[] tangents=new Vector4[vertices_all.Length];
		Color[] colors=new Color[vertices_all.Length];
		if (auto_border_transitions) {
			for(int i=0; i<vertices2D_r.Length; i++) {
				colors[i]=new Color(0,1,0,0);
			}
		}
		for(int i=vertices.Length; i<vertices_all.Length; i++) {
			colors[i]=new Color(1,1,0,0);
		}
		
		getUVBounds();
		if (snap_on_build) {
			iterateUVs(0, vertices_all, uvs, tangents, colors);
		} else {
			for(int i=0; i<vertices.Length; i++) {
				Vector3 vec=T_lw(vertices[i]);
				uvs[i]=new Vector2(vec.x*UV_ratio, vec.z*UV_ratio);
				vec=Vector3.Cross(msh.normals[i], Vector3.right);
				vec=Vector3.Cross(msh.normals[i], vec);
				tangents[i]=new Vector4(vec.x,vec.y,vec.z,-1);
				if (i<vertices2D_r.Length) {
					uvs[i+vertices.Length]=new Vector2(uvs[i].x, uvs[i].y);
					tangents[i+vertices.Length]=new Vector4(tangents[i].x, tangents[i].y, tangents[i].z, -1);
				}
			}
		}
		
		Vector2[] uvs2=new Vector2[vertices_all.Length];
		float minx_tmp, maxx_tmp, minz_tmp, maxz_tmp;
		minx_tmp=maxx_tmp=T_lw(vertices_all[0]).x;
		minz_tmp=maxz_tmp=T_lw(vertices_all[0]).z;
		for(int i=1; i<vertices_all.Length; i++) {
			Vector3 vec=T_lw(vertices_all[i]);
			if (vec.x<minx_tmp) minx_tmp=vec.x; else if (vec.x>maxx_tmp) maxx_tmp=vec.x;
			if (vec.z<minz_tmp) minz_tmp=vec.z; else if (vec.z>maxz_tmp) maxz_tmp=vec.z;
		}
		Vector4 tr=new Vector4(1.0f/(maxx_tmp-minx_tmp), 1.0f/(maxz_tmp-minz_tmp), -minx_tmp,-minz_tmp);
		for(int i=0; i<uvs2.Length; i++) {
			Vector3 vec=T_lw(vertices_all[i]);
			uvs2[i]=new Vector2( (vec.x+tr.z)*tr.x, (vec.z+tr.w)*tr.y );
			uvs2[i].x=uvs2[i].x*UV2range.x*0.99f+0.005f;
			uvs2[i].y=uvs2[i].y*UV2range.y*0.99f+0.005f;
		}
		
		msh.tangents=tangents;
		msh.uv = uvs;
		msh.uv2 = uvs2;
		msh.colors = colors;
       
		MeshFilter filter;
		if (gameObject.GetComponent(typeof(MeshRenderer))==null) {
	        gameObject.AddComponent(typeof(MeshRenderer));
		}
		if (gameObject.GetComponent(typeof(MeshFilter))==null) {
        	filter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
		} else {
			filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		}
		;
        filter.sharedMesh = msh;
		if (GetComponent<Renderer>().sharedMaterial!=null) {
			GetComponent<Renderer>().sharedMaterial.SetFloat("_tiling_factor", TilingFactors[act_lod]);
		} else {
			Material mat, default_mat;
			default_mat=(Material)(AssetDatabase.LoadAssetAtPath("Assets/VolumeGrass/_Shaders and Materials/Grass.mat", typeof(Material)));
			mat=new Material("");
			if (default_mat) {
				EditorUtility.CopySerialized(default_mat, mat);
				GetComponent<Renderer>().sharedMaterial=mat;
			} else {
				EditorUtility.DisplayDialog("Warning...", "Can't find default material at:\nAssets/VolumeGrass/_Shaders and Materials/Grass.mat   \n\nYou'll have to assing material properties\n(textures) by hand.", "Proceed", "");
				mat=new Material(Shader.Find("Grass Shader"));
				if (mat) {
					mat.name="Grass";
					GetComponent<Renderer>().sharedMaterial=mat;
				} else {
					EditorUtility.DisplayDialog("Warning...", "Can't find default material at:\nAssets/VolumeGrass/_Shaders and Materials/Grass.mat   \n\nYou'll have to assing material properties\n(textures) by hand.", "Proceed", "");
				}
			}
			if (_compatibility() && (!useOGL)) {
				GetComponent<Renderer>().sharedMaterial.shader=Shader.Find("Grass Shader"); // use d3d9/glsl version
			}			
			if (GetComponent<Renderer>().sharedMaterial) GetComponent<Renderer>().sharedMaterial.SetFloat("_tiling_factor", TilingFactors[act_lod]);
		}
		GetComponent<Renderer>().castShadows=false;
		
		if (LODs[act_lod]==null) {
			if ((act_lod>0) && (LOD_distances[0]==4000)) {
				LOD_distances[0]=100;
			}
		}
		if (LODs[act_lod]!=null) {
			DestroyImmediate(LODs[act_lod]);
		}
		LODs[act_lod]=msh;
		
		if (LODs_sidewalls[act_lod]!=null) {
			DestroyImmediate(LODs_sidewalls[act_lod]);
		}
		if (msh_sidewalls!=null) {
			LODs_sidewalls[act_lod]=msh_sidewalls;
		}
		
		Transform sidewalls=transform.Find("sidewalls");
		if (sidewalls==null) {
			GameObject go=new GameObject("sidewalls");
			MeshFilter mf;
			go.layer=GrassRenderingReservedLayer.layer_num;
			sidewalls=go.transform;
			sidewalls.position=transform.position;
			sidewalls.rotation=transform.rotation;
			sidewalls.localScale=transform.localScale;
			sidewalls.parent=transform;
	        go.AddComponent(typeof(MeshRenderer));
        	mf = go.AddComponent(typeof(MeshFilter)) as MeshFilter;
	        mf.sharedMesh = msh_sidewalls;
			go.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("GrassRenderDepth"));
			go.GetComponent<Renderer>().receiveShadows=false;
			go.GetComponent<Renderer>().castShadows=false;
		} else {
			GameObject go=sidewalls.gameObject;
			MeshFilter mf;
			go.layer=GrassRenderingReservedLayer.layer_num;
			if (go.GetComponent(typeof(MeshRenderer))==null) {
		        go.AddComponent(typeof(MeshRenderer));
			}
			if (go.GetComponent(typeof(MeshFilter))==null) {
		        go.AddComponent(typeof(MeshFilter));
			}
			mf = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
	        mf.sharedMesh = msh_sidewalls;
			if (go.GetComponent<Renderer>().sharedMaterial!=Shader.Find("GrassRenderDepth")) {
				go.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("GrassRenderDepth"));
			}
			go.GetComponent<Renderer>().receiveShadows=false;
			go.GetComponent<Renderer>().castShadows=false;
		}
		
		setupLODAndShader(act_lod, true);
		
		volume_vertices=filter.sharedMesh.vertices;
		volume_normals=filter.sharedMesh.normals;
		volume_colors=filter.sharedMesh.colors;
			
		return true;
	}
	public Vector3[] get_volume_vertices() {
		MeshFilter filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		if (filter!=null) {
			if ((volume_vertices==null) || (volume_vertices.Length==0)) {
				volume_vertices=filter.sharedMesh.vertices;
			}
		}
		return volume_vertices;
	}
	public Vector3[] get_volume_normals() {
		MeshFilter filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		if (filter!=null) {
			if ((volume_normals==null) || (volume_normals.Length==0)) {
				volume_normals=filter.sharedMesh.normals;
			}
		}
		return volume_normals;
	}
	public Color[] get_volume_colors() {
		MeshFilter filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		if (filter!=null) {
			if ((volume_colors==null) || (volume_colors.Length==0)) {
				volume_colors=filter.sharedMesh.colors;
			}
		}
		return volume_colors;
	}
	private bool Exists(List<int> lst, int val) {
		for (int i=0; i<lst.Count; i++) {
			if (lst[i]==val) return true;
		}
		return false;
	}
	private Vector3 getUVs(Vector2[,] grid, float[,] _z, Vector3 pos, float ox, float oz, float dx, float dz) {
		int ix,iz;
		float ddx,ddz;
		Vector3 uvs=new Vector3(0,0,0);
		ddx=pos.x-ox;
		ddz=pos.z-oz;
		ix=Mathf.FloorToInt(ddx/dx);
		iz=Mathf.FloorToInt(ddz/dz);
		if (ix<0) ix=0;
		if (iz<0) iz=0;
		if (ix>mapping_grid_size-2) ix=mapping_grid_size-2;
		if (iz>mapping_grid_size-2) iz=mapping_grid_size-2;
		ddx=(ddx-ix*dx)/dx;
		ddz=(ddz-iz*dz)/dz;
		uvs.x=(grid[ix,iz].x*(1-ddx) + grid[ix+1,iz].x*ddx)*(1-ddz) + (grid[ix,iz+1].x*(1-ddx) + grid[ix+1,iz+1].x*ddx)*ddz;
		uvs.y=(_z[ix,iz]*(1-ddx) + _z[ix+1,iz]*ddx)*(1-ddz) + (_z[ix,iz+1]*(1-ddx) + _z[ix+1,iz+1]*ddx)*ddz;
		uvs.z=(grid[ix,iz].y*(1-ddx) + grid[ix+1,iz].y*ddx)*(1-ddz) + (grid[ix,iz+1].y*(1-ddx) + grid[ix+1,iz+1].y*ddx)*ddz;
		return uvs;
	}
	public void getUVBounds() {
		for(int i=0; i<control_points.Count; i++) {
			Vector3 point_start=T_lw(control_points[i]);
			Vector3 point_end=T_lw(control_points[(i+1)%control_points.Count]);
			Vector3 tangent_start=T_lw(bezier_pointsA[i]);
			Vector3 tangent_end=T_lw(bezier_pointsB[(i+1)%control_points.Count]);
			if (Vector3.Distance(point_start, tangent_start)<0.001f) {
				tangent_start=(point_start*2.0f+point_end*1.0f)/3.0f;
			}
			if (Vector3.Distance(point_end, tangent_end)<0.001f) {
				tangent_end=(point_end*2.0f+point_start*1.0f)/3.0f;
			}
			if (snap_on_build) {
				SnapWorldPoint(ref point_start, ground_layerMask);
				SnapWorldPoint(ref point_end, ground_layerMask);
			}
			if (i==0) {
				minx=point_start.x;
				minz=point_start.z;
				maxx=point_start.x;
				maxz=point_start.z;
			} else {
				if (point_start.x<minx) {
					minx=point_start.x;
				} else if (point_start.x>maxx) {
					maxx=point_start.x;
				}
				if (point_start.z<minz) {
					minz=point_start.z;
				} else if (point_start.z>maxz) {
					maxz=point_start.z;
				}
			}
			for(int j=1; j<subdivisions[i]; j++) {
				float t=1.0f*j/(subdivisions[i]);
				Vector3 snap_pnt=get_Bezier_point(t, point_start, tangent_start, point_end, tangent_end);
				if (snap_on_build) {
					SnapWorldPoint(ref snap_pnt, ground_layerMask);
				}
				if (snap_pnt.x<minx) {
					minx=snap_pnt.x;
				} else if (snap_pnt.x>maxx) {
					maxx=snap_pnt.x;
				}
				if (snap_pnt.z<minz) {
					minz=snap_pnt.z;
				} else if (snap_pnt.z>maxz) {
					maxz=snap_pnt.z;
				}
			}
		}		
		maxx+=0.1f;
		maxz+=0.1f;
		reinitUV=true;
	}
	public void AdjustCustomUVBounds() {
		custom_minx=minx;
		custom_minz=minz;
		custom_maxx=maxx;
		custom_maxz=maxz;
	}
	public void iterateUVs(int mapping_iteration_count, Vector3[] vertices, Vector2[] uvs, Vector4[] tangents, Color[] colors) {
		
		float minx,minz,maxx,maxz;
		if (custom_UV_bounds) {
			minx=this.custom_minx;
			minz=this.custom_minz;
			maxx=this.custom_maxx;
			maxz=this.custom_maxz;
		} else {
			minx=this.minx;
			minz=this.minz;
			maxx=this.maxx;
			maxz=this.maxz;
		}
		float dx=(maxx-minx)/(mapping_grid_size-1);
		float dz=(maxz-minz)/(mapping_grid_size-1);
		if ((grid.GetLength(0)!=mapping_grid_size) || (reinitUV)) {
			reinitUV=false;
			grid=new Vector2[mapping_grid_size,mapping_grid_size];
			gridB=new Vector2[mapping_grid_size,mapping_grid_size];
			_z=new float[mapping_grid_size,mapping_grid_size];
			for(int i=0; i<mapping_grid_size; i++) {
				float _u=minx+i*dx;
				for(int j=0; j<mapping_grid_size; j++) {
					grid[i,j].x=_u;
					grid[i,j].y=minz+j*dz;
					Vector3 pos=new Vector3(grid[i,j].x,0,grid[i,j].y);
					SnapWorldPoint(ref pos, ground_layerMask);
					_z[i,j]=pos.y;
				}
			}
		}
		float dt=0.5f;
		int t=0;
		float dx2=dx*dx;
		float dy2;
		float dz2=dz*dz;
		float diag_sqrtX=dx/Mathf.Sqrt(dx2+dz2);
		float diag_sqrtZ=dz/Mathf.Sqrt(dx2+dz2);
		for(t=0; t<mapping_iteration_count; t++) {
			for(int i=0; i<mapping_grid_size; i++) {
				for(int j=0; j<mapping_grid_size; j++) {
					Vector2 target_uv=new Vector2(0,0);
					float d=0;
					if (i>0) {
						dy2=_z[i-1,j] - _z[i,j];
						dy2*=dy2;
						target_uv.x+=grid[i-1,j].x+Mathf.Sqrt(dx2+dy2);
						d+=1;
						if (j>0) {
							dy2=_z[i-1,j-1] - _z[i,j-1];
							dy2*=dy2;
							target_uv.x+=diag_sqrtX*(grid[i-1,j-1].x+Mathf.Sqrt(dx2+dy2));
							d+=diag_sqrtX;
						}
						if (j<mapping_grid_size-1) {
							dy2=_z[i-1,j+1] - _z[i,j+1];
							dy2*=dy2;
							target_uv.x+=diag_sqrtX*(grid[i-1,j+1].x+Mathf.Sqrt(dx2+dy2));
							d+=diag_sqrtX;
						}
					}
					if (i<mapping_grid_size-1) {
						dy2=_z[i+1,j] - _z[i,j];
						dy2*=dy2;
						target_uv.x+=grid[i+1,j].x-Mathf.Sqrt(dx2+dy2);
						d+=1;
						if (j>0) {
							dy2=_z[i+1,j-1] - _z[i,j-1];
							dy2*=dy2;
							target_uv.x+=diag_sqrtX*(grid[i+1,j-1].x-Mathf.Sqrt(dx2+dy2));
							d+=diag_sqrtX;
						}
						if (j<mapping_grid_size-1) {
							dy2=_z[i+1,j+1] - _z[i,j+1];
							dy2*=dy2;
							target_uv.x+=diag_sqrtX*(grid[i+1,j+1].x-Mathf.Sqrt(dx2+dy2));
							d+=diag_sqrtX;
						}						}
					if (d>0) target_uv.x/=d; else target_uv.x=grid[i,j].x;
					d=0;
					if (j>0) {
						dy2=_z[i,j-1] - _z[i,j];
						dy2*=dy2;
						target_uv.y+=grid[i,j-1].y+Mathf.Sqrt(dz2+dy2);
						d+=1;
						if (i>0) {
							dy2=_z[i-1,j-1] - _z[i-1,j];
							dy2*=dy2;
							target_uv.y+=diag_sqrtZ*(grid[i-1,j-1].y+Mathf.Sqrt(dz2+dy2));
							d+=diag_sqrtZ;
						}
						if (i<mapping_grid_size-1) {
							dy2=_z[i+1,j-1] - _z[i+1,j];
							dy2*=dy2;
							target_uv.y+=diag_sqrtZ*(grid[i+1,j-1].y+Mathf.Sqrt(dz2+dy2));
							d+=diag_sqrtZ;
						}						}
					if (j<mapping_grid_size-1) {
						dy2=_z[i,j+1] - _z[i,j];
						dy2*=dy2;
						target_uv.y+=grid[i,j+1].y-Mathf.Sqrt(dz2+dy2);
						d+=1;
						if (i>0) {
							dy2=_z[i-1,j+1] - _z[i-1,j];
							dy2*=dy2;
							target_uv.y+=diag_sqrtZ*(grid[i-1,j+1].y-Mathf.Sqrt(dz2+dy2));
							d+=diag_sqrtZ;
						}
						if (i<mapping_grid_size-1) {
							dy2=_z[i+1,j+1] - _z[i+1,j];
							dy2*=dy2;
							target_uv.y+=diag_sqrtZ*(grid[i+1,j+1].y-Mathf.Sqrt(dz2+dy2));
							d+=diag_sqrtZ;
						}						}
					if (d>0) target_uv.y/=d; else target_uv.y=grid[i,j].y;						

					gridB[i,j].x=grid[i,j].x + (target_uv.x-grid[i,j].x) * dt;
					gridB[i,j].y=grid[i,j].y + (target_uv.y-grid[i,j].y) * dt;
				}
			}
			Vector2[,] tmp_grid;
			tmp_grid=grid;
			grid=gridB;
			gridB=tmp_grid;
		}
		
		int first_bottom_idx=-1;
		Vector3 vec;
		Vector3 vec2;
		for(int i=0; i<vertices.Length; i++) {
			if (colors[i].r>0) {
				if (first_bottom_idx==-1) {
					first_bottom_idx=i;
				}
				uvs[i]=new Vector2(uvs[i-first_bottom_idx].x, uvs[i-first_bottom_idx].y);
				tangents[i]=new Vector4(tangents[i-first_bottom_idx].x, tangents[i-first_bottom_idx].y, tangents[i-first_bottom_idx].z, tangents[i-first_bottom_idx].w);
			} else {
				vec=getUVs(grid, _z, T_lw(vertices[i]), minx, minz, dx, dz);
				uvs[i].x=vec.x*UV_ratio;
				uvs[i].y=vec.z*UV_ratio;
				vec=T_wl(getUVs(grid, _z, T_lw(vertices[i]), minx, minz, dx, dz));
				vec2=T_wl(getUVs(grid, _z, T_lw(vertices[i])+new Vector3(0.05f,0,0), minx, minz, dx, dz));
				tangents[i]=new Vector4(vec2.x - vec.x, vec2.y - vec.y, vec2.z - vec.z, 0);
				tangents[i].Normalize();
				tangents[i].w=-1;
			}
		}
	}
	private bool check_subdivide(int iA, int iB, Vector2[] vertices2D_r, float[] vertices_depth_r, List<Vector3> add_points, ref Vector3 center) {
		if (iA>iB) return false;
		Vector3 pntA, pntB;
		if (!((iA<vertices2D_r.Length) && (iB<vertices2D_r.Length) && ( (((iA+1)%vertices2D_r.Length) == iB) || (((iB+1)%vertices2D_r.Length) == iA) ))) {
			if (iA<vertices2D_r.Length) {
				pntA=new Vector3(vertices2D_r[iA].x, vertices_depth_r[iA], vertices2D_r[iA].y);
			} else {
				pntA=add_points[iA - vertices2D_r.Length];
			}
			if (iB<vertices2D_r.Length) {
				pntB=new Vector3(vertices2D_r[iB].x, vertices_depth_r[iB], vertices2D_r[iB].y);
			} else {
				pntB=add_points[iB - vertices2D_r.Length];
			}
			center=0.5f*(pntA+pntB);
			pntA.y=pntB.y=0;
			if (Vector3.Distance(pntA, pntB)>min_edge_length[act_lod]*2) {
				Vector3 tmp_pnt=new Vector3(center.x, center.y, center.z);
				SnapWorldPoint(ref center, ground_layerMask);
				if (Mathf.Abs(center.y-tmp_pnt.y) > max_y_error[act_lod]) {
					for(int k=0; k<vertices2D_r.Length; k++) {
						Vector3 pntA2D=new Vector3(vertices2D_r[k].x, vertices2D_r[k].y, 0);
						Vector3 pntB2D=new Vector3(vertices2D_r[(k+1)%vertices2D_r.Length].x, vertices2D_r[(k+1)%vertices2D_r.Length].y, 0);
						Vector3 proj=HandleUtility.ProjectPointLine(new Vector3(center.x, center.z, 0), pntA2D, pntB2D);
						float min_dist=Vector2.Distance(pntA2D, pntB2D)*0.1f;
						if (Vector2.Distance(new Vector2(center.x, center.z), new Vector2(proj.x, proj.y))<min_dist) {
							return false;
						}
					}
					for(int k=0; k<add_points.Count; k++) {
						if (Vector3.Distance(center, add_points[k])<min_edge_length[act_lod]) {
							return false;
						}
					}
					return true;
				}
			}
		}
		return false;
	}
	static Array ConcatenateArrays(params Array[] arrays) {
		if (arrays==null) {
			throw new ArgumentNullException("arrays");
		}
		if (arrays.Length==0) {
			throw new ArgumentException("No arrays specified");
		}
		
		Type type = arrays[0].GetType().GetElementType();
		int totalLength = arrays[0].Length;
		for (int i=1; i < arrays.Length; i++) {
			if (arrays[i].GetType().GetElementType() != type) {
				throw new ArgumentException("Arrays must all be of the same type");
			}
			totalLength += arrays[i].Length;
		}
		
		Array ret = Array.CreateInstance(type, totalLength);
		int index=0;
		foreach (Array array in arrays) {
			Array.Copy (array, 0, ret, index, array.Length);
			index += array.Length;
		}
		return ret;
	}	
	
	public void showChecker() {
		myMat=GetComponent<Renderer>().sharedMaterial;
		if (!checkerMat) {
			checkerMat=new Material(Shader.Find("Diffuse"));
			Texture2D tex=new Texture2D(2,2);
			tex.SetPixel(0,0,Color.green);
			tex.SetPixel(1,1,Color.green);
			tex.SetPixel(0,1,Color.yellow);
			tex.SetPixel(1,0,Color.yellow);
			tex.Apply();
			tex.anisoLevel=0;
			tex.wrapMode=TextureWrapMode.Repeat;
			tex.filterMode=FilterMode.Point;
			checkerMat.mainTexture=tex;
		}
		GetComponent<Renderer>().sharedMaterial=checkerMat;
	}
	public void hideChecker() {
		GetComponent<Renderer>().sharedMaterial=myMat;
	}
	public void modify_height(int cover_verts_num, int[] cover_indices, float[] cover_strength, bool upflag) {
		MeshFilter filter=GetComponent(typeof(MeshFilter)) as MeshFilter;
		if (filter && filter.sharedMesh) {
			Color[] colors=filter.sharedMesh.colors;
			float val;
			if (upflag) {
				for(int i=0; i<cover_verts_num; i++) {
					val=colors[cover_indices[i]].g-cover_strength[i]*paint_opacity*0.25f;
					val=(val<0) ? 0 : val;
					colors[cover_indices[i]].g=val;
				}
			} else {
				for(int i=0; i<cover_verts_num; i++) {
					val=colors[cover_indices[i]].g+cover_strength[i]*paint_opacity*0.25f;
					val=(val>1) ? 1 : val;
					colors[cover_indices[i]].g=val;
				}
			}
			filter.sharedMesh.colors=colors;
		}
	}
	
#endif	
}
