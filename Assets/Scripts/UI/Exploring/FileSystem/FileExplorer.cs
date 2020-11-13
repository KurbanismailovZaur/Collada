using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using System;
using UnityEngine.Events;
using UnityEngine.UI;
using Exceptions;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace UI.Exploring.FileSystem
{
    public class FileExplorer : MonoSingleton<FileExplorer>
    {
        #region Classes
        [Serializable]
        public class SubmitedEvent : UnityEvent<ReadOnlyCollection<string>> { }

        private abstract class State
        {
            public enum Type
            {
                OpenFile,
                SaveFile
            }

            protected FileExplorer _explorer;

            protected State(FileExplorer explorer) => _explorer = explorer;

            public virtual void ShowContent(string path, bool updateHistory = true) => _explorer.ShowContent(path, updateHistory);

            public virtual void DeselectCurrentElement() => _explorer.DeselectCurrentElement();

            public abstract void SetFooterState(string filename);

            public abstract void Submit();

            private void SetResults(string[] results)
            {
                _explorer._lastResults = results;
                _explorer.LastResult = _explorer._lastResults.Length > 0 ? _explorer._lastResults[0] : null;
            }

            protected void SetResultsAndSubmit(string[] results)
            {
                SetResults(results);
                _explorer.Submited.Invoke(new ReadOnlyCollection<string>(_explorer._lastResults));
            }

            #region Event handlers
            public abstract void FilenameInput_OnEndEdit(string name);
            #endregion
        }

        private class OpenFileState : State
        {
            public OpenFileState(FileExplorer explorer) : base(explorer) { }

            public override void DeselectCurrentElement()
            {
                base.DeselectCurrentElement();

                _explorer._submitButton.interactable = false;
                _explorer._nameInput.text = string.Empty;
            }

            public override void SetFooterState(string filename)
            {
                _explorer.SetSubmitState(_explorer._openFileSubmitText, false);
            }

            public override void Submit()
            {
                SetResultsAndSubmit(new string[] { _explorer._currentElement.Path });
            }

            #region Event handlers
            public override void FilenameInput_OnEndEdit(string name)
            {
                _explorer.SelectElementByName(name);
            }
            #endregion
        }

        private class SaveFileState : State
        {
            public SaveFileState(FileExplorer explorer) : base(explorer) { }

            private bool CalculateSubmitState()
            {
                return _explorer._addressInput.text != string.Empty && _explorer._nameInput.text != string.Empty;
            }

            public override void ShowContent(string path, bool updateHistory = true)
            {
                base.ShowContent(path, updateHistory);

                _explorer._submitButton.interactable = CalculateSubmitState();
            }

            public override void SetFooterState(string filename)
            {
                _explorer._nameInput.text = filename;

                _explorer.SetSubmitState(_explorer._saveFileSubmitText, CalculateSubmitState());
            }

            public override void Submit()
            {
                var path = Path.Combine(_explorer._addressInput.text, _explorer._nameInput.text);

                if (!Path.HasExtension(path))
                    path += $".{_explorer.CurrentFilter}";

                SetResultsAndSubmit(new string[] { path });
            }

            #region Event handlers
            public override void FilenameInput_OnEndEdit(string name)
            {
                _explorer.SelectElementByName(name);

                if (!string.IsNullOrEmpty(_explorer.CurrentPath))
                    _explorer._submitButton.interactable = name != string.Empty;
            }
            #endregion
        }
        #endregion

        #region References
        [Header("File Explorer")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Text _titleText;

        [SerializeField]
        private Button _previousButton;

        [SerializeField]
        private Button _nextButton;

        [SerializeField]
        private Button _parentButton;

        [SerializeField]
        private Button _refreshButton;

        [SerializeField]
        private InputField _addressInput;

        [SerializeField]
        private ScrollRect _scrollRect;

        [SerializeField]
        private Transform _content;

        [SerializeField]
        private Transform _bookmarks;

        [SerializeField]
        private InputField _nameInput;

        [SerializeField]
        private Dropdown _filterDropdown;

        [SerializeField]
        private Button _submitButton;

        [SerializeField]
        private Text _submitText;

        [SerializeField]
        private Button _cancelButton;

        [Header("Exception")]
        [SerializeField]
        private GameObject _exception;

        [SerializeField]
        private Text _exceptionMessageText;

        [SerializeField]
        private Button _exceptionCloseButton;

        [Header("Prefabs")]
        [SerializeField]
        private Element _drivePrefab;

        [SerializeField]
        private Element _directoryPrefab;

        [SerializeField]
        private Element _filePrefab;
        #endregion

        private string[] _filters;

        private Element _currentElement;

        private List<string> _pathsHistory = new List<string>();

        private int _currentPathHistoryIndex = -1;

        private Dictionary<State.Type, State> _states = new Dictionary<State.Type, State>();

        private State _currentState;

        private string[] _lastResults;

        [Header("Submit")]
        [SerializeField]
        private string _openFileSubmitText;

        [SerializeField]
        private string _saveFileSubmitText;

        [Header("Element")]
        [SerializeField]
        private Color _defaultColor = Color.white;

        [SerializeField]
        private Color _selectedColor = Color.blue;

        [Header("Events")]
        public SubmitedEvent Submited;

        public UnityEvent Canceled;

        #region Properties
        public bool IsBusy { get; private set; }

        private string CurrentPath { get => _pathsHistory[_currentPathHistoryIndex]; }

        private string[] Filters
        {
            get => _filters;
            set
            {
                _filters = value;

                var dropdownFilters = new List<string>(_filters);

                _filterDropdown.ClearOptions();

                for (int i = 0; i < dropdownFilters.Count; i++)
                    dropdownFilters[i] = dropdownFilters[i].Replace(" ", ", ");

                _filterDropdown.AddOptions(dropdownFilters);
            }
        }

        private string CurrentFilter { get => Filters[_filterDropdown.value]; }

        private string[] CurrentFlterExtensions { get; set; }

        public ReadOnlyCollection<string> LastResults { get => new ReadOnlyCollection<string>(_lastResults); }

        public string LastResult { get; private set; }

        public Coroutine Routine { get; private set; }
        #endregion

        #region Methods
        #region Initialization
        private void Awake()
        {
            SubscribeOnInnerEvents();
            CreateStatesPool();
        }

        private void SubscribeOnInnerEvents()
        {
            _previousButton.onClick.AddListener(ShowPreviousContent);
            _nextButton.onClick.AddListener(ShowNextContent);
            _parentButton.onClick.AddListener(ShowParentContent);
            _refreshButton.onClick.AddListener(RefreshContent);

            _addressInput.onEndEdit.AddListener(AddressInput_OnEndEdit);

            foreach (var bookmark in _bookmarks.GetComponentsInChildren<Bookmark>())
                bookmark.Clicked.AddListener(Bookmark_Clicked);

            _scrollRect.Clicked.AddListener(ScrollRect_Clicked);

            _filterDropdown.onValueChanged.AddListener(FilterDropdown_OnValueChanged);

            _nameInput.onEndEdit.AddListener(FilenameInput_OnEndEdit);

            _submitButton.onClick.AddListener(Submit);
            _cancelButton.onClick.AddListener(Cancel);

            _exceptionCloseButton.onClick.AddListener(HideException);
        }
        #endregion

        #region State
        private void CreateStatesPool()
        {
            _states.Add(State.Type.OpenFile, new OpenFileState(this));
            _states.Add(State.Type.SaveFile, new SaveFileState(this));
        }

        private void SetCurrentState(State.Type type) => _currentState = _states[type];
        #endregion

        public Coroutine OpenFile(string title = null, string path = null, string filters = null)
        {
            CheckAndCorrectAll(ref path, ref filters);

            ShowExplorer(State.Type.OpenFile, title, path, null);

            return Routine;
        }

        public Coroutine SaveFile(string title = null, string path = null, string filters = null, string filename = null)
        {
            CheckAndCorrectAll(ref path, ref filters, ref filename);

            ShowExplorer(State.Type.SaveFile, title, path, filename);

            return Routine;
        }

        private IEnumerator BusyRoutine()
        {
            while (IsBusy) yield return null;

            Routine = null;
        }

        #region Exploring
        private void ShowExplorer(State.Type type, string title, string path, string filename)
        {
            SetCurrentState(type);

            ClearHistory();

            IsBusy = true;
            SetCanvasGroupParameters(1f, true);

            _titleText.text = title;

            _currentState.ShowContent(path);
            _currentState.SetFooterState(filename);

            Routine = StartCoroutine(BusyRoutine());
        }

        private void StopExploring()
        {
            IsBusy = false;
            SetCanvasGroupParameters(0f, false);
        }

        private void SetCanvasGroupParameters(float alpha, bool state)
        {
            _canvasGroup.alpha = alpha;
            _canvasGroup.blocksRaycasts = _canvasGroup.interactable = state;
        }

        private void SetSubmitState(string name, bool state)
        {
            _submitText.text = name;
            _submitButton.interactable = state;
        }
        #endregion

        #region Checking
        private void CheckBusy()
        {
            if (IsBusy) throw new BusyException("File explorer already opened.");
        }

        private void CheckAndCorrectPath(ref string path)
        {
            if (path == null) path = string.Empty;

            if (path != string.Empty && !Directory.Exists(path)) throw new IOException("Directory not exist.");
        }

        private string[] CheckAndCorrectFilters(ref string filters)
        {
            if (filters == null) filters = "*";

            filters = filters.ToLower();

            var splittedFilters = filters.Split('|');

            for (int i = 0; i < splittedFilters.Length; i++)
            {
                splittedFilters[i] = splittedFilters[i].Trim();

                var extensions = new List<string>(splittedFilters[i].Split(' '));

                foreach (var ex in extensions)
                    if (ex == string.Empty) throw new FormatException("Extension can't be empty string");

                if (extensions.Count > 1 && extensions.Any(ex => ex == "*"))
                    throw new FormatException("Filter can not contain * and any extension together.");
            }

            for (int i = 0; i < splittedFilters.Length; i++)
            {
                var extensions = splittedFilters[i].Split(' ');

                for (int j = 0; j < extensions.Length - 1; j++)
                    for (int k = j + 1; k < extensions.Length; k++)
                        if (extensions[j] == extensions[k])
                            throw new FormatException("Filter can not contain same extensions.");
            }

            return splittedFilters;
        }

        private void CheckAndCorrectAll(ref string path, ref string filters)
        {
            CheckBusy();
            CheckAndCorrectPath(ref path);
            Filters = CheckAndCorrectFilters(ref filters);
            CalculateCurrentFilterExtensions();
        }

        private void CheckAndCorrectAll(ref string path, ref string filters, ref string filename)
        {
            CheckAndCorrectAll(ref path, ref filters);

            if (filename == null) filename = "Untitled";

            var invalidChars = Path.GetInvalidFileNameChars();

            if (filename.Any(c => invalidChars.Contains(c)))
                throw new FormatException("Filename contains invalid symbol");
        }
        #endregion

        #region Content
        private void ShowContent(string path, bool updateHistory = true)
        {
            ClearContent();

            try
            {
                if (path == string.Empty)
                    ShowLogicalDrivesContent();
                else
                    ShowDirectoryContent(path);

                if (updateHistory)
                    AddPathToHistory(path);

                _previousButton.interactable = _currentPathHistoryIndex != 0;
                _nextButton.interactable = _currentPathHistoryIndex != _pathsHistory.Count - 1;
                _parentButton.interactable = CurrentPath != string.Empty;

                _addressInput.text = path;
            }
            catch (Exception ex)
            {
                _currentState.ShowContent(CurrentPath, false);
                ShowException(ex.Message);
            }
        }

        private void ShowContentIfNotCurrent(string path)
        {
            if (CurrentPath != path) _currentState.ShowContent(path);
        }

        private void ShowLogicalDrivesContent()
        {
            var drivesInfo = DriveInfo.GetDrives();

            foreach (var driveInfo in drivesInfo)
            {
                var drive = Element.Factory.Create(_drivePrefab, _content, driveInfo.Name, $"{driveInfo.Name}");

                drive.Clicked.AddListener(DriveElement_Clicked);
                drive.DoubleClicked.AddListener(DriveElement_DoubleClicked);
            }
        }

        private void ShowDirectoryContent(string path)
        {
            if (!Directory.Exists(path)) throw new IOException("Directory not exist.");

            var directories = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);

            foreach (var directoryPath in directories)
            {
                var directory = Element.Factory.Create(_directoryPrefab, _content, directoryPath, Path.GetFileName(directoryPath));

                directory.Clicked.AddListener(DirectoryElement_Clicked);
                directory.DoubleClicked.AddListener(DirectoryElement_DoubleClicked);
            }

            foreach (var filePath in files)
            {
                var file = Element.Factory.Create(_filePrefab, _content, filePath, Path.GetFileName(filePath));

                file.Clicked.AddListener(FileElement_Clicked);
                file.DoubleClicked.AddListener(FileElement_DoubleClicked);
            }

            FilterFiles();
        }

        private void ShowPreviousContent()
        {
            if (_currentPathHistoryIndex == 0) return;

            _currentPathHistoryIndex -= 1;

            _currentState.ShowContent(CurrentPath, false);
        }

        private void ShowNextContent()
        {
            if (_currentPathHistoryIndex == _pathsHistory.Count - 1) return;

            _currentPathHistoryIndex += 1;

            _currentState.ShowContent(CurrentPath, false);
        }

        private void ShowParentContent()
        {
            if (CurrentPath == string.Empty) return;

            _currentState.ShowContent(Path.GetDirectoryName(CurrentPath) ?? string.Empty);
        }

        private void RefreshContent() => _currentState.ShowContent(CurrentPath, false);

        private void FilterFiles()
        {
            var files = _content.GetComponentsInChildren<Element>(true).Where(el => el.ElementType == Element.Type.File);

            foreach (var file in files) Filter(file);
        }

        private void Filter(Element element)
        {
            element.gameObject.SetActive(IsAllowedExtension(element));
        }

        private bool IsAllowedExtension(Element element)
        {
            return CurrentFilter == "*" || CurrentFlterExtensions.Any(ex => element.Extension == ex);
        }

        private void CalculateCurrentFilterExtensions()
        {
            CurrentFlterExtensions = Filters[_filterDropdown.value].Split(' ');
        }

        private void ClearContent()
        {
            _currentState.DeselectCurrentElement();

            foreach (Transform child in _content)
                Destroy(child.gameObject);
        }
        #endregion

        #region History
        private void AddPathToHistory(string path)
        {
            for (int i = _pathsHistory.Count - 1; i > _currentPathHistoryIndex; i--)
                _pathsHistory.RemoveAt(i);

            _pathsHistory.Add(path);
            _currentPathHistoryIndex += 1;
        }

        private void ClearHistory()
        {
            _pathsHistory.Clear();
            _currentPathHistoryIndex = -1;
        }
        #endregion

        #region Selecting
        private void SelectElement(Element element)
        {
            _currentElement = element;
            _currentElement.Color = _selectedColor;

            if (_currentElement.ElementType == Element.Type.File)
            {
                _nameInput.text = element.Name;
                _submitButton.interactable = true;
            }
        }

        private void SelectOnlyThisElement(Element element)
        {
            _currentState.DeselectCurrentElement();
            SelectElement(element);
        }

        private void SelectElementByName(string name)
        {
            _currentState.DeselectCurrentElement();

            var element = _content.GetComponentsInChildren<Element>()
                .ToList()
                .Find(el => el.ElementType == Element.Type.File && el.Name.ToLower() == name.ToLower());


            if (element) SelectElement(element);
        }

        private void DeselectCurrentElement()
        {
            if (!_currentElement) return;

            _currentElement.Color = _defaultColor;
            _currentElement = null;
        }
        #endregion

        #region Exception
        private void ShowException(string message)
        {
            _exception.SetActive(true);
            _exceptionMessageText.text = message;
        }

        private void HideException() => _exception.SetActive(false);
        #endregion

        #region Decision
        private void Submit()
        {
            StopExploring();

            _currentState.Submit();
        }

        private void Cancel()
        {
            StopExploring();

            ResetLastResults();

            Canceled.Invoke();
        }

        private void ResetLastResults()
        {
            LastResult = null;
            _lastResults = new string[0];
        }
        #endregion

        #region Event handlers
        private void AddressInput_OnEndEdit(string path) => _currentState.ShowContent(path);

        private void ScrollRect_Clicked() { _currentState.DeselectCurrentElement(); }

        #region Elements
        private void DriveElement_Clicked(Element drive) => SelectOnlyThisElement(drive);

        private void DriveElement_DoubleClicked(Element drive) => _currentState.ShowContent(drive.Path);

        private void DirectoryElement_Clicked(Element directory) => SelectOnlyThisElement(directory);

        private void DirectoryElement_DoubleClicked(Element directory) => _currentState.ShowContent(directory.Path);

        private void FileElement_Clicked(Element directory) => SelectOnlyThisElement(directory);

        private void FileElement_DoubleClicked(Element directory) => Submit();
        #endregion

        private void Bookmark_Clicked(Bookmark bookmark)
        {
            ShowContentIfNotCurrent(Environment.GetFolderPath(bookmark.SpecialFolder));
        }

        private void FilterDropdown_OnValueChanged(int value)
        {
            CalculateCurrentFilterExtensions();

            if (_currentElement && !IsAllowedExtension(_currentElement))
                _currentState.DeselectCurrentElement();

            FilterFiles();
        }

        private void FilenameInput_OnEndEdit(string name) => _currentState.FilenameInput_OnEndEdit(name);
        #endregion
        #endregion
    }
}