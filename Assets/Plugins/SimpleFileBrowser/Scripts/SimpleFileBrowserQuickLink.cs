using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleFileBrowserQuickLink : SimpleFileBrowserItem, IPointerClickHandler
{
	#region Properties
	private string m_targetPath;
	public string TargetPath { get { return m_targetPath; } }
	#endregion

	#region Initialization Functions
	public void SetQuickLink( Sprite icon, string name, string targetPath )
	{
		SetFile( icon, name, true );

		m_targetPath = targetPath;
	}
	#endregion

	#region Pointer Events
	public new void OnPointerClick( PointerEventData eventData )
	{
		fileBrowser.OnQuickLinkSelected( this );
	}
	#endregion

	#region Other Events
	#endregion
}