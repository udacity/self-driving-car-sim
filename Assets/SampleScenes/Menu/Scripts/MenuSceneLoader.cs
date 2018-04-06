using System;
using UnityEngine;

public class MenuSceneLoader : MonoBehaviour
{
    public GameObject menuUI;

    private GameObject m_Go;

	void Awake ()
	{
	    if (m_Go == null)
	    {
	        m_Go = Instantiate(menuUI);
	    }
	}
}
