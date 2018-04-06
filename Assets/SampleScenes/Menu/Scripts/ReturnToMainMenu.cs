using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenu : MonoBehaviour
{
    private bool m_Levelloaded;


    public void Start()
    {
        DontDestroyOnLoad(this);
    }


    private void OnLevelWasLoaded(int level)
    {
        m_Levelloaded = true;
    }


    private void Update()
    {
        if (m_Levelloaded)
        {
            Canvas component = gameObject.GetComponent<Canvas>();
            component.enabled = false;
            component.enabled = true;
            m_Levelloaded = false;
        }
    }


    public void GoBackToMainMenu()
    {
        Debug.Log("going back to main menu");
        SceneManager.LoadScene("MainMenu");
    }
}
