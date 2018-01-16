using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(VolumeGrass))]
public class VolumeGrassEditor : Editor {
#if UNITY_EDITOR	
	private Vector3[] cover_verts=new Vector3[500];
	//private Quaternion[] cover_norms=new Quaternion[500];
	//private Quaternion[] cover_norms_flip=new Quaternion[500];
	private	float[] cover_strength=new float[500];
	private	int[] cover_indices=new int[500];
	private	int cover_verts_num=0;
	private int cover_verts_num_start_drag=0;
	private float lCovTim=0;
	private bool control_down_flag=false;
	
	public override void OnInspectorGUI () {
		VolumeGrass _target=(VolumeGrass)target;
		
		GUILayout.Space(10);
		GUILayout.Label ("Mode", EditorStyles.boldLabel);
		int _state = GUILayout.Toolbar(_target.state, _target.stateStrings);
		MeshFilter filter = _target.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		if ((_target.state==0) && (_state==1)) {
			// build mesh
			if (!_target.BuildMesh()) {
				EditorUtility.DisplayDialog("Error...", "Can't build mesh   ", "Proceed", "");
				return; // nie można zbudowac mesha (np. za mało wierzchołków)
			}
		} else if ((_target.state==1) && (_state==0)) {
			if (filter && filter.sharedMesh) {
				filter.sharedMesh=null;
				MeshFilter filter_sidewalls = null;
				Transform tr=_target.transform.Find("sidewalls");
				if (tr!=null) filter_sidewalls=tr.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
				if (filter_sidewalls && filter_sidewalls.sharedMesh) filter_sidewalls.sharedMesh=null;
			}
			_target.state=_state;
			EditorUtility.SetDirty(_target);
			return;
		}
		_target.state=_state;
		
		if (_target.state==1) {
			GUILayout.Space(15);
			GUILayout.Label ("Grass height/coverage modification", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
				bool prev_paint_height=_target.paint_height;
				GUILayout.Label ("Modify volume height on vertices", EditorStyles.label );
				_target.paint_height = EditorGUILayout.Toggle (_target.paint_height);
				if (prev_paint_height!=_target.paint_height) {
					EditorUtility.SetDirty(_target);
				}
			GUILayout.EndHorizontal();
			if (_target.paint_height) {
				GUILayout.BeginHorizontal();
					GUILayout.Label ("Area size", EditorStyles.label );
					_target.paint_size = EditorGUILayout.Slider(_target.paint_size, 0.5f, 50);
				GUILayout.EndHorizontal();				
				GUILayout.BeginHorizontal();
					GUILayout.Label ("Area smoothness", EditorStyles.label );
					_target.paint_smoothness = EditorGUILayout.Slider (_target.paint_smoothness, 0, 1);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
					GUILayout.Label ("Opacity", EditorStyles.label );
					_target.paint_opacity = EditorGUILayout.Slider (_target.paint_opacity, 0, 1);
				GUILayout.EndHorizontal();				
			}
		}
		
		GUILayout.Space(15);
		
		GUILayout.Label ("General Settings", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Show node numbers", EditorStyles.label );
			bool tmp_showNodeNumbers = EditorGUILayout.Toggle (_target.showNodeNumbers);
			if (_target.showNodeNumbers!=tmp_showNodeNumbers) {
				_target.showNodeNumbers=tmp_showNodeNumbers;
				EditorUtility.SetDirty(_target);
			}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label("Show tesselation points in build mode", EditorStyles.label);
			bool new_flag=EditorGUILayout.Toggle(_target.show_tesselation_points);
			if (_target.show_tesselation_points!=new_flag) {
				_target.show_tesselation_points=new_flag;
				if ((!new_flag) && (_target.which_active==3)) {
					_target.active_idx=-1;
				}
				EditorUtility.SetDirty(_target);
			}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Default subnodes", EditorStyles.label );
			_target.bezier_subdivisions = EditorGUILayout.IntSlider (_target.bezier_subdivisions, 1, 50);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Snap to ground when dragging node", EditorStyles.label );
			_target.snap_on_move = EditorGUILayout.Toggle (_target.snap_on_move);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Snap to ground every node at once", EditorStyles.label );
			_target.snap_always = EditorGUILayout.Toggle (_target.snap_always);
		GUILayout.EndHorizontal();
		GUILayout.Space(2);
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Ground collision layer mask", EditorStyles.label );
			_target.ground_layerMask.value=EditorGUILayout.LayerField(_target.ground_layerMask.value);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Disable(hide) on incompatible hardware", EditorStyles.label );
			_target.hide_on_old_hardware = EditorGUILayout.Toggle (_target.hide_on_old_hardware);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Force ALT (slower) shader in editor", EditorStyles.label );
			bool prev_useOGL = _target.useOGL;
			_target.useOGL = EditorGUILayout.Toggle (_target.useOGL);
			if (_target.useOGL!=prev_useOGL) {
				_target.checkOGL();
			}
		GUILayout.EndHorizontal();

		GUILayout.Space(15);
		GUILayout.Label("Build Settings", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Slices on blades texture", EditorStyles.label );
			float prev_slices_num=_target.slices_num;
			_target.slices_num=EditorGUILayout.IntSlider(_target.slices_num, 1, 64);
			if (_target.slices_num!=prev_slices_num) {
				_target.setupLODAndShader(_target.act_lod, true);
			}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Slice planes per world unit", EditorStyles.label );
			float UV_ratio=1.0f/(_target.mesh_height*_target.slices_num);
			float prev_plane_num=_target.plane_num;
			_target.plane_num=EditorGUILayout.Slider( _target.plane_num*UV_ratio, 0.3f, 80f)/UV_ratio;
			if (_target.plane_num!=prev_plane_num) {
				_target.setupLODAndShader(_target.act_lod, true);
			}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Grass height in world units", EditorStyles.label );
			_target.mesh_height=EditorGUILayout.Slider(_target.mesh_height, 0.02f, 3f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Additional mesh height offset", EditorStyles.label );
			_target.add_height_offset=EditorGUILayout.Slider (_target.add_height_offset, -2f, 2f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label("Subnode optimization threshold angle", EditorStyles.label);
			_target.colinear_treshold = EditorGUILayout.Slider (_target.colinear_treshold, 91f, 180f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Adjust to ground", EditorStyles.label );
			_target.snap_on_build = EditorGUILayout.Toggle (_target.snap_on_build);
		GUILayout.EndHorizontal();
		if (_target.snap_on_build) {
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Max height error", EditorStyles.label);
				_target.max_y_error[_target.act_lod] = EditorGUILayout.Slider ( _target.max_y_error[_target.act_lod], 0.01f, 1);
			GUILayout.EndHorizontal();
			_target.min_edge_length[_target.act_lod] = EditorGUILayout.Slider ("Min edge length", _target.min_edge_length[_target.act_lod], 0.01f, 5);
			GUILayout.BeginHorizontal();
				GUILayout.Label ("UV map grid resolution", EditorStyles.label);
				_target.mapping_grid_size = EditorGUILayout.IntSlider ( _target.mapping_grid_size, 10, 300);
			GUILayout.EndHorizontal();
			bool prev_custom_UV_bounds=_target.custom_UV_bounds;
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Custom UV map grid bounds", EditorStyles.label);
				_target.custom_UV_bounds = EditorGUILayout.Toggle ( _target.custom_UV_bounds);
			GUILayout.EndHorizontal();
			if (prev_custom_UV_bounds!=_target.custom_UV_bounds) {
				if (_target.custom_UV_bounds) {
					_target.getUVBounds();
					_target.AdjustCustomUVBounds();
				}
				EditorUtility.SetDirty(_target);
			}
			if (_target.custom_UV_bounds) {
				Vector4 old_bounds = new Vector4(_target.custom_minx, _target.custom_maxx, _target.custom_minz, _target.custom_maxz);
				Vector4 new_bounds = EditorGUILayout.Vector4Field("UV bounds rect (X=xMin, Y=xMax, Z=zMin, W=zMax)", old_bounds);
				if (Vector4.Distance(old_bounds, new_bounds)>0) {
					if (new_bounds.x>_target.minx) new_bounds.x=_target.minx;
					if (new_bounds.y<_target.maxx) new_bounds.y=_target.maxx;
					if (new_bounds.z>_target.minz) new_bounds.z=_target.minz;
					if (new_bounds.w<_target.maxz) new_bounds.w=_target.maxz;
					_target.custom_minx=new_bounds.x;
					_target.custom_maxx=new_bounds.y;
					_target.custom_minz=new_bounds.z;
					_target.custom_maxz=new_bounds.w;
					_target.reinitUV=true;
					EditorUtility.SetDirty(_target);
				}
			}
			GUILayout.BeginHorizontal();
				GUILayout.Label ("UV2 range (lightmap 0..u, 0..v)", EditorStyles.label);
				_target.UV2range.x = EditorGUILayout.Slider (_target.UV2range.x, 0.1f, 1f);
				_target.UV2range.y = EditorGUILayout.Slider (_target.UV2range.y, 0.1f, 1f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Border transitions on build", EditorStyles.label);
				_target.auto_border_transitions = EditorGUILayout.Toggle ( _target.auto_border_transitions);
			GUILayout.EndHorizontal();
			bool prev_fullBackGeometry=_target.fullBackGeometry;
			GUILayout.BeginHorizontal();
				GUILayout.Label ("Silhouettes backcut", EditorStyles.label);
				_target.fullBackGeometry = EditorGUILayout.Toggle ( _target.fullBackGeometry);
			GUILayout.EndHorizontal();
			if (!prev_fullBackGeometry && _target.fullBackGeometry) {
				EditorUtility.DisplayDialog("Info", "This feature can improve grass silhouette on _very_ wavy terrain, but requires full grass geometry (instead of sidewalls only) to be rendered into depth buffer. Takes effect on play.", "Proceed", "");
			}
			
		}
		GUILayout.Space(12);
		if ((filter==null) || (filter.sharedMesh==null)) {
			GUILayout.Label ("  Current LOD mesh ", EditorStyles.miniLabel);
		} else {
			GUILayout.Label ("  Current LOD mesh ("+filter.sharedMesh.vertices.Length+" verts, "+(filter.sharedMesh.triangles.Length/3)+" tris)", EditorStyles.miniLabel);
		}
		if (_target.state==1) {
			GUILayout.BeginHorizontal();
				GUILayout.Label("  Used when distance < than ", EditorStyles.miniLabel);
				_target.LOD_distances[_target.act_lod]=EditorGUILayout.IntField(_target.LOD_distances[_target.act_lod]);
			GUILayout.EndHorizontal();
		}
		for(int i=0; i<4; i++) {
			if (_target.LODs[i]==null) {
				_target.lodStrings[i]=_target.lodStrings_empty[i];
			} else {
				_target.lodStrings[i]=_target.lodStrings_occupied[i];
			}
		}
		int act_lod = GUILayout.Toolbar(_target.act_lod, _target.lodStrings);
		if (_target.act_lod!=act_lod) {
			if (_target.state==1) {
				_target.setupLODAndShader(act_lod, true);
			}
			_target.act_lod=act_lod;
		}
		if (_target.state==1) {
			GUILayout.BeginHorizontal();
				string lb;
				if (_target.LODs[_target.act_lod]!=null) {
					lb="Rebuild LOD";
				} else {
					lb="Build LOD";
				}
				if (GUILayout.Button(lb, GUILayout.Width(85), GUILayout.Height(20))) {
					if (!_target.BuildMesh()) {
						EditorUtility.DisplayDialog("Error...", "Can't build mesh   ", "Proceed", "");
					}
				}
				if (_target.LODs[_target.act_lod]!=null) {
					if (GUILayout.Button("Delete LOD", GUILayout.Width(85), GUILayout.Height(20))) {
						if ((filter!=null) && (filter.sharedMesh!=null)) {
							filter.sharedMesh=null;
							DestroyImmediate(_target.LODs[_target.act_lod]);
							_target.LODs[_target.act_lod]=null;
							if (_target.LODs_sidewalls[_target.act_lod]!=null) {
								Transform tr=_target.transform.Find("sidewalls");
								if (tr!=null) {
									tr.parent=null;
									DestroyImmediate(tr);
								}
								DestroyImmediate(_target.LODs_sidewalls[_target.act_lod]);
								_target.LODs_sidewalls[_target.act_lod]=null;
							}
						}
					}
				}
				if (_target.snap_on_build && (_target.LODs[_target.act_lod]!=null)) {
					if (GUILayout.Button("Refine UV at slopes", GUILayout.Height(20))) {
						if ((filter!=null) && (filter.sharedMesh!=null)) {
							Mesh msh=filter.sharedMesh;
							Vector3[] vertices=msh.vertices;
							Vector2[] uvs=msh.uv;
							Vector4[] tangents=msh.tangents;
							Color[] colors=msh.colors;
							_target.iterateUVs(_target.mapping_grid_size>>2, vertices, uvs, tangents, colors);
							msh.uv=uvs;
							msh.tangents=tangents;
						}
					}
				}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				bool prev_checkerMatFlag=_target.checkerMatFlag;
				GUILayout.Label("Show units checker (useful when refining)", EditorStyles.label);
				_target.checkerMatFlag=EditorGUILayout.Toggle(_target.checkerMatFlag);
				if (_target.checkerMatFlag && (!prev_checkerMatFlag)) {
					_target.showChecker();
				} else if ((!_target.checkerMatFlag) && prev_checkerMatFlag) {
					_target.hideChecker();
				}
			GUILayout.EndHorizontal();
		}
		if (_target.active_idx>=0) {
			GUILayout.Space(15);
			if (_target.which_active==3) {
				// tesselation points
				GUILayout.Label ("Tesselation Point Properties", EditorStyles.boldLabel);
				Vector3 vec;
				if (_target.localGlobalState==0) {
					// local
					vec = EditorGUILayout.Vector3Field("Position", _target.tesselation_points[_target.active_idx]);
				} else {
					// global
					vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.tesselation_points[_target.active_idx])));
				}
				if (_target.ConstrainPoints(vec)) EditorUtility.SetDirty(_target);
				GUILayout.BeginHorizontal();
					_target.localGlobalState = GUILayout.Toolbar(_target.localGlobalState, _target.localGlobalStrings);
					if (GUILayout.Button("Delete Point", GUILayout.Width(100), GUILayout.Height(20))) {
						Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
						_target.DeleteActiveControlPoint();
						EditorUtility.SetDirty(_target);
					}
				GUILayout.EndHorizontal();
			} else {
				// nodes
				GUILayout.Label ("Node Properties", EditorStyles.boldLabel);
				if (_target.state==0) {
					Vector3 vec;
					if (_target.localGlobalState==0) {
						// local
						if (_target.which_active==0) {
							vec = EditorGUILayout.Vector3Field("Position", _target.control_points[_target.active_idx]);
						} else if (_target.which_active==1) {
							vec = EditorGUILayout.Vector3Field("Position", _target.bezier_pointsA[_target.active_idx]);
						} else {
							vec = EditorGUILayout.Vector3Field("Position", _target.bezier_pointsB[_target.active_idx]);
						}
					} else {
						// global
						if (_target.which_active==0) {
							vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.control_points[_target.active_idx])));
						} else if (_target.which_active==1) {
							vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.bezier_pointsA[_target.active_idx])));
						} else {
							vec = _target.T_wl(EditorGUILayout.Vector3Field("Position", _target.T_lw(_target.bezier_pointsB[_target.active_idx])));
						}
					}
					if (_target.ConstrainPoints(vec)) EditorUtility.SetDirty(_target);
					GUILayout.BeginHorizontal();
						_target.localGlobalState = GUILayout.Toolbar(_target.localGlobalState, _target.localGlobalStrings);
						if (GUILayout.Button("Delete Node", GUILayout.Width(100), GUILayout.Height(20))) {
							Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
							_target.DeleteActiveControlPoint();
							EditorUtility.SetDirty(_target);
						}
					GUILayout.EndHorizontal();
				}
				int subs = EditorGUILayout.IntSlider ("Subnodes count", (int)_target.subdivisions[_target.active_idx], 1, 50);
				if (subs!=_target.subdivisions[_target.active_idx]) {
					_target.subdivisions[_target.active_idx]=subs;
					EditorUtility.SetDirty(_target);
				}
				GUILayout.BeginHorizontal();
					GUILayout.Label ("Optimize collinear subnodes", EditorStyles.label );
					_target.optimize_subnodes[_target.active_idx] = EditorGUILayout.Toggle(_target.optimize_subnodes[_target.active_idx]);
				GUILayout.EndHorizontal();					
				GUILayout.BeginHorizontal();
					GUILayout.Label ("Build sidewall", EditorStyles.label );
					_target.side_walls[_target.active_idx] = EditorGUILayout.Toggle(_target.side_walls[_target.active_idx]);
				GUILayout.EndHorizontal();					
			}
		}

		GUILayout.Space(15);
	}
	
	public void OnSceneGUI() {
		VolumeGrass _target=(VolumeGrass)target;
		
		int i;
		EditorWindow currentWindow = EditorWindow.mouseOverWindow;
		if(currentWindow) {
			//Rect winRect = currentWindow.position;
			Event current = Event.current;
			
			switch(current.type) {
				case EventType.keyDown:
					if (current.keyCode==KeyCode.M) {
						_target.paint_height=!_target.paint_height;
						EditorUtility.SetDirty(_target);
					}
				break;
			}
			
			if (_target.state==1) {
			if (_target.paint_height) {
				
				if (current.alt) return;
				if (current.control) {
						if (current.type==EventType.mouseMove) {
							if (control_down_flag) {
								control_down_flag=false;
								EditorUtility.SetDirty(_target);
							}
						}
						return;
				}
				control_down_flag=true;
				switch(current.type) {
					case EventType.mouseDown:
						get_paint_coverage();
						cover_verts_num_start_drag=cover_verts_num;
						if (cover_verts_num>0) {
							Undo.RegisterSceneUndo("grass edit (height)");
							_target.modify_height(cover_verts_num, cover_indices, cover_strength, current.shift);
							current.Use();
						} else {
							_target.undo_flag=true;
						}
					break;
					case EventType.mouseDrag:
						get_paint_coverage();
						if (cover_verts_num>0) {
							if (_target.undo_flag) {
								Undo.RegisterSceneUndo("grass edit (height)");
								_target.undo_flag=false;
							}
						}
						if (cover_verts_num_start_drag>0) {
							_target.modify_height(cover_verts_num, cover_indices, cover_strength, current.shift);
							current.Use();
						}
					break;
					case EventType.mouseMove:
						get_paint_coverage();
					break;
				}
		
				if (current.shift) {
					for(i=0; i<cover_verts_num; i++) {
						Handles.color=new Color(0,1,0,cover_strength[i]);
						//Handles.ArrowCap(0, cover_verts[i], cover_norms[i], 5*cover_strength[i]);
						Handles.DrawSolidDisc(cover_verts[i], Camera.current.transform.position-cover_verts[i], HandleUtility.GetHandleSize(cover_verts[i])*0.03f);
					}
				} else {
					Handles.color=Color.red;
					for(i=0; i<cover_verts_num; i++) {
						Handles.color=new Color(1,0,0,cover_strength[i]);
						//Handles.ArrowCap(0, cover_verts[i], cover_norms_flip[i], 5*cover_strength[i]);
						Handles.DrawSolidDisc(cover_verts[i], Camera.current.transform.position-cover_verts[i], HandleUtility.GetHandleSize(cover_verts[i])*0.03f);
					}
				}
					
				return;
			}
			}
			
			switch(current.type) {
				case EventType.keyDown:
					if (current.keyCode==KeyCode.Delete) {
						if ((_target.state==0) || (_target.which_active==3)) {
							if (_target.active_idx>=0) {
								Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
								_target.DeleteActiveControlPoint();
								current.Use();
							}
						} else {
							if (_target.active_idx>=0) {
								current.Use();
								if (_target.which_active==0) {
									EditorUtility.DisplayDialog("Info", "Deleting nodes in Edit mode only   ", "Proceed", "");
								}
							}
						}
					} else if (current.keyCode==KeyCode.Return) {
						if (_target.state==1) {
							// rebuild
							current.Use();
							if (!_target.BuildMesh()) {
								EditorUtility.DisplayDialog("Error...", "Can't build mesh   ", "Proceed", "");
							}
						}
					}
					break;
				case EventType.keyUp:
//					current.Use();
					break;
				case EventType.mouseDown:
					//Debug.Log(current +"     "+controlID);
					float dist;
					float min_dist;
				
					_target.active_idx=-1;
					_target.which_active=0;
					// pressing control points
					min_dist=12f; // promień kliku w obrębie którego "łapiemy" pkt kontrolny
					for(i=0; i<_target.control_points.Count; i++) {
						dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[i])));
						if (dist<min_dist) {
							min_dist=dist;
							_target.active_idx=i;
						}
					}
					if (_target.state==0) { // zaznaczanie pktów beziera tylko w trybie edycji (w rybie build i tak są niewidoczne)
						// pressing bezierA points
						min_dist=5f; // kółko bezier handla jest mniejsze niż control point
						for(i=0; i<_target.control_points.Count; i++) {
							// pkt beziera traktujemy jako "aktywny" jeśli jest choć trochę oddalony od control_pointa
							if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[i])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsA[i])))>0.01f) {
								dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsA[i])));
								if (dist<min_dist) {
									min_dist=dist;
									_target.which_active=1;
									_target.active_idx=i;
								}
							}
						}
						// pressing bezierB points
						min_dist=5f; // kółko bezier handla jest mniejsze niż control point
						for(i=0; i<_target.control_points.Count; i++) {
							// pkt beziera traktujemy jako "aktywny" jeśli jest choć trochę oddalony od control_pointa
							if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[i])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsB[i])))>0.01f) {
								dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsB[i])));
								if (dist<min_dist) {
									min_dist=dist;
									_target.which_active=2;
									_target.active_idx=i;
								}
							}
						}
					}
					// pressing tesselation points
					if ((_target.state==0) || (_target.show_tesselation_points)) {
						min_dist=8f; // kółko tesselation handla jest mniejsze niż control point
						for(i=0; i<_target.tesselation_points.Count; i++) {
							dist=Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(T_lw(_target.tesselation_points[i])));
							if (dist<min_dist) {
								min_dist=dist;
								_target.which_active=3;
								_target.active_idx=i;
							}
						}
					}
					if ((_target.state==0) && current.shift && (!current.alt) && (_target.active_idx==-1) && (_target.which_active==0)) {
						// dodawanie pktów kontrolnych
						Vector3 insert_pos=new Vector3(0,0,0);
						int insert_idx=-1;
						_target.GetInsertPos(current.mousePosition, ref insert_pos, ref insert_idx);
						if (insert_idx<0) {
							// add point
							Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
							_target.AddControlPoint(GetWorldPointFromMouse(_target.ground_layerMask), -1);
						} else {
							// insert point
							Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
							_target.AddControlPoint(insert_pos, insert_idx);
						}
						current.Use();
					} else if (current.shift && current.alt && (_target.active_idx==-1) && (_target.which_active==0) && ((_target.state==0) || (_target.show_tesselation_points))) {
						// dodawanie pktów tesselacji
						Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
						_target.AddTesselationPoint(GetWorldPointFromMouse(_target.ground_layerMask));
						current.Use();
					} else if ((_target.state==0) && (_target.active_idx>=0) && (_target.which_active==0) && (current.alt)) {
						// dodawanie pktów beziera
						if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[_target.active_idx])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsA[_target.active_idx])))<0.01f) {
							// wstawienie bezier_pointA
							//_target.which_active=1;
							Vector3 dir_vec=_target.control_points[(_target.active_idx+1)%_target.control_points.Count] - _target.control_points[_target.active_idx];
							dir_vec+=1.5f*(_target.control_points[(_target.active_idx+_target.control_points.Count-1)%_target.control_points.Count] - _target.control_points[_target.active_idx]);
							if (dir_vec.magnitude<0.01f) {
								dir_vec=Vector3.right;
							} else if (dir_vec.magnitude>5) {
								dir_vec.Normalize();
								dir_vec*=5;
							}
							_target.bezier_pointsA[_target.active_idx]-=dir_vec;
						} else if (Vector2.Distance(HandleUtility.WorldToGUIPoint(T_lw(_target.control_points[_target.active_idx])), HandleUtility.WorldToGUIPoint(T_lw(_target.bezier_pointsB[_target.active_idx])))<0.01f) {
							// wstawienie bezier_pointB
							//_target.which_active=2;
							Vector3 dir_vec=_target.control_points[(_target.active_idx+_target.control_points.Count-1)%_target.control_points.Count] - _target.control_points[_target.active_idx];
							dir_vec+=1.5f*(_target.control_points[(_target.active_idx+1)%_target.control_points.Count] - _target.control_points[_target.active_idx]);
							if (dir_vec.magnitude<0.01f) {
								dir_vec=Vector3.right;
							} else if (dir_vec.magnitude>5) {
								dir_vec.Normalize();
								dir_vec*=5;
							}
							_target.bezier_pointsB[_target.active_idx]-=dir_vec;
						}
						//current.Use();
					}
