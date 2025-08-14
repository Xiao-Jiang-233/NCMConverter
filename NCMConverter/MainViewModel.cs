using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace NCMConverter
{
    public class FileItem : INotifyPropertyChanged
    {
        private string? status;
        public string Status
        {
            get => status ?? "";
            set { status = value; OnPropertyChanged(); }
        }
        public string FilePath { get; set; } = "";
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FileItem> FileList { get; set; } = new ObservableCollection<FileItem>();
        private FileItem? selectedFile;
        public FileItem? SelectedFile
        {
            get => selectedFile;
            set { selectedFile = value; OnPropertyChanged(); }
        }
        private bool isSaveToSourceFolder = true;
        public bool IsSaveToSourceFolder
        {
            get => isSaveToSourceFolder;
            set 
            { 
                isSaveToSourceFolder = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSaveToCustomFolder));
                if (value)
                {
                    CustomSavePath = string.Empty;
                }
            }
        }

        public bool IsSaveToCustomFolder
        {
            get => !isSaveToSourceFolder;
            set { IsSaveToSourceFolder = !value; }
        }

        private string customSavePath = string.Empty;
        public string CustomSavePath
        {
            get => customSavePath;
            set 
            { 
                customSavePath = value; 
                OnPropertyChanged(); 
            }
        }

        public ICommand AddFileCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand ClearListCommand { get; }
        public ICommand StartProcessCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand BrowseFolderCommand { get; }

        private string updateLog = string.Empty;
        public string UpdateLog
        {
            get => updateLog;
            set { updateLog = value; OnPropertyChanged(); }
        }


        public MainViewModel()
        {
            AddFileCommand = new RelayCommand(_ => AddFile());
            AddFolderCommand = new RelayCommand(_ => AddFolder());
            ClearListCommand = new RelayCommand(_ => FileList.Clear());
            StartProcessCommand = new RelayCommand(_ => StartProcess());
            RemoveFileCommand = new RelayCommand(item => RemoveFile(item as FileItem));
            BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
            _ = CheckAndUpdateLibAsync();
        }

        private void BrowseFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择保存目录",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CustomSavePath = dialog.SelectedPath;
            }
        }

        private async System.Threading.Tasks.Task CheckAndUpdateLibAsync()
        {
            UpdateLog = "正在检查libncmdump核心库...\n";
            bool ok = await LibDownloader.CheckAndUpdateLibraryAsync(msg => UpdateLog += msg + "\n");
            if (ok)
                UpdateLog += "libncmdump核心库可用。\n";
            else
                UpdateLog += "libncmdump核心库不可用，请检查网络或手动下载。\n";
        }

        private void AddFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "NCM 文件 (*.ncm)|*.ncm|所有文件 (*.*)|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    if (!FileListExists(file))
                        FileList.Add(new FileItem { Status = "等待", FilePath = file });
                }
            }
        }

        // 公共方法，用于添加单个文件（供拖放功能使用）
        public void AddFile(string filePath)
        {
            if (!FileListExists(filePath))
                FileList.Add(new FileItem { Status = "等待", FilePath = filePath });
        }

        private void AddFolder()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var files = System.IO.Directory.GetFiles(dlg.SelectedPath, "*.ncm", System.IO.SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (!FileListExists(file))
                        FileList.Add(new FileItem { Status = "等待", FilePath = file });
                }
            }
        }

        // 公共方法，用于添加目录中的所有.ncm文件（供拖放功能使用）
        public void AddFilesFromDirectory(string directoryPath)
        {
            var files = System.IO.Directory.GetFiles(directoryPath, "*.ncm", System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (!FileListExists(file))
                    FileList.Add(new FileItem { Status = "等待", FilePath = file });
            }
        }

        private bool FileListExists(string filePath)
        {
            foreach (var item in FileList)
                if (item.FilePath == filePath) return true;
            return false;
        }

        private async void StartProcess()
        {
            if (FileList.Count == 0)
            {
                UpdateLog += "请先添加需要转换的文件。\n";
                return;
            }

            string? saveDir = IsSaveToSourceFolder ? null : await SelectCustomSaveDir();
            if (!IsSaveToSourceFolder && string.IsNullOrEmpty(saveDir))
            {
                return;
            }

            for (int i = 0; i < FileList.Count; i++)
            {
                var item = FileList[i];
                item.Status = "处理中";
                OnPropertyChanged(nameof(FileList));

                int currentProgress = (int)(((double)(i + 1) / FileList.Count) * 100);
                UpdateLog += $"正在处理 {i + 1}/{FileList.Count} ({currentProgress}%): {System.IO.Path.GetFileName(item.FilePath)}\n";

                string outDir = IsSaveToSourceFolder ? System.IO.Path.GetDirectoryName(item.FilePath) ?? "" : saveDir ?? "";
                bool ok = await NcmConvertService.ConvertNcmAsync(item.FilePath, outDir, msg => UpdateLog += msg + "\n");
                item.Status = ok ? "完成" : "失败";
                OnPropertyChanged(nameof(FileList));
            }

            UpdateLog += $"全部处理完成。\n";
        }

        private Task<string?> SelectCustomSaveDir()
        {
            var tcs = new TaskCompletionSource<string?>();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                using var dlg = new System.Windows.Forms.FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    tcs.SetResult(dlg.SelectedPath);
                else
                    tcs.SetResult(null);
            });
            return tcs.Task;
        }
        private void RemoveFile(FileItem? item) { if (item != null) FileList.Remove(item); }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => canExecute == null || canExecute(parameter);
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }
}
