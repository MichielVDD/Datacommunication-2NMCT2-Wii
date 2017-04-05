using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using WII.HID.Lib;

namespace WiiControllerProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public HIDDevice _device; //WII-Controller
        public Byte _ledRumble = 0x10;
        bool _rumble = false; //rumble moet aanblijven bij sent status 0x15;
        bool _chkUpdate = false; //Update checkboxen
        System.Windows.Threading.DispatcherTimer statusTimer = new System.Windows.Threading.DispatcherTimer();

        //Huidige accel waarde
        private int[] _xyzAccelValue = new int[3];

        //Volgorde x-y-z + nulpunt van de as
        public int[] _xPlusCal = { 613, 504, 518, 508 }; //linkse kant naar boven
        public int[] _yPlusCal = { 508, 608, 518, 504 }; //IR naar beneden
        public int[] _zPlusCal = { 508, 504, 616, 518 }; //Plat op tafel 


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Connectie openen met HID device met als vendor ID &H57E en als product ID &H306 
            _device = HIDDevice.GetHIDDevice(0x57E, 0x306);

            if (_device != null)
            {
                //StatusTimer set
                statusTimer.Tick += new EventHandler(statusTimer_Tick);
                statusTimer.Interval = new TimeSpan(0, 0, 0, 1);
                statusTimer.Start();

                FirstLed();
                Report(0x12, new byte[2] { 0x04, 0x37 });
            }
            else
            {
                Console.WriteLine("Geen device gevonden");
            }
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            if (_rumble == true)
            {
                Report(0x15, new byte[1] { 0x01 });
            }
            else
            {
                Report(0x15, new byte[1] { 0x00 });
            }

        }

        public void Report(byte reportID, byte[] data)
        {
            //Report aanmaken 
            HIDReport report = _device.CreateReport();
            // Report ID instellen op dat voor het aansturen van de player LED's             
            report.ReportID = reportID;
            //DATA invullen
            for (int i = 0; i < data.Length; ++i)
            {
                report.Data[i] = data[i];
            }
            //Het report versturen   
            _device.WriteReport(report);
            _device.ReadReport(OnReadReport);
        }

        private void OnReadReport(HIDReport report)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
            {
                this.Dispatcher.Invoke(new ReadReportCallback(OnReadReport), report);
            }
            else
            {
                switch (report.ReportID)
                {
                    case 0x20:
                        //status report
                        double batteryInfo = report.Data[5] * 0.5;
                        lblBattery.Content = batteryInfo + "%";
                        pgbBattery.Value = batteryInfo;
                        Leds(report);

                        break;
                    case 0x37:
                        GetButtons(report);
                        Accelerometer(report);
                        break;
                }
                _device.ReadReport(OnReadReport);
            }
        }

        private void GetButtons(HIDReport report)
        {
            SolidColorBrush actief = new SolidColorBrush(System.Windows.Media.Colors.Blue);
            SolidColorBrush nietactief = new SolidColorBrush(System.Windows.Media.Colors.White);

            byte byte1 = report.Data[0];

            if ((byte1 & 0x01) == 0x01)
            {
                Left.Fill = actief;
            }
            else
            {
                Left.Fill = nietactief;
            }
            if ((byte1 & 0x02) == 0x02)
            {
                Right.Fill = actief;
            }
            else
            {
                Right.Fill = nietactief;
            }
            if ((byte1 & 0x04) == 0x04)
            {
                Down.Fill = actief;
            }
            else
            {
                Down.Fill = nietactief;
            }
            if ((byte1 & 0x08) == 0x08)
            {
                Up.Fill = actief;
            }
            else
            {
                Up.Fill = nietactief;
            }
            if ((byte1 & 0x10) == 0x10)
            {
                Plus.Fill = actief;
            }
            else
            {
                Plus.Fill = nietactief;
            }


            byte byte2 = report.Data[1];

            if ((byte2 & 0x01) == 0x01)
            {
                Twee.Fill = actief;
            }
            else
            {
                Twee.Fill = nietactief;
            }
            if ((byte2 & 0x02) == 0x02)
            {
                Een.Fill = actief;
            }
            else
            {
                Een.Fill = nietactief;
            }
            if ((byte2 & 0x04) == 0x04)
            {
                B.Fill = actief;
            }
            else
            {
                B.Fill = nietactief;
            }
            if ((byte2 & 0x08) == 0x08)
            {
                A.Fill = actief;
            }
            else
            {
                A.Fill = nietactief;
            }
            if ((byte2 & 0x10) == 0x10)
            {
                Min.Fill = actief;
            }
            else
            {
                Min.Fill = nietactief;
            }
            if ((byte2 & 0x80) == 0x80)
            {
                Home.Fill = actief;
            }
            else
            {
                Home.Fill = nietactief;
            }
        }

        private void FirstLed()
        {
            chkLed1.Checked -= LedRumble;
            chkLed1.Unchecked -= LedRumble;
            chkLed1.IsChecked = true;
            Report(0x11, new byte[] { _ledRumble });
            chkLed1.Checked += LedRumble;
            chkLed1.Unchecked += LedRumble;
        }

        private void Leds(HIDReport report)
        {
            int ledStatus = report.Data[2];
            int values = ledStatus & 0xF0;

            _chkUpdate = true;
            if ((values & 0x10) == 0)
            {
                chkLed1.IsChecked = false;
            }
            else
            {
                chkLed1.IsChecked = true;
            }
            if ((values & 0x20) == 0)
            {
                chkLed2.IsChecked = false;
            }
            else
            {
                chkLed2.IsChecked = true;
            }
            if ((values & 0x40) == 0)
            {
                chkLed3.IsChecked = false;
            }
            else
            {
                chkLed3.IsChecked = true;
            }
            if ((values & 0x80) == 0)
            {
                chkLed4.IsChecked = false;
            }
            else
            {
                chkLed4.IsChecked = true;
            }
            _chkUpdate = false;
        }

        private void LedRumble(object sender, RoutedEventArgs e)
        {
            if (_chkUpdate == false)
            {
                CheckBox ch = sender as CheckBox;
                String send = ch.Tag.ToString();

                switch (send)
                {
                    case "led1":
                        _ledRumble ^= 0x10;
                        break;
                    case "led2":
                        _ledRumble ^= 0x20;
                        break;
                    case "led3":
                        _ledRumble ^= 0x40;
                        break;
                    case "led4":
                        _ledRumble ^= 0x80;
                        break;
                    case "rumble":
                        _ledRumble ^= 0x01;
                        if (_rumble == false)
                        {
                            _rumble = true;
                        }
                        else
                        {
                            _rumble = false;
                        }
                        break;
                }

                Report(0x11, new byte[] { _ledRumble });
            }

        }

        private void Accelerometer(HIDReport report)
        {
            _xyzAccelValue[0] = ((int)report.Data[2] << 2) ^ (report.Data[0] >> 5);
            _xyzAccelValue[1] = ((int)report.Data[3] << 2) ^ (report.Data[1] >> 5);
            _xyzAccelValue[2] = ((int)report.Data[4] << 2) ^ (report.Data[1] >> 6);

            GetNoAccelValue();

            double[] controllerAccelPosition = new double[3];
            controllerAccelPosition[0] = Math.Round((double)(_xyzAccelValue[0] - _xPlusCal[3]) / (double)(_xPlusCal[0] - _xPlusCal[3]), 7);
            controllerAccelPosition[1] = Math.Round((double)(_xyzAccelValue[1] - _yPlusCal[3]) / (double)(_yPlusCal[1] - _yPlusCal[3]), 7);
            controllerAccelPosition[2] = Math.Round((double)(_xyzAccelValue[2] - _zPlusCal[3]) / (double)(_zPlusCal[2] - _zPlusCal[3]), 7);

            lblX.Content = controllerAccelPosition[0];
            lblY.Content = controllerAccelPosition[1];
            lblZ.Content = controllerAccelPosition[2];
        }

        public void GetNoAccelValue()
        {
             //Bereken de exacte waarde van het nulpunt op die as
            _xPlusCal[3] = (_yPlusCal[0] + _zPlusCal[0]) / 2; //x
            _yPlusCal[3] = (_xPlusCal[1] + _zPlusCal[1]) / 2; //y
            _zPlusCal[3] = (_xPlusCal[2] + _yPlusCal[2]) / 2; //z
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            _zPlusCal[0] = _xyzAccelValue[0];
            _zPlusCal[1] = _xyzAccelValue[1];
            _zPlusCal[2] = _xyzAccelValue[2];
        }

        private void btnStap2_Click(object sender, RoutedEventArgs e)
        {
            _yPlusCal[0] = _xyzAccelValue[0];
            _yPlusCal[1] = _xyzAccelValue[1];
            _yPlusCal[2] = _xyzAccelValue[2];
        }

        private void btnStap3_Click(object sender, RoutedEventArgs e)
        {
            _xPlusCal[0] = _xyzAccelValue[0];
            _xPlusCal[1] = _xyzAccelValue[1];
            _xPlusCal[2] = _xyzAccelValue[2];
        }
    }
}
