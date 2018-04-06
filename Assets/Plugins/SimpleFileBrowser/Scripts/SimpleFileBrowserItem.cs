using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleFileBrowserItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	#region Constants
	private const float DOUBLE_CLICK_TIME = 0.5f;
	#endregion

	#region Variables
	protected SimpleFileBrowser fileBrowser;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Image icon;
	
	[SerializeField]
	private Text nameText;

	private float prevTouchTime = Mathf.NegativeInfinity;
	#endregion

	#region Properties
	private RectTransform m_transform;
	public RectTransform transformComponent
	{
		get
		{
			if( m_transform == null )
				m_transform = (RectTransform) transform;

			return m_transform;
		}
	}

	public string Name { get { return nameText.text; } }

	private bool m_isDirectory;
	public bool IsDirectory { get { return m_isDirectory; } }
	#endregion

	#region Initialization Functions
	public void SetFileBrowser( SimpleFileBrowser fileBrowser )
	{
		this.fileBrowser = fileBrowser;
	}

	public void SetFile( Sprite icon, string name, bool isDirectory )
	{
		this.icon.sprite = icon;
		nameText.text = name;

		m_isDirectory = isDirectory;
	}
	#endregion

	#region Pointer Events
	public void OnPointerClick( PointerEventData eventData )
	{
		if( Time.realtimeSinceStartup - prevTouchTime < DOUBLE_CLICK_TIME )
		{
			if( fileBrowser.SelectedFile == this )
				fileBrowser.OnItemOpened( this );

			prevTouchTime = Mathf.NegativeInfinity;
		}
		else
		{
			fileBrowser.OnItemSelected( this );
			prevTouchTime = Time.realtimeSinceStartup;
		}
	}

	public void OnPointerEnter( PointerEventData eventData )
	{
		if( fileBrowser.SelectedFile != this )
			background.color = fileBrowser.hoveredFileColor;
	}

	public void OnPointerExit( PointerEventData eventData )
	{
		if( fileBrowser.SelectedFile != this )
			background.color = fileBrowser.normalFileColor;
	}
	#endregion

	#region Other Events
	public void Select()
	{
		background.color = fileBrowser.selectedFileColor;
	}

	public void Deselect()
	{
		background.color = fileBrowser.normalFileColor;
	}
	#endregion
}