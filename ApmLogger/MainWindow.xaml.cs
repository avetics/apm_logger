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



namespace ApmLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static bool _continue;
        static SerialPort _serialPort;
        static string port;
        

        static string path = @"apm_log.csv";


        public MainWindow()
        {
            InitializeComponent();
            if (SerialPort.GetPortNames().Count() > 0) {
                port = SerialPort.GetPortNames().First();
                statusText.Content = "To Log Data click Start";

            }
            else
            {
                port = "";
                portText.Text = "";
                startBtn.IsEnabled = false;
                stopBtn.IsEnabled = false;
                statusText.Content = "No Serial connection";
            }
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
                {
                   
                }
            
            

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
            _serialPort.PortName = port;
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
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            _continue = false;
            System.Diagnostics.Process.Start("notepad.exe", path);
            startBtn.IsEnabled = true;
            MainWindow.main.Status = "Click Start to log APM Data";
        }
    }
}
