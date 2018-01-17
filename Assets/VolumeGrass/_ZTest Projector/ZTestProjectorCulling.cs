using UnityEngine;
using System.Collections;

//
// attach this to projectors that render on VolumeGrass
// so they ignore inner sidewalls of grass
//
public class ZTestProjectorCulling : MonoBehaviour {
	
	void Awake () {
		Projector proj=GetComponent<Projector>();
		if (proj) {
			proj.ignoreLayers|=(1<<GrassRenderingReservedLayer.layer_num);
		}
	}
	
}
