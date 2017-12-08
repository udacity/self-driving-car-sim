using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.IO;

public class image_viewer : MonoBehaviour {

	public string filesLocation = @"D:\udacity\slam\icp_lab\icp_grid\src";

	private Texture2D tex;
	private Sprite mySprite;
	private SpriteRenderer sr;

	void Awake()
	{
		sr = gameObject.AddComponent<SpriteRenderer>() as SpriteRenderer;
		sr.color = new Color(0.9f, 0.9f, 0.9f, 1.0f);

		transform.position = new Vector3(1.5f, 1.5f, 0.0f);
	}

	public IEnumerator Start () {
		yield return StartCoroutine(
			"LoadAll",
			Directory.GetFiles(filesLocation, "my_map.png", SearchOption.AllDirectories)
		);
	}

	public IEnumerator LoadAll (string[] filePaths) {
		foreach (string filePath in filePaths) {
			WWW load = new WWW("file:///"+filePath);
			yield return load;
			if (!string.IsNullOrEmpty(load.error)) {
				Debug.LogWarning(filePath + " error");
			} else {
				tex = load.texture;
				mySprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
				sr.sprite = mySprite;
			}
		}
	}
}
