using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using WD.Tester.Client;
using Module.Blade;
using NLog;

namespace TestClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger logger = LogManager.GetLogger("TestUILog");
        BladeModel bladeModel = null;

        public MainWindow()
        {
            InitializeComponent();

            bladeModel = new BladeModel();

            bladeModel.RemoteInstance.comStatusEvent += new WD.Tester.Module.StatusEventHandler(TestOnStatusEvent);
        }

        private void TestOnStatusEvent(object sender, WD.Tester.Module.StatusEventArgs e)
        {
            switch ( (WD.Tester.Enums.eventInts)e.EventType )
            {
                case WD.Tester.Enums.eventInts.Notify:
                    break;
                case WD.Tester.Enums.eventInts.NotifyWithContent:
                    break;
            }
        }

        private void btn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            logger.Debug("btn_Click [Content:{0}]", btn.Content.ToString());
            switch (btn.Content.ToString())
            {
                #region Blade status control
                case "Connect":
                    bladeModel.Connect("127.0.0.1", string.Empty, string.Empty);// 10.113.201.113
                    btn.Content = "Disconnect";
                    break;
                case "Disconnect":
                    bladeModel.Disconnect();
                    btn.Content = "Connect";
                    break;
                case "MemsOpen":
                    bladeModel.MemsControl = OnOffState.On;
                    btn.Content = "MemsClose";
                    break;
                case "MemsClose":
                    bladeModel.MemsControl = OnOffState.Off;
                    btn.Content = "MemsOpen";
                    break;
                case "CardPowerOpen":
                    bladeModel.CardPowerControl = OnOffState.On;
                    btn.Content = "CardPowerClose";
                    break;
                case "CardPowerClose":
                    bladeModel.CardPowerControl = OnOffState.Off;
                    btn.Content = "CardPowerOpen";
                    break;
                case "LCDOpen":
                    bladeModel.LCDControl = OnOffState.On;
                    btn.Content = "LCDClose";
                    break;
                case "LCDClose":
                    bladeModel.LCDControl = OnOffState.Off;
                    btn.Content = "LCDOpen";
                    break;
                case "AuxOut0Open":
                    bladeModel.AuxOut0Control = OnOffState.On;
                    btn.Content = "AuxOut0Close";
                    break;
                case "AuxOut0Close":
                    bladeModel.AuxOut0Control = OnOffState.Off;
                    btn.Content = "AuxOut0Open";
                    break;
                case "AuxOut1Open":
                    bladeModel.AuxOut1Control = OnOffState.On;
                    btn.Content = "AuxOut1Close";
                    break;
                case "AuxOut1Close":
                    bladeModel.AuxOut1Control = OnOffState.Off;
                    btn.Content = "AuxOut1Open";
                    break;
                #endregion Blade status control

                #region SN info
                case "RefreshSN":
                    RefreshSN();
                    break;
                #endregion SN info

                #region Slot status control

                #endregion Slot status control

                #region Data transmission
                case "WriteData":

                    break;
                case "ReadData":

                    break;
                #endregion Data transmission

                default:
                    break;
            }
        }

        private void RefreshSN()
        {
            //textBox1.Text = "test MemsSN";
            textBox1.Text = bladeModel.MEMSSN;
            textBox3.Text = bladeModel.DISKSN;
            textBox4.Text = bladeModel.MBPSN;
            textBox5.Text = bladeModel.ActuatorSN;
            textBox6.Text = bladeModel.PCBASN;
            textBox7.Text = bladeModel.BladeSN;
        }

        private void Btn_Service_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            logger.Debug("btn_Click [Content:{0}]", btn.Content.ToString());
            switch (btn.Content.ToString())
            {
                case "BladeFileRead":
                    break;
                case "BladeFileWrite":
                    break;
                case "Ping":
                    bladeModel.RemoteInstance.PingAllEvent("Hello");
                    break;
                case "InitializeTCL":
                    bladeModel.RemoteInstance.InitializeTCL();
                    break;
                case "BladeFileDir":
                    string path = "";
                    string filter = "";
                    bladeModel.RemoteInstance.BladeFileDir(path, filter);
                    break;
                case "GetBladeString":
                    break;
                case "SetBladeString":
                    break;
                case "GetBladeInteger":
                    break;
                case "SetBladeInteger":
                    break;
                case "CopyFileOnBlade":
                    string fromFile = "";
                    string toFile = "";
                    bladeModel.RemoteInstance.CopyFileOnBlade(fromFile, toFile);
                    break;
                case "BladeDelFile":
                    string fileName = "";
                    bladeModel.RemoteInstance.BladeDelFile(fileName);
                    break;
                case "SafelyRemove":
                    bladeModel.RemoteInstance.SafelyRemove();
                    break;
                case "PinMotionToggle":
                    bladeModel.RemoteInstance.PinMotionToggle();
                    break;
                case "Name":
                    bladeModel.RemoteInstance.Name();
                    break;
                case "GradeFilePath":
                    bladeModel.RemoteInstance.GradeFilePath();
                    break;
                case "FirmwareFilePath":
                    bladeModel.RemoteInstance.FirmwareFilePath();
                    break;
                case "FactFilePath":
                    bladeModel.RemoteInstance.FactFilePath();
                    break;
                case "GetMemsState":
                    bladeModel.RemoteInstance.GetMemsState(); // Add define.dll
                    break;
                case "GetFwRev":
                    bladeModel.RemoteInstance.GetFwRev();
                    break;
                case "GetBladeType":
                    bladeModel.RemoteInstance.GetBladeType();
                    break;
                case "GetSerialNumber":
                    bladeModel.RemoteInstance.GetSerialNumber();
                    break;
                case "GetTclStart":
                    bladeModel.RemoteInstance.GetTclStart();
                    break;
                case "CardPower":
                    bool state = true;
                    bladeModel.RemoteInstance.CardPower(state);
                    break;
                case "SetSerialNumber":
                    string serial = "";
                    bladeModel.RemoteInstance.SetSerialNumber(serial);
                    break;
                case "SetBladeType":
                    string bladeType = "";
                    bladeModel.RemoteInstance.SetBladeType(bladeType);
                    break;
                case "SetMotorBaseplateSN":
                    string serial1 = "";
                    bladeModel.RemoteInstance.SetMotorBaseplateSN(serial1);
                    break;
                case "SetMotorSN":
                    string s2 = "";
                    bladeModel.RemoteInstance.SetMotorSN(s2);
                    break;
                case "SetActuatorSN":
                    string s3 = "";
                    bladeModel.RemoteInstance.SetActuatorSN(s3);
                    break;
                case "SetDiskSN":
                    string s4 = "";
                    bladeModel.RemoteInstance.SetDiskSN(s4);
                    break;
                case "SetPcbaSN":
                    string s5 = "";
                    bladeModel.RemoteInstance.SetPcbaSN(s5);
                    break;
                case "SetJadeSN":
                    string s6 = "";
                    bladeModel.RemoteInstance.SetJadeSN(s6);
                    break;
                case "SetBladeLoc":
                    string s7 = "";
                    bladeModel.RemoteInstance.SetBladeLoc(s7);
                    break;
                case "SetMemsOpenDelay":
                    string delayMs = "";
                    bladeModel.RemoteInstance.SetMemsOpenDelay(delayMs);
                    break;
                case "SetMemsCloseDelay":
                    string delayMs1 = "";
                    bladeModel.RemoteInstance.SetMemsCloseDelay(delayMs1);
                    break;
                case "SetFlexSN":
                    string sn = "";
                    bladeModel.RemoteInstance.SetFlexSN(sn);
                    break;
                case "SetMemsSN":
                    string sn2 = "";
                    bladeModel.RemoteInstance.SetMemsSN(sn2);
                    break;
                case "SetTclStart":
                    string command = "";
                    bladeModel.RemoteInstance.SetTclStart(command);
                    break;
                case "PinMotion":
                    bool state2 = true;
                    bladeModel.RemoteInstance.PinMotion(state2);
                    break;
                case "BackLight":
                    bool state3 = true;
                    bladeModel.RemoteInstance.BackLight(state3);
                    break;
                case "AuxOut0":
                    int output = 0;
                    bladeModel.RemoteInstance.AuxOut0(output);
                    break;
                case "AuxOut1":
                    int output1 = 0;
                    bladeModel.RemoteInstance.AuxOut1(output1);
                    break;

                default:
                    break;
            }
        }

        // huoqu moge mulu xiamian moge xml wenjian de shuju     yi 
        private void Btn_BladeFileDir_Click(object sender, RoutedEventArgs e)
        {
            // remoteConnectLib.BladeFileDir();
        }
    }
}