//					if ((prev_target_active_idx!=_target.active_idx) || (prev_target_which_active!=_target.which_active)) {
//						Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
//					}
					_target.undo_flag=false;
					//current.Use();
				break;
//				case EventType.mouseMove:
//				break;
				case EventType.mouseDrag:
					//current.Use();
				break;
				case EventType.mouseUp:
					//current.Use();
				break;
      			case EventType.layout:
			      // HandleUtility.AddDefaultControl(controlID);
        		break;
				
			}
			
		}
		
		// Node Numbers
		if (_target.showNodeNumbers) {
			for(i=0; i<_target.control_points.Count; i++) {
				Handles.Label(T_lw(_target.control_points[i]), "  "+i);
			}
		}
		
		// tesselation points labels
		if ((_target.state==0) || (_target.show_tesselation_points)) {
			for(i=0; i<_target.tesselation_points.Count; i++) {
				Handles.Label(T_lw(_target.tesselation_points[i]), " tp");
			}
		}
		
		// control_points
		for(i=0; i<_target.control_points.Count; i++) {
          Vector3 vec= 
            Handles.FreeMoveHandle(T_lw(_target.control_points[i]), 
            Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.control_points[i]))*0.08f, Vector3.one, 
            Handles.RectangleCap);
			if ( (_target.state==0) && (Vector3.Distance(vec, T_lw(_target.control_points[i]))>0) ) {
				if (!_target.undo_flag) {
					Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
					_target.undo_flag=true;
				}
				Vector3 delta_bezierA=T_lw(_target.bezier_pointsA[i])-T_lw(_target.control_points[i]);
				Vector3 delta_bezierB=T_lw(_target.bezier_pointsB[i])-T_lw(_target.control_points[i]);
				if (_target.snap_on_move)
					_target.control_points[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
				else
					_target.control_points[i]=T_wl(vec);
				_target.bezier_pointsA[i]=T_wl(T_lw(_target.control_points[i])+delta_bezierA);
				_target.bezier_pointsB[i]=T_wl(T_lw(_target.control_points[i])+delta_bezierB);
			}
		}
		Handles.color=Color.gray;
		// tesselation point handles
		if ((_target.state==0) || (_target.show_tesselation_points)) {
			for(i=0; i<_target.tesselation_points.Count; i++) {
	          Vector3 vec= 
	            Handles.FreeMoveHandle(T_lw(_target.tesselation_points[i]), 
	            Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.tesselation_points[i]))*0.06f, Vector3.one, 
	            Handles.CircleCap);
				if (Vector3.Distance(vec, T_lw(_target.tesselation_points[i]))>0) {
					if (!_target.undo_flag) {
						Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
						_target.undo_flag=true;
					}
					if (_target.snap_on_move)
						_target.tesselation_points[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
					else
						_target.tesselation_points[i]=T_wl(vec);
				}
			}
		}
		// custom UV grid bounds handles
		if (_target.custom_UV_bounds) {
			Vector3 handle_pos, vec;
			// minx
			handle_pos=new Vector3(_target.custom_minx, _target.transform.position.y, 0.5f*(_target.custom_minz+_target.custom_maxz));
            vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.x-handle_pos.x)>0) {
				if (!_target.undo_flag) {
					Undo.RegisterUndo(_target, "grass edit");
					_target.undo_flag=true;
				}
				if (vec.x>_target.minx) vec.x=_target.minx;
				_target.custom_minx=vec.x;
			}
			// maxx
			handle_pos=new Vector3(_target.custom_maxx, _target.transform.position.y, 0.5f*(_target.custom_minz+_target.custom_maxz));
            vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.x-handle_pos.x)>0) {
				if (!_target.undo_flag) {
					Undo.RegisterUndo(_target, "grass edit");
					_target.undo_flag=true;
				}
				if (vec.x<_target.maxx+0.1f) vec.x=_target.maxx+0.1f;
				_target.custom_maxx=vec.x;
			}
			// minz
			handle_pos=new Vector3(0.5f*(_target.custom_minx+_target.custom_maxx), _target.transform.position.y, _target.custom_minz);
            vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.z-handle_pos.z)>0) {
				if (!_target.undo_flag) {
					Undo.RegisterUndo(_target, "grass edit");
					_target.undo_flag=true;
				}
				if (vec.z>_target.minz) vec.z=_target.minz;
				_target.custom_minz=vec.z;
			}
			// maxz
			handle_pos=new Vector3(0.5f*(_target.custom_minx+_target.custom_maxx), _target.transform.position.y, _target.custom_maxz);
            vec=Handles.FreeMoveHandle(handle_pos, Quaternion.identity, HandleUtility.GetHandleSize(handle_pos)*0.04f, Vector3.one, Handles.DotCap);
			if (Mathf.Abs(vec.z-handle_pos.z)>0) {
				if (!_target.undo_flag) {
					Undo.RegisterUndo(_target, "grass edit");
					_target.undo_flag=true;
				}
				if (vec.z<_target.maxz+0.1f) vec.z=_target.maxz+0.1f;
				_target.custom_maxz=vec.z;
			}
		}
		
		if (_target.state==0) {
			if (_target.which_active!=3) {
				// bezier handle A
				for(i=0; i<_target.control_points.Count; i++) {
					if ( (_target.active_idx==i) && ((_target.which_active==1) || (Vector3.Distance(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsA[i]))>0.01f)) ) {
			          Vector3 vec= 
			            Handles.FreeMoveHandle(T_lw(_target.bezier_pointsA[i]), 
			            Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.bezier_pointsA[i]))*0.05f, Vector3.one, 
			            Handles.CircleCap);
						if (Vector3.Distance(vec, T_lw(_target.bezier_pointsA[i]))>0) {
							if (!_target.undo_flag) {
								Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
								_target.undo_flag=true;
							}
							if (_target.snap_on_move)
								_target.bezier_pointsA[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
							else
								_target.bezier_pointsA[i]=T_wl(vec);
						}
						Handles.DrawLine(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsA[i]));
					}
				}
				// bezier handle B
				for(i=0; i<_target.control_points.Count; i++) {
					if ( (_target.active_idx==i) && ((_target.which_active==2) || (Vector3.Distance(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsB[i]))>0.01f)) ) {
			          Vector3 vec= 
			            Handles.FreeMoveHandle(T_lw(_target.bezier_pointsB[i]), 
			            Quaternion.identity, HandleUtility.GetHandleSize(T_lw(_target.bezier_pointsB[i]))*0.05f, Vector3.one, 
			            Handles.CircleCap);
						if (Vector3.Distance(vec, T_lw(_target.bezier_pointsB[i]))>0) {
							if (!_target.undo_flag) {
								Undo.RegisterUndo(new Object[2]{_target, _target.transform}, "grass edit");
								_target.undo_flag=true;
							}
							if (_target.snap_on_move)
								_target.bezier_pointsB[i]=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
							else
								_target.bezier_pointsB[i]=T_wl(vec);
						}
						Handles.DrawLine(T_lw(_target.control_points[i]), T_lw(_target.bezier_pointsB[i]));
					}
				}
			}
		}
		
	}
	private void get_paint_coverage() {
		if (Time.realtimeSinceStartup<lCovTim) return;
		lCovTim=Time.realtimeSinceStartup+0.04f;
		VolumeGrass _target=(VolumeGrass)target;		
		Vector3[] vertices=_target.get_volume_vertices();
		Vector3[] normals=_target.get_volume_normals();
		Color[] colors=_target.get_volume_colors();
		if ((vertices!=null) && (normals!=null)) {
			Vector3 pnt=T_wl(GetWorldPointFromMouse(_target.ground_layerMask));
			cover_verts_num=0;
			for(int i=0; i<vertices.Length; i++) {
				float dist=Vector3.Distance(pnt, vertices[i]);
				if ((cover_verts_num<cover_verts.Length) && (colors[i].r==0) && (dist<_target.paint_size)) {
					cover_verts[cover_verts_num]=T_lw(vertices[i]);
					//cover_norms[cover_verts_num]=Quaternion.LookRotation(normals[i]);
					//cover_norms_flip[cover_verts_num]=Quaternion.LookRotation(-normals[i]);
					cover_strength[cover_verts_num]=(_target.paint_size-dist*_target.paint_smoothness)/_target.paint_size;
					cover_indices[cover_verts_num]=i;
					cover_verts_num++;
				}
			}
		}
		EditorUtility.SetDirty(_target);
	}		
    private Vector3 GetWorldPointFromMouse(LayerMask layerMask)
    {
		float planeLevel = 0;
        var groundPlane = new Plane(Vector3.up, new Vector3(0, planeLevel, 0));

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit rayHit;
        Vector3 hit = new Vector3(0,0,0);
        float dist;
		
        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, 1<<layerMask.value))
            hit = rayHit.point;
        else if (groundPlane.Raycast(ray, out dist))
            hit = ray.origin + ray.direction.normalized * dist;

        return hit;
    }
	
	Vector3 T_lw(Vector3 input) {
		VolumeGrass _target=(VolumeGrass)target;
		return _target.transform.TransformPoint(input);
	}
	Vector3 T_wl(Vector3 input) {
		VolumeGrass _target=(VolumeGrass)target;
		return _target.transform.InverseTransformPoint(input);
	}
#endif
}
