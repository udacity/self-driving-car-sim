using UnityEngine;
using System.Collections;
using UnityEngine.UI;
/// <summary>
/// Created for GameSparks tutorial
/// Sean Durkan 3, 8, 2015
/// </summary>
public class ScrollbarScript : MonoBehaviour 
{
	/// <summary>This is a refrence to the scrollbar object, attached through the unity editor</summary>
	public Scrollbar scrollbar;
	/// <summary>We need the input transform rectangle so we can get the height and size of the text</summary>
	public RectTransform inputTxtRect;
	/// <summary>This is where the text is output to the scene</summary>
	private Text inputTxt;
	Vector2 previousSize, startPos; // used to update the size when text is entered, and the position when the text is scrolled

	void Start ()
	{
		scrollbar.onValueChanged.AddListener(OnValueChanged); // assign the scrollbar listener here
		inputTxt = inputTxtRect.GetComponent<Text> ();
		previousSize = inputTxtRect.sizeDelta;
	}

	void OnValueChanged (float _value)
	{
		// we can make sure the scrollbar will only scroll the right value if we use the scroll-value as a percentage of how far the text can move
		inputTxtRect.anchoredPosition = new Vector2(inputTxtRect.anchoredPosition.x , startPos.y + inputTxtRect.sizeDelta.y * _value);
	}
	

	void LateUpdate () 
	{
		// We update the size and position of the text-box based on how many lines are in the text //
		// each line is a player score and rank. We also have to adjust the position for the new size //
		int lines = inputTxt.text.Split ('\n').Length; // we can get the number of lines by slitting for each line-break
		if (inputTxtRect.sizeDelta.y < (lines*inputTxt.fontSize))  // if the size is smaller than it should be, then new lines have been added
		{
			inputTxtRect.sizeDelta = new Vector2 (inputTxtRect.sizeDelta.x, lines*inputTxt.fontSize); // we increase the size
			float deltaY = inputTxtRect.sizeDelta.y - previousSize.y; // we get the difference in size in pixel, this will be how much we adjust the position for the new size
			inputTxtRect.anchoredPosition -= new Vector2(0, deltaY/2); // we set the new position
			previousSize = inputTxtRect.sizeDelta; // update the previous position
			startPos = inputTxtRect.anchoredPosition; // update the start position, used in the OnValueChanged listener
			print ("Leaderboard Data Updated...");
		}
	}
}














