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

        //Global data vars
        static string gimbal;
        static string up_lidar;

        
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
                   // Console.WriteLine(data[1]);
                    TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                    int ctime = (int)t.TotalSeconds;

                    MainWindow.main.Status  = String.Format("Gimbal Pitch:{0}, Up Lidar: {1}", data[0], data[1]);

                    gimbal = data[0];
                    up_lidar = data[1];

                  //  using (StreamWriter sw = File.AppendText(path))
                  //  {
                  //      sw.WriteLine("{0},{1},{2}",t,data[0],data[1]);
                  //  }

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
            Thread h = new Thread(parseHokuyuData);
            h.Start();

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
      

        private void parseHokuyuData()
        {
           // const int GET_NUM = 10;
            const int start_step = 0;
            const int end_step = 760;
            try
            {
          
                SerialPort urg = new SerialPort(hPort, 115200);
                urg.NewLine = "\n\n";

                urg.Open();

                urg.Write(SCIP_Writer.SCIP2());
                urg.ReadLine(); // ignore echo back
                urg.Write(SCIP_Writer.MD(start_step, end_step));
                urg.ReadLine(); // ignore echo back

                List<long> distances = new List<long>();
                long time_stamp = 0;
                //for (int i = 0; i < GET_NUM; ++i)
                while(_continue)
                {
                    string receive_data = urg.ReadLine();
                    if (!SCIP_Reader.MD(receive_data, ref time_stamp, ref distances))
                    {
                        Console.WriteLine(receive_data);
                        break;
                    }
                    if (distances.Count == 0)
                    {
                        Console.WriteLine(receive_data);
                        continue;
                    }
                    // show distance data
                    //Console.WriteLine(time_stamp.ToString() +"," + gimbal + "," + up_lidar+","+parse_distance(distances));

                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(time_stamp.ToString() + "," + gimbal + "," + up_lidar + "," + parse_distance(distances));
                    }
                }

                urg.Write(SCIP_Writer.QT()); // stop measurement mode
                urg.ReadLine(); // ignore echo back
                urg.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
        }

        private string parse_distance(List<long> distances)
        {
            string result = "";
            result = string.Join(":", distances);
            return result;
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
