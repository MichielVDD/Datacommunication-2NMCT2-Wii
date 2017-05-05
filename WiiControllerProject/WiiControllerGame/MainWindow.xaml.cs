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

namespace WiiControllerGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public HIDDevice _device; //WII-Controller
        private bool _start = true; //Start Game
        public bool _Shoot = false;
        private Byte _ledRumble = 0xFE; //Standaard led
        private int _Levens = 4; //begin miss
        private int _Score = 0; //Score 
        private int _LimitScore = 10; //Score 
        private int _Countdown = 3;

        //Cursor
        private int cX = 0; //Cursor X
        private int cY = 0; //Cursor Y

        //Timer - aftellen voor start
        System.Windows.Threading.DispatcherTimer statusTimer;
        //Timer - totale tijd nodig voor de game uit te spelen
        System.Windows.Threading.DispatcherTimer resultTimer;
        int _Seconden = 0;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            InitStart();
        }
        private void InitStart()
        {
            bool result = Int32.TryParse(txtScore.Text, out _LimitScore);
            if (result == false)
            {
                _LimitScore = 10;
                txtScore.Text = _LimitScore.ToString();
            }

            //StatusTimer set
            statusTimer = new System.Windows.Threading.DispatcherTimer();
            statusTimer.Tick += new EventHandler(statusTimer_Tick);
            statusTimer.Interval = new TimeSpan(0, 0, 1);
            statusTimer.Start();
            txbCountdown.Text = _Countdown.ToString();
            _Countdown--;

            //StatusTimer set
            resultTimer = new System.Windows.Threading.DispatcherTimer();
            resultTimer.Tick += new EventHandler(resultTimer_Tick);
            resultTimer.Interval = new TimeSpan(0, 0, 1);
        }
        private void resultTimer_Tick(object sender, EventArgs e)
        {
             _Seconden++;
        }
        private void statusTimer_Tick(object sender, EventArgs e)
        {
            txbCountdown.Text = _Countdown.ToString();

            if (_Countdown == 0)
            {
                resultTimer.Start();
                statusTimer.Stop();
                txbCountdown.Text = "";
                ResetVariables();
                _device = HIDDevice.GetHIDDevice(0x57E, 0x306);
                _start = true;

                if (_device != null)
                {
                    Report(0x11, new byte[1] { _ledRumble });
                    EnableConfigureIR();
                    Report(0x12, new byte[2] { 0x04, 0x37 });
                    RndNumber();
                }
                else
                {
                    Console.WriteLine("Geen device gevonden");
                }
            }else
            {
                _Countdown--;
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
                            break;
                        //Alle data terug van report. Status/Buttons/IR/Accel
                        case 0x37:
                        //Stop met tonen Cursor en game na stoppen game
                            if (_start == true)
                            {
                                GetIRPoints(report);
                            }
                            break;
                    }
                    _device.ReadReport(OnReadReport);
                }
        }
        private void EnableConfigureIR()
        {
            Report(0x13, new byte[1] { 0x04 });
            Report(0x1A, new byte[1] { 0x04 });

            WriteData(0x04b00030, new byte[1] { 0x08 });
            WriteData(0x04b00000, new byte[9] { 0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xaa, 0x00, 0x64 });
            WriteData(0x04b0001a, new byte[2] { 0x63, 0x03 });
            WriteData(0x04b00033, new byte[1] { 0x08 });
            WriteData(0x04b00030, new byte[1] { 0x08 });
        }
        private void WriteData(int address, byte[] data)
        {
            if ((_device != null))
            {
                int index = 0;
                while (index < data.Length)
                {
                    // Bepaal hoeveel bytes er nog moeten verzonden worden                     
                    int leftOver = data.Length - index;
                    // We kunnen maximaal 16 bytes per keer verzenden dus moeten we het aantal te verzenden bytes daarop limiteren                     
                    int count = (leftOver > 16 ? 16 : leftOver);
                    int tempAddress = address + index;
                    HIDReport report = _device.CreateReport();
                    report.ReportID = 0x16; report.Data[0] = (byte)((tempAddress & 0x4000000) >> 0x18);
                    report.Data[1] = (byte)((tempAddress & 0xff0000) >> 0x10);
                    report.Data[2] = (byte)((tempAddress & 0xff00) >> 0x8);
                    report.Data[3] = (byte)((tempAddress & 0xff));
                    report.Data[4] = (byte)count;
                    Buffer.BlockCopy(data, index, report.Data, 5, count);
                    _device.WriteReport(report);
                    index += 16;
                }
            }
        }
        private void GetIRPoints(HIDReport report)
        {
            int x1 = report.Data[5] | ((report.Data[7] & 0x30) << 4);
            int x2 = report.Data[8] | ((report.Data[7] & 0x03) << 8);
            int x3 = report.Data[10] | ((report.Data[12] & 0x30) << 4);
            int x4 = report.Data[13] | ((report.Data[12] & 0x03) << 8);

            int y1 = report.Data[6] | ((report.Data[7] & 0xC0) << 2);
            int y2 = report.Data[9] | ((report.Data[7] & 0x0C) << 6);
            int y3 = report.Data[11] | ((report.Data[12] & 0xC0) << 2);
            int y4 = report.Data[14] | ((report.Data[12] & 0x0C) << 6);


            Ellipse el = new Ellipse();
            //Check if Cursor hits Target on B press
            byte button = report.Data[1];


            //Controleer of de knop is ingedrukt
            //Controller of het target is geraakt
            if ((button & 0x04) == 0x04)
            {
                el.Fill = Brushes.Gray;
                //Bij het indrukken maar 1 maal schieten
                if (_Shoot == false)
                {
                    _Shoot = true;
                    Report(0x11, new byte[1] { (Byte)(_ledRumble ^= 0x01) });
                    if (x1 > (1023 - cX) - 60 & x1 < ((1015 - cX)) & y1 > cY & y1 < (cY + 60))
                    {
                        _Score++;
                        txbScore.Text = _Score.ToString();
                        gameDraw.Children.Clear();
                        if (_Score >= _LimitScore)
                        {
                            EndGame("WIN");
                        }
                        else
                        {
                            RndNumber();
                            ShootCanvas();
                        }
                    }
                    else
                    {
                        _Levens--;
                        txbLevens.Text = _Levens.ToString();
                        //Update leds
                        LevensCounter();
                        if (_Levens == 0) EndGame("LOSE");
                    }
                }
            }
            else
            {
                el.Fill = Brushes.Black;
                if (_Shoot == true)
                {
                    _Shoot = false;
                    Report(0x11, new byte[1] { (Byte)(_ledRumble ^= 0x01) });
                }
            }

            //Indien de game gedaan is dan moet je de cursor niet meer tonen
            if (_start == true)
            {
                DrawCursor(x1, y1, el);
                UpdateTimerUI();
            }
        }
        private void DrawCursor(int x1, int y1, Ellipse el)
        {
 
            if (y1 < 752 & x1 > 10)
            {
                cursorDraw.Children.Clear();

                el.Width = 15;
                el.Height = 15;

                Canvas.SetLeft(el, 1015 - x1);
                Canvas.SetTop(el, y1);

                cursorDraw.Children.Add(el);
            }
        }
        private String GetUserTime()
        {
            TimeSpan ts = TimeSpan.FromSeconds(_Seconden);
            String time = string.Format("Tijd: {0}", new DateTime(ts.Ticks).ToString("HH:mm:ss"));
            return time;
        }
        private void UpdateTimerUI()
        {
            txbResultaat.Text = GetUserTime();
        }
        private void LevensCounter()
        {
            switch (_Levens)
            {
                case 1:
                    _ledRumble ^= 0x20;
                    break;
                case 2:
                    _ledRumble ^= 0x40;
                    break;
                case 3:
                    _ledRumble ^= 0x80;
                    break;
            }

            Report(0x11, new byte[1] { _ledRumble });
        }
        private void RndNumber()
        {
            Random rnd = new Random();
            cX = rnd.Next(120, 903);
            cY = rnd.Next(120, 647);
            ShootCanvas();
        }
        private void ShootCanvas()
        {
            gameDraw.Children.Clear();
            Ellipse el = new Ellipse();
            el.Width = 60;
            el.Height = 60;
            el.SetValue(Canvas.LeftProperty, (Double)cX);
            el.SetValue(Canvas.TopProperty, (Double)cY);
            el.Fill = Brushes.Red;

            gameDraw.Children.Add(el);
        }
        private void EndGame(String s)
        {
            resultTimer.Stop();

            //Laatste shot rumble nog even laten
            Thread.Sleep(100);

            //spel stoppen
            _start = false;
            gameDraw.Children.Clear();
            cursorDraw.Children.Clear();
            _ledRumble = 0x00;
            Report(0x11, new byte[1] { _ledRumble });
            btnStart.Content = "Restart Game";
            ScoreBord(txtName.Text + " / " + s + " (" + txbScore.Text + "-" + txbLevens.Text + ") in: " + GetUserTime());
            cursorDraw.Children.Clear();
        }
        private void ScoreBord(String s)
        {
            lstScores.Items.Add(s);
        }
        private void ResetVariables()
        {
            _Seconden = 0;
            _Shoot = false;
            _Levens = 4;
            txbLevens.Text = _Levens.ToString();
            _Countdown = 3;
            _ledRumble = 0xFE;
            txbResultaat.Text = "";
            _Score = 0;
            txbScore.Text = _Score.ToString();
        }

    }
}
