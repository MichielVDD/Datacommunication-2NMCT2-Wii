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
            //Connectie openen met HID device met als vendor ID &H57E en als product ID &H306 
            _device = HIDDevice.GetHIDDevice(0x57E, 0x306);
        }

        public HIDDevice _device; //WII-Controller
        public Byte _ledRumble = 0x10; //standaard Led1 aan
        bool _rumble = false; //rumble moet aanblijven bij sent status 0x15;
        bool _chkUpdate = false; //Update checkboxen
        System.Windows.Threading.DispatcherTimer statusTimer = new System.Windows.Threading.DispatcherTimer();


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //StatusTimer set
            statusTimer.Tick += new EventHandler(statusTimer_Tick);
            statusTimer.Interval = new TimeSpan(0, 0, 0, 1);
            statusTimer.Start();

            FirstLed();
            Report(0x12, new byte[2] { 0x04, 0x37 });
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
            if (data != null)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    report.Data[i] = data[i];
                }
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
                        double batteryInfo = report.Data[5];
                        lblBattery.Content = batteryInfo + "%";
                        pgbBattery.Value = batteryInfo;
                        Leds(report);

                        break;
                    case 0x37:
                        GetButtons(report);
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
            int ledposition = 0x10;
            Console.Write(values);

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
            if(_chkUpdate == false)
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

        
    }
}
