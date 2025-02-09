using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SFB;
using System;

namespace FileRenamer
{
    public class FileRenamerLogic : IDisposable
    {
        #region Fields 

        private List<string> _inputFilePaths;
        private Dictionary<string, string> _processedFiles;

        private string _exportFolderPath = ""; // Path to export folder
        private FileRenamerSettings _settings;

        private bool _processed = false;
        private string _resultMsg = "";
        private string _errorMsg = "";

        #endregion


        #region Properties 

        public bool HasInputFiles => _inputFilePaths != null && _inputFilePaths.Count > 0;
        public FileRenamerSettings Settings => _settings;
        public List<string> InputFilePaths => _inputFilePaths;
        public Dictionary<string, string> ProcessedFiles => _processedFiles;

        public bool Processed => _processed;
        public string LastResultMsg => _resultMsg;
        public string LastErrorMsg => _errorMsg;

        #endregion


        #region Methods

        #region Init

        public FileRenamerLogic(FileRenamerSettings settings)
        {
            _settings = settings;
            _inputFilePaths = new List<string>();
            _processedFiles = new Dictionary<string, string>();

            Settings.OnNamingSettingsUpdated += UnsetProcessedStatus;
        }

        public void Dispose()
        {
            Settings.OnNamingSettingsUpdated -= UnsetProcessedStatus;
        }

        #endregion

        public void RequestFiles()
        {
            _inputFilePaths = StandaloneFileBrowser
                .OpenFilePanel("Select Files", "", FileRenamerSettings.SupportedFileExtensions, true)
                .ToList();
            UnsetProcessedStatus();
        }

        public void RemoveInputFilePath(string targetFilePath)
        {
            if (_inputFilePaths.Remove(targetFilePath))
            {
                UnsetProcessedStatus();
            }
        }

        public string RequestFile()
        {
            string[] filePath = StandaloneFileBrowser.OpenFilePanel("Select Template File", "", FileRenamerSettings.SupportedFileExtensions, false);
            return filePath.Length > 0 ? Path.GetFileNameWithoutExtension(filePath[0]) : null;
        }

        public string RequestFolder()
        {
            return EditorUtility.OpenFolderPanel("Select Export Folder", "", "");
        }

        public void ProcessFiles()
        {
            if(Processed)
            {
                return;
            }

            _processedFiles.Clear();
            List<string> files = _inputFilePaths?.Where(file => File.Exists(file)).ToList();

            if (files == null || files.Count == 0)
            {
                _inputFilePaths = null;
                _errorMsg = "# Not processed : Files do not exist!";
                return;
            }

            FileRenamerUtilities.SortFiles(files, Settings.SortAscending);

            int fileNumberingIndex = 0;

            // Rename and export files

            for (int currentFileIndex = 0; currentFileIndex < files.Count; currentFileIndex++)
            {
                string originalFilePath = files[currentFileIndex];
                string newFileName = Settings.FileNameTemplate;
                bool hasOwnNumbering = false;

                // Extract existing numbering if the option is enabled

                if (Settings.PreserveExistingNumbering)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
                    string existingNumbering = FileRenamerUtilities.ExtractNumbering(fileNameWithoutExtension);

                    if (!string.IsNullOrEmpty(existingNumbering))
                    {
                        newFileName += $"_{existingNumbering}";
                        hasOwnNumbering = true;
                    }
                }

                // Add numbering if the option is enabled

                if (Settings.AddNumbering && !hasOwnNumbering)
                {
                    newFileName += $"_{fileNumberingIndex + 1:00}";
                    ++fileNumberingIndex;
                }

                string extension = Path.GetExtension(originalFilePath);
                newFileName += extension;

                _processedFiles.Add(originalFilePath, newFileName);
            }

            _resultMsg = $"Processed {files.Count} files";
            _processed = true;
        }

        private void UnsetProcessedStatus()
        {
            _processed = false;
        }

        public void TryExportFiles()
        {
            if (_processedFiles.Count == 0)
            {
                _errorMsg = "# NOT EXPORTED : No files selected!";
                return;
            }

            // Open folder dialog to select export folder using StandaloneFileBrowser
            _exportFolderPath = RequestFolder();
            _exportFolderPath = TryCreateSubfolder(_settings.FileNameTemplate, _settings.CreateSubFolder);

            if (string.IsNullOrEmpty(_exportFolderPath))
            {
                _errorMsg = "# NOT EXPORTED : Incorrect export path!";
                return;
            }

            ExportFiles();
            TryOpenExportFolder();

            Debug.Log($"Exported {_processedFiles.Count} files to {_exportFolderPath}");
            _resultMsg = $"Exported {_processedFiles.Count} files to {_exportFolderPath}";
        }

        private void TryOpenExportFolder()
        {
            if (_settings.OpenExportFolder)
            {
                FileRenamerUtilities.OpenFolder(Path.GetFullPath(_exportFolderPath));
            }
        }

        private void ExportFiles()
        {
            foreach (var entry in _processedFiles)
            {
                string originalPath = entry.Key;
                string newFileName = entry.Value;
                string newFilePath = Path.Combine(_exportFolderPath, newFileName);

                File.Copy(originalPath, newFilePath, overwrite: Settings.OverwriteFiles);
            }
        }

        private string TryCreateSubfolder(string fileNameTemplate, bool createSubfolder)
        {
            if (createSubfolder)
            {
                return FileRenamerUtilities.CreateFolder(_exportFolderPath, fileNameTemplate);
            }
            return _exportFolderPath;
        }

        #endregion
    }
}