using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Management;
using SCIP_library;





namespace ApmLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static bool _continue;
        static SerialPort _serialPort;
        static string apmPort;
        static string hPort;
        static string path = @"apm_log.csv";
        
        public MainWindow()
        {
            InitializeComponent();
            refreshComPorts();
            main = this;

        }

        internal static MainWindow main;
        internal string Status
        {
            get { return statusText.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { statusText.Content = value; })); }
        }


        public static void Read()
        {


            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    IList<string> data = message.Split(',');
                    Console.WriteLine(data[1]);
                    TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                    int ctime = (int)t.TotalSeconds;

                    MainWindow.main.Status  = String.Format("Gimbal Pitch:{0}, Up Lidar: {1}", data[0], data[1]);

                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine("{0},{1},{2}",t,data[0],data[1]);


                   
                    }

                }
                catch (TimeoutException) { }
            }
        }

       

      

       
       

       

       

        private void button_Click(object sender, RoutedEventArgs e)
        {
            _continue = true;
        using (StreamWriter sw = File.CreateText(path))
        {}
            Thread t = new Thread(parseSerialData);
            t.Start();
            startBtn.IsEnabled = false;
        }


        private void parseSerialData()
        {
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            _serialPort = new SerialPort();

            // Allow the user to set the appropriate properties.
            _serialPort.PortName = apmPort;
            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            _serialPort.Open();
            _continue = true;
            readThread.Start();

         
            readThread.Join();
            _serialPort.Close();

        }
    
        

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            _continue = false;
            System.Diagnostics.Process.Start("notepad.exe", path);
            startBtn.IsEnabled = true;
            MainWindow.main.Status = "Click Start to log APM Data";
        }

        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            refreshComPorts();
        }

        public void refreshComPorts()
        {
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();
                    string deviceId = item["DeviceID"].ToString();

                    Console.WriteLine(deviceId);

                    if (desc.Contains("Arduino"))
                    {
                        // return deviceId;
                        Console.WriteLine("Arduino");
                        apmPort = deviceId;
                        apmComLabel.Content = deviceId;
                    }

                    if (desc.Contains("URG"))
                    {
                        // return deviceId;
                        Console.WriteLine("URG");
                        hPort = deviceId;
                        hokuyuComLabel.Content = deviceId;

                    }
                }
            }
            catch (ManagementException e)
            {
                /* Do Nothing */
                statusText.Content = "Please plug in the Hokuyu and APM";
                startBtn.IsEnabled = false;
                stopBtn.IsEnabled = false;
            }
        }
    }
}
