using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;

namespace NCMConverter
{


    /// <summary>
    /// NCM转换器主窗口类
    /// 负责处理用户界面交互和文件拖放功能
    /// </summary>
    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();
            SetupLogAutoScroll();
        }

        private void SetupLogAutoScroll()
        {
            // 订阅日志文本框的文本变化事件，实现自动滚动到最新日志
            LogTextBox.TextChanged += (sender, e) =>
            {
                // 自动滚动到文本框末尾，确保最新日志可见
                LogTextBox.ScrollToEnd();
            };
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            // 检查拖放的数据是否为文件类型
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 获取拖放的文件路径数组
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                // 获取主视图模型实例以调用添加文件方法
                var viewModel = (MainViewModel)DataContext;
                
                // 遍历所有拖放的项目
                foreach (string file in files)
                {
                    // 判断是否为目录
                    if (Directory.Exists(file))
                    {
                        // 添加目录中的所有.ncm文件到转换队列
                        viewModel.AddFilesFromDirectory(file);
                    }
                    // 判断是否为.ncm文件
                    else if (File.Exists(file) && Path.GetExtension(file).ToLower() == ".ncm")
                    {
                        // 将.ncm文件添加到转换队列
                        viewModel.AddFile(file);
                    }
                }
            }
        }

        // 其他UI事件由ViewModel处理，此处保持空实现
    }
}