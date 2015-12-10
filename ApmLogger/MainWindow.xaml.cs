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
using System.Windows.Threading;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Management;
using SCIP_library;
using System.Timers;






namespace ApmLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static bool _continue;
        static bool _visualize;
        static SerialPort _serialPort;
        static string apmPort;
        static string hPort;
        static string path = @"apm_log.csv";
        const int scan_width = 1080;

        //Global data vars
        static string gimbal;
        static string up_lidar;
        static string down_lidar;
        static string filesize = "0 MB";
        static List<long> last_distances;

        static int zoom_max = 50;
        static int zoom_increment = 5;
        static int current_zoom = 25;
      
        
        public MainWindow()
        {
            InitializeComponent();
            refreshComPorts();
            main = this;

            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 1080;
            aTimer.Enabled = true;


            _visualize = false;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
           if (_visualize)
            {
                if (last_distances != null)
                {
                    Dispatcher.Invoke(new Action(() => { h_visualize(new List<long>(last_distances)); })); 
                    
                }
            }

        }

        private void h_visualize(List<long> d)
        {
            //var allLines = System.IO.File.ReadAllLines(@"sample.txt").ToList();

            Dispatcher.Invoke(new Action(() => { canvas.Children.Clear(); }));

           
            float angle = -45;
            float increment = 1 / 4.0f;// 4.22f;
            foreach (long s in d)
            {
                AddLineWithLengthAndAngle(angle, s);
                angle = angle + increment;
            }
        }

        private void AddLineWithLengthAndAngle(float a, float length)
        {
            double angle = (Math.PI / -180.0) * a;
            double x1 = 250;
            double y1 = 250;
            double x2 = x1 + (Math.Cos(angle) * length/current_zoom);
            double y2 = y1 + (Math.Sin(angle) * length/current_zoom);
            int intX1 = Convert.ToInt32(x1);
            int intY1 = Convert.ToInt32(y1);
            int intX2 = Convert.ToInt32(x2);
            int intY2 = Convert.ToInt32(y2);
            
            Line line = new Line();
            line.Visibility = System.Windows.Visibility.Visible;
            line.StrokeThickness = 1;
            line.Stroke = System.Windows.Media.Brushes.Red;
            line.X1 = intX1;
            line.X2 = intX2;
            line.Y1 = intY1;
            line.Y2 = intY2;

            //Console.WriteLine(angle);

            //canvas.Children.Add(line);


            Dispatcher.Invoke(new Action(() => { canvas.Children.Add(line); }));

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
                    //Console.WriteLine(message);
                    IList<string> data = message.Split(',');
                   // Console.WriteLine(data[1]);
                    TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                    int ctime = (int)t.TotalSeconds;

                    try
                    {
                        MainWindow.main.Status = String.Format("size:{3} Pitch:{0} Lidar:{1}/{2}", data[0], data[1], data[2], filesize);
                        gimbal = data[0];
                        up_lidar = data[1];
                        down_lidar = data[2];
                    }
                    catch {
                        MainWindow.main.Status = String.Format("Date Error");
                    }
                   

                  

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
            //parseHokuyuData();
            startBtn.IsEnabled = false;
            _visualize = true;
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
            const int end_step = scan_width;
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
                        //Console.WriteLine(receive_data);
                        break;
                    }
                    if (distances.Count == 0)
                    {
                        //Console.WriteLine(receive_data);
                        continue;
                    }
                    // show distance data
                    //Console.WriteLine(time_stamp.ToString() +"," + gimbal + "," + up_lidar+","+parse_distance(distances));

                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.AutoFlush = true; 
                        long length = sw.BaseStream.Length;//will give expected output
                        long KB = length / 1024;
                        long MB = KB / 1024;
                        filesize = String.Format("{0} MB", MB);
                       // Console.WriteLine("{0} Distance values", distances.Count());

                        sw.WriteLine(time_stamp.ToString() + "," + gimbal + "," + up_lidar + "," + down_lidar +"," + parse_distance(distances));

                        last_distances = distances;

                        
                      
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
            _visualize = false;
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
            catch 
            {
                /* Do Nothing */
                statusText.Content = "Please plug in the Hokuyu and APM";
                startBtn.IsEnabled = false;
                stopBtn.IsEnabled = false;
            }
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (current_zoom < zoom_max)
            {
                current_zoom = current_zoom + zoom_increment;
            }
        }

        private void button_Copy1_Click(object sender, RoutedEventArgs e)
        {
            if (current_zoom > 10)
            {
                current_zoom = current_zoom - zoom_increment;
            } else if (current_zoom < 10)
            {
                current_zoom = 1;
            } else
            {
                current_zoom = 1;
            }
        }
    }
}
