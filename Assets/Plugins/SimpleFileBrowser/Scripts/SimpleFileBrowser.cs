using UnityEngine;
using UnityEngine.UI;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class SimpleFileBrowser : MonoBehaviour
{
	#region Structs
	[Serializable]
	private struct FiletypeIcon
	{
		public string extension;
		public Sprite icon;
	}

	[Serializable]
	private struct QuickLink
	{
		public Environment.SpecialFolder target;
		public string name;
		public Sprite icon;
	}
	#endregion

	#region Constants
	private const string ALL_FILES_FILTER_TEXT = "All Files (.*)";
	private string DEFAULT_PATH = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
	#endregion

	#region Static Variables
	public static bool Success = false;
	public static string Result = null;

	private static SimpleFileBrowser m_instance = null;
	private static SimpleFileBrowser instance
	{
		get
		{
			if( m_instance == null )
			{
				m_instance = Instantiate<GameObject>( Resources.Load<GameObject>( "SimpleFileBrowserCanvas" ) ).GetComponent<SimpleFileBrowser>();
				DontDestroyOnLoad( m_instance.gameObject );
				m_instance.gameObject.SetActive( false );
			}

			return m_instance;
		}
	}
	#endregion

	#region Variables
	[Header( "References" )]

	[SerializeField]
	private SimpleFileBrowserItem itemPrefab;

	[SerializeField]
	private SimpleFileBrowserQuickLink quickLinkPrefab;

	private List<SimpleFileBrowserItem> items = new List<SimpleFileBrowserItem>();
	private int activeItemCount = 0;

	[SerializeField]
	private Text titleText;

	[SerializeField]
	private InputField pathInputField;

	[SerializeField]
	private InputField searchInputField;
	
	[SerializeField]
	private RectTransform quickLinksContainer;

	[SerializeField]
	private RectTransform filesContainer;

	[SerializeField]
	private ScrollRect filesScrollRect;

	[SerializeField]
	private InputField filenameInputField;

	[SerializeField]
	private Image filenameImage;

	[SerializeField]
	private Dropdown filtersDropdown;

	[SerializeField]
	private Toggle showHiddenFilesToggle;

	[SerializeField]
	private Text submitButtonText;

	[Header( "Icons" )]

	[SerializeField]
	private Sprite folderIcon;

	[SerializeField]
	private Sprite driveIcon;

	[SerializeField]
	private Sprite defaultIcon;

	[SerializeField]
	private FiletypeIcon[] filetypeIcons;

	private Dictionary<string, Sprite> filetypeToIcon;

	[Header( "Other" )]

	public Color normalFileColor = Color.white;
	public Color hoveredFileColor = new Color32( 225, 225, 255, 255 );
	public Color selectedFileColor = new Color32( 0, 175, 255, 255 );

	public Color wrongFilenameColor = new Color32( 255, 100, 100, 255 );
	
	public int minWidth = 380;
	public int minHeight = 300;

	[SerializeField]
	private string[] excludeExtensions;

	private HashSet<string> excludedExtensionsSet;
	
	[SerializeField]
	private QuickLink[] quickLinks;

	[SerializeField]
	private bool generateQuickLinksForDrives = true;

	private FileAttributes ignoredFileAttributes = FileAttributes.System;

	private List<string> filters = new List<string>( new string[] { ALL_FILES_FILTER_TEXT } );
	
	private float itemHeight;
	
	private int currentPathIndex = -1;
	private List<string> pathsFollowed = new List<string>();

	// Required in RefreshFiles() function
	private UnityEngine.EventSystems.PointerEventData nullPointerEventData;
	#endregion

	#region Properties
	private string m_currentPath = string.Empty;
	private string CurrentPath
	{
		get
		{
			return m_currentPath;
		}
		set
		{
			if( m_currentPath != value )
			{
				if( !Directory.Exists( value ) )
					return;

				m_currentPath = value;
				pathInputField.text = m_currentPath;

				if( currentPathIndex == -1 || pathsFollowed[currentPathIndex] != m_currentPath )
				{
					currentPathIndex++;
					if( currentPathIndex < pathsFollowed.Count )
					{
						pathsFollowed[currentPathIndex] = value;
						for( int i = pathsFollowed.Count - 1; i >= currentPathIndex + 1; i-- )
						{
							pathsFollowed.RemoveAt( i );
						}
					}
					else
					{
						pathsFollowed.Add( m_currentPath );
					}
				}

				m_searchString = string.Empty;
				searchInputField.text = m_searchString;

				filesScrollRect.verticalNormalizedPosition = 1;

				filenameImage.color = Color.white;

				RefreshFiles();
			}
		}
	}

	private string m_searchString = string.Empty;
	private string SearchString
	{
		get
		{
			return m_searchString;
		}
		set
		{
			if( m_searchString != value )
			{
				m_searchString = value;
				searchInputField.text = m_searchString;

				RefreshFiles();
			}
		}
	}

	private SimpleFileBrowserItem m_selectedFile;
	public SimpleFileBrowserItem SelectedFile
	{
		get
		{
			return m_selectedFile;
		}
		private set
		{
			if( m_selectedFile != value )
			{
				if( m_selectedFile != null )
					m_selectedFile.Deselect();

				m_selectedFile = value;

				if( m_selectedFile != null )
				{
					if( m_folderSelectMode || !m_selectedFile.IsDirectory )
						filenameInputField.text = m_selectedFile.Name;

					m_selectedFile.Select();
				}
			}
		}
	}

	private bool m_acceptNonExistingFilename = false;
	public bool AcceptNonExistingFilename
	{
		get
		{
			return m_acceptNonExistingFilename;
		}
		set
		{
			if( m_acceptNonExistingFilename != value )
			{
				m_acceptNonExistingFilename = value;
			}
		}
	}

	private bool m_folderSelectMode = false;
	public bool FolderSelectMode
	{
		get
		{
			return m_folderSelectMode;
		}
		set
		{
			if( m_folderSelectMode != value )
			{
				m_folderSelectMode = value;

				if( m_folderSelectMode )
				{
					filtersDropdown.options[0].text = "Folders";
					filtersDropdown.value = 0;
					filtersDropdown.RefreshShownValue();
					filtersDropdown.interactable = false;
				}
				else
				{
					filtersDropdown.options[0].text = ALL_FILES_FILTER_TEXT;
					filtersDropdown.interactable = true;
				}
			}
		}
	}

	public string Title
	{
		get
		{
			return titleText.text;
		}
		set
		{
			titleText.text = value;
		}
	}

	public string SubmitButtonText
	{
		get
		{
			return submitButtonText.text;
		}
		set
		{
			submitButtonText.text = value;
		}
	}
	#endregion

	#region Delegates
	public delegate void OnSuccess( string path );
	public delegate void OnCancel();

	public event OnSuccess onSuccess;
	public event OnCancel onCancel;
	#endregion

	#region Messages
	void Awake()
	{
		m_instance = this;

		itemHeight = ( (RectTransform) itemPrefab.transform ).sizeDelta.y;

		nullPointerEventData = new UnityEngine.EventSystems.PointerEventData( null );

		InitializeFiletypeIcons();
		filetypeIcons = null;

		SetExcludedExtensions( excludeExtensions );
		excludeExtensions = null;

		filenameInputField.onValidateInput += OnValidateFilenameInput;

		InitializeQuickLinks();
	}
	
	void OnApplicationFocus( bool focus )
	{
		if( focus )
		{
			RefreshFiles();
		}
	}
	#endregion

	#region Initialization Functions
	private void InitializeFiletypeIcons()
	{
		filetypeToIcon = new Dictionary<string, Sprite>();
		for( int i = 0; i < filetypeIcons.Length; i++ )
		{
			FiletypeIcon thisIcon = filetypeIcons[i];
			filetypeToIcon[thisIcon.extension] = thisIcon.icon;
		}
	}

	private void InitializeQuickLinks()
	{
		Vector2 anchoredPos = new Vector2( 0f, -quickLinksContainer.sizeDelta.y );

		if( generateQuickLinksForDrives )
		{
			string[] drives = Directory.GetLogicalDrives();
			
			for( int i = 0; i < drives.Length; i++ )
			{
				AddQuickLink( driveIcon, drives[i], drives[i], ref anchoredPos );
			}
		}

		for( int i = 0; i < quickLinks.Length; i++ )
		{
			QuickLink quickLink = quickLinks[i];
			string quickLinkPath = Environment.GetFolderPath( quickLink.target );

			if( !Directory.Exists( quickLinkPath ) )
				continue;

			AddQuickLink( quickLink.icon, quickLink.name, quickLinkPath, ref anchoredPos );
		}

		quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );
	}
	#endregion

	#region Button Events
	public void OnBackButtonPressed()
	{
		if( currentPathIndex > 0 )
		{
			currentPathIndex--;
			CurrentPath = pathsFollowed[currentPathIndex];
		}
	}

	public void OnForwardButtonPressed()
	{
		if( currentPathIndex < pathsFollowed.Count - 1 )
		{
			currentPathIndex++;
			CurrentPath = pathsFollowed[currentPathIndex];
		}
	}

	public void OnUpButtonPressed()
	{
		DirectoryInfo parentPath = Directory.GetParent( m_currentPath );

		if( parentPath != null )
			CurrentPath = parentPath.FullName;
	}

	public void OnSubmitButtonClicked()
	{
		string path = m_currentPath;
		if( filenameInputField.text.Length > 0 )
			path = Path.Combine( path, filenameInputField.text );
		else
			path = GetPathWithoutTrailingDirectorySeparator( path );
		
		if( File.Exists( path ) )
		{
			if( !m_folderSelectMode )
			{
				OnOperationSuccessful( path );
			}
			else
			{
				filenameImage.color = wrongFilenameColor;
			}
		}
		else if( Directory.Exists( path ) )
		{
			if( m_folderSelectMode )
			{
				OnOperationSuccessful( path );
			}
			else
			{
				if( m_currentPath == path )
					filenameImage.color = wrongFilenameColor;
				else
					CurrentPath = path;
			}
		}
		else
		{
			if( m_acceptNonExistingFilename )
			{
				if( !m_folderSelectMode && filtersDropdown.value != 0 )
					path = Path.ChangeExtension( path, filters[filtersDropdown.value] );

				OnOperationSuccessful( path );
			}
			else
			{
				filenameImage.color = wrongFilenameColor;
			}
		}
	}

	public void OnCancelButtonClicked()
	{
		OnOperationCanceled();
	}
	#endregion

	#region Other Events
	private void OnOperationSuccessful( string path )
	{
		Success = true;
		Result = path;

		Hide();

		if( onSuccess != null )
		{
			onSuccess( path );
		}
	}

	private void OnOperationCanceled()
	{
		Success = false;
		Result = null;

		Hide();

		if( onCancel != null )
		{
			onCancel();
		}
	}
	
	public void OnPathChanged( string newPath )
	{
		newPath = GetPathWithoutTrailingDirectorySeparator( newPath );
		CurrentPath = newPath;
	}

	public void OnSearchStringChanged( string newSearchString )
	{
		SearchString = newSearchString;
	}

	public void OnFilterChanged()
	{
		RefreshFiles();
	}

	public void OnShowHiddenFilesToggleChanged()
	{
		RefreshFiles();
	}

	public void OnQuickLinkSelected( SimpleFileBrowserQuickLink quickLink )
	{
		if( quickLink != null )
		{
			CurrentPath = quickLink.TargetPath;
		}
	}

	public void OnItemSelected( SimpleFileBrowserItem item )
	{
		SelectedFile = item;
	}

	public void OnItemOpened( SimpleFileBrowserItem item )
	{
		if( item.IsDirectory )
		{
			CurrentPath = Path.Combine( m_currentPath, item.Name );
		}
		else
		{
			OnSubmitButtonClicked();
		}
	}

	public char OnValidateFilenameInput( string text, int charIndex, char addedChar )
	{
		if( addedChar == '\n' )
		{
			OnSubmitButtonClicked();
			return '\0';
		}

		return addedChar;
	}
	#endregion

	#region Helper Functions
	public void Show()
	{
		currentPathIndex = -1;
		pathsFollowed.Clear();

		SelectedFile = null;

		m_searchString = string.Empty;
		searchInputField.text = m_searchString;

		filesScrollRect.verticalNormalizedPosition = 1;

		filenameImage.color = Color.white;

		Success = false;
		Result = null;

		gameObject.SetActive( true );
	}

	public void Hide()
	{
		gameObject.SetActive( false );
	}

	public void RefreshFiles()
	{
		if( !Directory.Exists( m_currentPath ) )
			return;

		SelectedFile = null;

		for( int i = 0; i < activeItemCount; i++ )
		{
			items[i].gameObject.SetActive( false );
		}

		activeItemCount = 0;

		if( !showHiddenFilesToggle.isOn )
			ignoredFileAttributes |= FileAttributes.Hidden;
		else
			ignoredFileAttributes &= ~FileAttributes.Hidden;

		string[] files;
		
		files = Directory.GetDirectories( m_currentPath );

		TryAddFiles( files, true );

		if( !m_folderSelectMode )
		{
			files = Directory.GetFiles( m_currentPath );

			TryAddFiles( files, false );
		}

		filesContainer.sizeDelta = new Vector2( 0f, activeItemCount * itemHeight );

		// Prevent the case where the all the content stays offscreen after changing the search string
		filesScrollRect.OnScroll( nullPointerEventData );
	}

	private void TryAddFiles( string[] paths, bool isDirectory )
	{
		string searchStringLowercase = m_searchString.ToLower();

		for( int i = 0; i < paths.Length; i++ )
		{
			try
			{
				if( !isDirectory )
				{
					FileInfo fileInfo = new FileInfo( paths[i] );
					if( ( fileInfo.Attributes & ignoredFileAttributes ) != 0 )
						continue;

					string extension = Path.GetExtension( paths[i] ).ToLower();
					if( excludedExtensionsSet.Contains( extension ) ||
						( filtersDropdown.value != 0 && extension != filters[filtersDropdown.value] ) )
						continue;
				}
				else
				{
					DirectoryInfo directoryInfo = new DirectoryInfo( paths[i] );
					if( ( directoryInfo.Attributes & ignoredFileAttributes ) != 0 )
						continue;
				}

				if( m_searchString.Length == 0 )
				{
					AddFile( paths[i], isDirectory );
				}
				else
				{
					string filename = Path.GetFileName( paths[i] ).ToLower();
					if( filename.StartsWith( searchStringLowercase ) || filename.EndsWith( searchStringLowercase ) )
						AddFile( paths[i], isDirectory );
				}
			}
			catch( Exception e )
			{
				Debug.LogException( e );
				return;
			}
		}
	}

	private void AddFile( string path, bool isDirectory )
	{
		if( activeItemCount == filesContainer.childCount )
		{
			SimpleFileBrowserItem item = (SimpleFileBrowserItem) Instantiate( itemPrefab, filesContainer, false );
			item.SetFileBrowser( this );
			items.Add( item );

			item.transformComponent.anchoredPosition = new Vector2( 0f, -activeItemCount * itemHeight );
		}
		else
		{
			items[activeItemCount].gameObject.SetActive( true );
		}

		Sprite icon;
		if( isDirectory )
			icon = folderIcon;
		else if( !filetypeToIcon.TryGetValue( Path.GetExtension( path ).ToLower(), out icon ) )
			icon = defaultIcon;

		items[activeItemCount].SetFile( icon, Path.GetFileName( path ), isDirectory );

		activeItemCount++;
	}

	private void AddQuickLink( Sprite icon, string name, string path, ref Vector2 anchoredPos )
	{
		SimpleFileBrowserQuickLink quickLink = (SimpleFileBrowserQuickLink) Instantiate( quickLinkPrefab, quickLinksContainer, false );
		quickLink.SetFileBrowser( this );

		if( icon != null )
			quickLink.SetQuickLink( icon, name, path );
		else
			quickLink.SetQuickLink( folderIcon, name, path );

		quickLink.transformComponent.anchoredPosition = anchoredPos;

		anchoredPos.y -= itemHeight;
	}

	private string GetPathWithoutTrailingDirectorySeparator( string path )
	{
		// Credit: http://stackoverflow.com/questions/6019227/remove-the-last-character-if-its-directoryseparatorchar-with-c-sharp
		if( Path.GetDirectoryName( path ) != null && ( path[path.Length - 1] == '\\' || path[path.Length - 1] == '/' ) )
			path = path.TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );

		return path;
	}
	#endregion

	#region File Browser Functions (static)
	public static bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel,
									   bool folderMode = false, string initialPath = null,
									   string title = "Save", string saveButtonText = "Save" )
	{
		if( instance.gameObject.activeSelf )
		{
			Debug.LogError( "Error: Multiple dialogs are not allowed!" );
			return false;
		}
		
		if( ( initialPath == null || !Directory.Exists( initialPath ) ) && instance.m_currentPath.Length == 0 )
			initialPath = instance.DEFAULT_PATH;

		instance.onSuccess = onSuccess;
		instance.onCancel = onCancel;

		instance.FolderSelectMode = folderMode;
		instance.Title = title;
		instance.SubmitButtonText = saveButtonText;

		instance.AcceptNonExistingFilename = !folderMode;

		instance.Show();
		instance.CurrentPath = initialPath;

		return true;
	}

	public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel, 
									   bool folderMode = false, string initialPath = null,
									   string title = "Load", string loadButtonText = "Select" )
	{
		if( instance.gameObject.activeSelf )
		{
			Debug.LogError( "Error: Multiple dialogs are not allowed!" );
			return false;
		}

		if( ( initialPath == null || !Directory.Exists( initialPath ) ) && instance.m_currentPath.Length == 0 )
			initialPath = instance.DEFAULT_PATH;

		instance.onSuccess = onSuccess;
		instance.onCancel = onCancel;

		instance.FolderSelectMode = folderMode;
		instance.Title = title;
		instance.SubmitButtonText = loadButtonText;

		instance.AcceptNonExistingFilename = false;
		
		instance.Show();
		instance.CurrentPath = initialPath;

		return true;
	}

	public static IEnumerator WaitForSaveDialog( bool folderMode = false, string initialPath = null,
												 string title = "Save", string saveButtonText = "Save" )
	{
		if( instance.gameObject.activeSelf )
		{
			Debug.LogError( "Error: Multiple dialogs are not allowed!" );
			yield break;
		}

		ShowSaveDialog( null, null, folderMode, initialPath, title, saveButtonText );

		while( instance.gameObject.activeSelf )
			yield return null;
	}

	public static IEnumerator WaitForLoadDialog( bool folderMode = false, string initialPath = null,
												 string title = "Load", string loadButtonText = "Select" )
	{
		if( instance.gameObject.activeSelf )
		{
			Debug.LogError( "Error: Multiple dialogs are not allowed!" );
			yield break;
		}

		ShowLoadDialog( null, null, folderMode, initialPath, title, loadButtonText );

		while( instance.gameObject.activeSelf )
			yield return null;
	}

	public static bool AddQuickLink( Sprite icon, string name, string path )
	{
		if( Directory.Exists( path ) )
		{
			Vector2 anchoredPos = new Vector2( 0f, -instance.quickLinksContainer.sizeDelta.y );

			path = instance.GetPathWithoutTrailingDirectorySeparator( path );
			instance.AddQuickLink( icon, name, path, ref anchoredPos );

			instance.quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );

			return true;
		}

		return false;
	}

	public static void SetExcludedExtensions( params string[] excludedExtensions )
	{
		if( instance.excludedExtensionsSet == null )
			instance.excludedExtensionsSet = new HashSet<string>();
		else
			instance.excludedExtensionsSet.Clear();

		if( excludedExtensions != null )
		{
			for( int i = 0; i < excludedExtensions.Length; i++ )
			{
				instance.excludedExtensionsSet.Add( excludedExtensions[i].ToLower() );
			}
		}
	}

	public static void SetFilters( List<string> filters )
	{
		if( filters == null )
			filters = new List<string>();

		for( int i = 0; i < filters.Count; i++ )
			filters[i] = filters[i].ToLower();

		filters.Insert( 0, ALL_FILES_FILTER_TEXT );
		
		instance.filtersDropdown.ClearOptions();
		instance.filtersDropdown.AddOptions( filters );

		instance.filters = filters;
	}

	public static void SetFilters( params string[] filters )
	{
		List<string> filtersList;
		if( filters == null )
			filtersList = new List<string>( 1 );
		else
			filtersList = new List<string>( filters.Length + 1 );

		filtersList.Add( ALL_FILES_FILTER_TEXT );

		if( filters != null )
		{
			for( int i = 0; i < filters.Length; i++ )
			{
				filtersList.Add( filters[i].ToLower() );
			}
		}

		instance.filtersDropdown.ClearOptions();
		instance.filtersDropdown.AddOptions( filtersList );

		instance.filters = filtersList;
	}

	public static bool SetDefaultFilter( string defaultFilter )
	{
		defaultFilter = defaultFilter.ToLower();

		for( int i = 0; i < instance.filters.Count; i++ )
		{
			if( instance.filters[i] == defaultFilter )
			{
				instance.filtersDropdown.value = i;
				instance.filtersDropdown.RefreshShownValue();

				return true;
			}
		}

		return false;
	}
	#endregion
}