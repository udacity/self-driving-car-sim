using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashTimer : MonoBehaviour {

	// Use this for initialization
	void Start () {
		StartCoroutine (LoadMenuScene());
	}

	IEnumerator LoadMenuScene(){
		yield return new WaitForSeconds (1.5f);
		SceneManager.LoadScene ("MenuScene");
	}		
}
