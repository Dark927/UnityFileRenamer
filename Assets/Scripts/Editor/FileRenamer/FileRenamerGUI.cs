using FileRenamer.Styles;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FileRenamer
{
    public class FileRenamerGUI : EditorWindow
    {
        #region Fields 

        private FileRenamerLogic _fileRenamer;
        private string _errorMsg = "";
        private string _resultMsg = "";


        private bool _showInputFilesList = false;   // Display input files names.
        private bool _previewResultFilesList = false; // Display processed files names.
        private Vector2 _scrollPos = Vector2.zero;

        #endregion


        #region Methods

        #region Init

        [MenuItem("Tools/File Renamer")]
        public static void ShowWindow()
        {
            FileRenamerGUI window = GetWindow<FileRenamerGUI>("File Renamer");
            window.minSize = FileRenamerStyleGUI.WindowMinSize;
        }

        private void Awake()
        {
            _fileRenamer = new FileRenamerLogic(new FileRenamerSettings());
        }

        private void OnDisable()
        {
            _fileRenamer?.Dispose();
        }

        #endregion

        private void OnGUI()
        {
            DisplayHeader();

            DisplayFilesSelection();
            DisplaySelectedFilesStatus();
            FileRenamerStyleGUI.DrawUILine(Color.black);
            EditorGUILayout.Space();


            DisplayNameTemplateSettings();
            DisplayToogles();

            ExportFiles();

            DisplayErrorMsg();
            DisplayResultMsg();
        }

        private static void DisplayHeader()
        {
            EditorGUILayout.Space();
            FileRenamerStyleGUI.DrawUILine(Color.black);
            GUILayout.Label("File Renamer - made by Dark", EditorStyles.boldLabel);
            FileRenamerStyleGUI.DrawUILine(Color.black);
            EditorGUILayout.Space();
        }

        private void DisplayFilesSelection()
        {
            GUILayout.Label("Files Selection [ Input ]", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Select Files"))
            {
                _fileRenamer.RequestFiles();
                ClearMessages();
            }
        }

        private void DisplaySelectedFilesStatus()
        {
            if(_fileRenamer == null)
            {
                Close();
                return;
            }

            if (!_fileRenamer.HasInputFiles)
            {
                GUILayout.Label($"No files selected.", FileRenamerStyleGUI.YellowLabel);
                return;
            }

            GUILayout.Label($"Selected Files: {_fileRenamer.InputFilePaths.Count}", FileRenamerStyleGUI.GreenLabel);

            // Toggle to show/hide the file list
            DisplayToggle("Show File List", ref _showInputFilesList);
            TryDisplayInputFilesList();
        }

        private void TryDisplayInputFilesList()
        {
            if (!_showInputFilesList)
            {
                return;
            }

            EditorGUILayout.Space();

            // Begin a scroll view for long lists
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));

            for (int currentFileIndex = 0; currentFileIndex < _fileRenamer.InputFilePaths.Count; currentFileIndex++)
            {
                string currentFile = _fileRenamer.InputFilePaths[currentFileIndex];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(Path.GetFileName(currentFile), GUILayout.ExpandWidth(true));


                // Button to remove the file
                if (GUILayout.Button("rm", GUILayout.Width(25)))
                {
                    _fileRenamer.RemoveInputFilePath(currentFile);
                    EditorGUILayout.EndHorizontal();

                    // Stop iteration to prevent state corruption
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Ensure the scroll always ends properly
            EditorGUILayout.EndScrollView();

        }

        private void DisplayNameTemplateSettings()
        {
            GUILayout.Label("File Name Template [ Settings ]", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _fileRenamer.Settings.FileNameTemplate = EditorGUILayout.TextField("File Name Template", _fileRenamer.Settings.FileNameTemplate);
            EditorGUILayout.Space();

            if (GUILayout.Button("Select template from file", FileRenamerStyleGUI.ButtonsLayouts))
            {
                _fileRenamer.Settings.FileNameTemplate = _fileRenamer.RequestFile() ?? _fileRenamer.Settings.FileNameTemplate;
            }

            TryPreviewResult();
        }

        private void TryPreviewResult()
        {
            if (!_fileRenamer.HasInputFiles)
            {
                return;
            }

            DisplayToggle("Preview Result", ref _previewResultFilesList);

            if (!_previewResultFilesList)
            {
                return;
            }

            _fileRenamer.ProcessFiles();
            PreviewResult();
        }

        private void PreviewResult()
        {
            GUILayout.Label("Preview:", EditorStyles.boldLabel);

            // Scroll view for displaying old -> new file names
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));

            foreach (var entry in _fileRenamer.ProcessedFiles)
            {
                string oldFileName = Path.GetFileName(entry.Key);
                string newFileName = entry.Value;

                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(oldFileName, FileRenamerStyleGUI.YellowLabel, GUILayout.Width(200), GUILayout.ExpandWidth(true));
                GUILayout.Label(" → ", GUILayout.Width(30));
                GUILayout.Label(newFileName, FileRenamerStyleGUI.GreenLabel, GUILayout.Width(200), GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DisplayToogles()
        {
            EditorGUILayout.Space();

            DisplayToggle("Can Overwrite Files",
                () => _fileRenamer.Settings.OverwriteFiles,
                value => _fileRenamer.Settings.OverwriteFiles = value,
                FileRenamerStyleGUI.ToogleLabelLayouts);

            DisplayToggle("Sort Ascending",
                () => _fileRenamer.Settings.SortAscending,
                value => _fileRenamer.Settings.SortAscending = value,
                FileRenamerStyleGUI.ToogleLabelLayouts);

            DisplayToggle("Add Numbering",
                () => _fileRenamer.Settings.AddNumbering,
                value => _fileRenamer.Settings.AddNumbering = value,
                FileRenamerStyleGUI.ToogleLabelLayouts);

            DisplayToggle("Preserve Existing Numbering",
                () => _fileRenamer.Settings.PreserveExistingNumbering,
                value => _fileRenamer.Settings.PreserveExistingNumbering = value,
                FileRenamerStyleGUI.ToogleLabelLayouts);

            DisplayToggle("Create Subfolder",
                () => _fileRenamer.Settings.CreateSubFolder,
                value => _fileRenamer.Settings.CreateSubFolder = value,
                FileRenamerStyleGUI.ToogleLabelLayouts);

            DisplayToggle("Open Export Folder",
                () => _fileRenamer.Settings.OpenExportFolder,
                value => _fileRenamer.Settings.OpenExportFolder = value,
                FileRenamerStyleGUI.ToogleLabelLayouts);

            EditorGUILayout.Space();
        }

        private void DisplayToggle(string label, ref bool relatedParameter, params GUILayoutOption[] labelOptions)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(label, labelOptions);
            relatedParameter = EditorGUILayout.Toggle(relatedParameter);

            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }


        private void DisplayToggle(string label, Func<bool> getter, Action<bool> setter, params GUILayoutOption[] labelOptions)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(label, labelOptions);
            bool newValue = EditorGUILayout.Toggle(getter());
            setter(newValue); 

            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void ExportFiles()
        {
            if (GUILayout.Button("Process Files"))
            {
                _fileRenamer.ProcessFiles();
                _fileRenamer.TryExportFiles();
                UpdateMessages(_fileRenamer);
            }
        }

        private void UpdateMessages(FileRenamerLogic fileRenamer)
        {
            _resultMsg = fileRenamer.LastResultMsg;
            _errorMsg = fileRenamer.LastErrorMsg;
        }

        private void DisplayResultMsg()
        {
            if (!string.IsNullOrEmpty(_resultMsg))
            {
                _errorMsg = "";
                GUILayout.Label(_resultMsg, FileRenamerStyleGUI.GreenLabel);
            }
        }

        private void DisplayErrorMsg()
        {
            if (!string.IsNullOrEmpty(_errorMsg))
            {
                GUILayout.Label(_errorMsg, FileRenamerStyleGUI.RedLabel);
            }
        }

        private void ClearMessages()
        {
            _errorMsg = string.Empty;
            _resultMsg = string.Empty;
        }

        #endregion
    }
}