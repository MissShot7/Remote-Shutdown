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
using System.ComponentModel;
using System.Windows.Forms;
using Application = System.Windows.Application;


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
            CreateNotifyIcon(); //system tray
            this.Hide(); //starts hidden
            ServerToggle(); //start server
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

        private void ServerToggle()
        {
            if (OnOffButton.Content.ToString() == "On") //power of
            {
                OnOffButton.Content = "Off";
                ServerClass.StopServer();
            }
            else //power on
            {
                if (ServerClass.StartServer()) //if success
                {
                    OnOffButton.Content = "On";
                }
            }
        }
        private void OnOffButton_Click(object sender, RoutedEventArgs e)
        {
            ServerToggle();

        }

        //tray
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        private WindowState m_storedWindowState = WindowState.Normal;

        private void CreateNotifyIcon()
        {
            // Initialize NotifyIcon
            m_notifyIcon = new System.Windows.Forms.NotifyIcon();
            m_notifyIcon.BalloonTipText = "Remote Shutdown has been minimised. Click the tray icon to show.";
            m_notifyIcon.BalloonTipTitle = "Remote Shutdown";
            m_notifyIcon.Text = "Remote Shutdown";
            m_notifyIcon.Icon = new System.Drawing.Icon("trayIcon.ico");

            // Create a context menu for the tray icon
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            var openItem = new System.Windows.Forms.ToolStripMenuItem("Open", null, OpenItem_Click);
            var closeItem = new System.Windows.Forms.ToolStripMenuItem("Close", null, CloseItem_Click);
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(closeItem);
            m_notifyIcon.ContextMenuStrip = contextMenu;
            

            m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);
            m_notifyIcon.Visible = true; // Make sure the icon is visible
        }

        private void OpenItem_Click(object sender, EventArgs e)
        {
            // Restore the app when 'Open' is clicked
            Show();
            WindowState = m_storedWindowState;
        }

        private void CloseItem_Click(object sender, EventArgs e)
        {
            // Close the app when 'Close' is clicked from the tray menu
            CloseApp();
        }

        private void m_notifyIcon_Click(object sender, EventArgs e)
        {
            // Check for left mouse button click
            if (e is System.Windows.Forms.MouseEventArgs mouseEventArgs && mouseEventArgs.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // Show the app when the tray icon is clicked with the left mouse button
                Show();
                WindowState = m_storedWindowState;
            }
        }

        private void CloseApp()
        {
            // Close the app
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
            Application.Current.Shutdown();
            ServerClass.StopServer();
        }

        // Override OnClosing event to prevent closing the app, hide instead
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (e.Cancel == false) // Allow the user to close the app by choosing "Close" from the context menu
            {
                e.Cancel = true; // Prevent the app from closing
                Hide(); // Hide the window instead
            }
        }

        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide(); // Hide the window when minimized
            }
            else
            {
                m_storedWindowState = WindowState;
            }
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }

        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }

    }
}