using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;

namespace Remote_Shutdown
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        static string LogFilePath;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            //read previous logs

            LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.txt");

            // Check if the file exists
            if (!File.Exists(LogFilePath))
            {
                // Create the file and write an initial message
                File.WriteAllText(LogFilePath, "New console");
            }
            else
            {
                // Read the contents of the file
                string content = File.ReadAllText(LogFilePath);
                NCL(content, false);
            }
        }
        public static void NCL(string _txt, bool AddToLogFile=true) //New console log
        {
            string txt = DateTime.Now + " " + _txt + ";\n";
            if (AddToLogFile) { File.AppendAllText(LogFilePath, txt); } //add text to file
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow.Instance.ConsoleBox.AppendText(txt); //add text to console
                MainWindow.Instance.ConsoleBox.ScrollToEnd();
            });
        }


        private void OnOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnOffButton.Content.ToString() == "On") //power of
            {
                OnOffButton.Content = "Off";
                ServerClass.StopServer();
            } else //power on
            {
                if (ServerClass.StartServer()) //if success
                {
                    OnOffButton.Content = "On";
                }
            }

        }
    }
}