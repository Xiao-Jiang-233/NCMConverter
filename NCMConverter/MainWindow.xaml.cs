using System.Windows;
using System.Windows.Controls;

namespace NCMConverter
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
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
            // 监听TextBox文本变化事件
            LogTextBox.TextChanged += (sender, e) =>
            {
                // 在UI线程中执行滚动操作
                LogTextBox.ScrollToEnd();
            };
        }

        // 事件全部交由ViewModel处理，保留空壳
    }
}