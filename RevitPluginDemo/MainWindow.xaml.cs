using System.Windows;

namespace RevitPlugin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // 按钮点击事件处理
        private void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            string userInput = CommandInput.Text;
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                MessageBox.Show($"用户输入: {userInput}");
            }
            else
            {
                MessageBox.Show("请输入有效的指令");
            }
        }
    }
}
