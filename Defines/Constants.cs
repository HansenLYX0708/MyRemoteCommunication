using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Hitachi.Tester
{
    public class Constants
    {
        public static readonly int NumTrayDrawers = GetConfig<int>("NumTrayDrawers", 7);
        public static readonly int NumTraysPerDrawer = GetConfig<int>("NumTraysPerDrawer", 2);
        public static readonly int NumTrays = GetConfig<int>("NumTrays", 14);
        public static readonly int NumBlades = GetConfig<int>("NumBlades", 5);
        public static readonly int TopBladeMaskBit = GetConfig<int>("TopBladeMaskBit", 0x20);
        public static readonly int NumPickerTip = GetConfig<int>("NumPickerTip", 2);
        public static readonly int TesterClientSinkPort = GetConfig<int>("TesterClientSinkPort", 8086);
        public static readonly int ButtonSmallSize = GetConfig<int>("ButtonSmallSize", 23);
        public static readonly int ButtonLargeSize = GetConfig<int>("ButtonLargeSize", 94);
        public static readonly int SliderSerialFullLen = GetConfig<int>("SliderSerialFullLen", 10);
        public static readonly int SliderColOffset = GetConfig<int>("SliderColOffset", 8);    // used to parse sliderSN
        public static readonly int SliderRowOffset = GetConfig<int>("SliderRowOffset", 5);    // used to parse sliderSN
        public static readonly int QuickButtonPanelSize = GetConfig<int>("QuickButtonPanelSize", 115);
        public static readonly int DefaultRampValue = GetConfig<int>("DefaultRampValue", 100);
        public static readonly int MaxRampValue = GetConfig<int>("MaxRampValue", 255);
        public static readonly int MinRampValue = GetConfig<int>("MinRampValue", 0);
        public static readonly int TesterObjectFactErrorLimit = GetConfig<int>("TesterObjectFactErrorLimit", 50);
        public static readonly int LongAutoPopDelay = GetConfig<int>("LongAutoPopDelay", 30000);
        public static readonly int RegularAutoPopDelay = GetConfig<int>("RegularAutoPopDelay", 5000);
        public static readonly int MaxTextBoxAppendBytes = GetConfig<int>("MaxTextBoxAppendBytes", 262100);
        public static readonly int MinBunnyPosition = GetConfig<int>("MinBunnyPosition", 100);
        public static readonly int MaxBunnyPosition = GetConfig<int>("MaxBunnyPosition", 5900);
        public static readonly int MinBunnyVelocity = GetConfig<int>("MinBunnyVelocity", 10);
        public static readonly int MaxBunnyVelocity = GetConfig<int>("MaxBunnyVelocity", 511);
        public static readonly int MinBunnyAcceleration = GetConfig<int>("MinBunnyAcceleration", 1);
        public static readonly int MaxBunnyAcceleration = GetConfig<int>("MaxBunnyAcceleration", 511);
        public static readonly int TypicalOpenAcceleration = GetConfig<int>("TypicalOpenAcceleration", 50);
        public static readonly int TypicalOpenVelocity = GetConfig<int>("TypicalOpenVelocity", 50);
        public static readonly int StartinglOpenPosition = GetConfig<int>("StartinglOpenPosition", 3000);
        public static readonly int TypicalCloseAcceleration = GetConfig<int>("TypicalCloseAcceleration", 50);
        public static readonly int TypicalCloseVelocity = GetConfig<int>("TypicalCloseVelocity", 50);
        public static readonly int TypicallClosePosition = GetConfig<int>("TypicallClosePosition", 4500);
        public static readonly int BitWindowRefreshRate = GetConfig<int>("BitWindowRefreshRate", 25);  // ms
        public static readonly int CopyRetryLimit = GetConfig<int>("CopyRetryLimit", 2);
        public static readonly int BunnyRetryLimit = GetConfig<int>("BunnyRetryLimit", 10);
        public static readonly int AnalogAverageCount = GetConfig<int>("AnalogAverageCount", 50);
        //public static readonly int SleepMs = GetConfig<int>("SleepMs", 10);
        public static readonly int MemsTimeout = GetConfig<int>("MemsTimeout", 3000);
        public static readonly int CommandWindowMaxCharLength = GetConfig<int>("CommandWindowMaxCharLength", 32767);
        public static readonly int MaxWorkerThreadsConst = GetConfig<int>("MaxWorkerThreadsConst", 5000);
        public static readonly int KeepAliveTimeout = GetConfig<int>("KeepAliveTimeout", 30000);
        public static readonly int WcfBladePort = GetConfig<int>("WcfBladePort", 9124);
        public static readonly int MemsOpenTimeoutMins = GetConfig<int>("MemsOpenTimeoutMins", 10);

        public static readonly int WdYRows = GetConfig<int>("WdYRows", 22);
        public static readonly int WdXCols = GetConfig<int>("WdXCols", 16);
        public static readonly int HgstYRows = GetConfig<int>("HgstYRows", 20);
        public static readonly int HgstXCols = GetConfig<int>("HgstXCols", 25);
 
       public static readonly double AnalogSlope = GetConfig<double>("AnalogSlope", 52.3);
        public static readonly double AnalogOffset = GetConfig<double>("AnalogOffset", -160.0);
       public static readonly double AnalogOffset1 = GetConfig<double>("AnalogOffset1", 3.01);
       public static readonly double AnalogOffset2 = GetConfig<double>("AnalogOffset2", 0.0);
       public static readonly double verNumFirm = GetConfig<double>("verNumFirm", 3.0);
       public static readonly double verNumDriver = GetConfig<double>("verNumDriver", 4.0);
        public static readonly double GuiZoomX = GetConfig<double>("ZoomX", 0.22);
        public static readonly double GuiZoomY = GetConfig<double>("ZoomY", 0.22);
        public static readonly double DialogZoomX = GetConfig<double>("ZoomX", 0.5);
        public static readonly double DialogZoomY = GetConfig<double>("ZoomY", 0.5);

        public static readonly int CleanPickerDivisions = GetConfig<int>("CleanPickerDivisions", 4);
        public static readonly double CleanPickerResolution = GetConfig<double>("CleanPickerResolution", 18.0);

        public static readonly string SpitSlider = GetConfig<string>("SpitSlider", "spitSlider");
        //public static readonly string MoveBladeToBlade = GetConfig<string>("MoveBladeToBlade", "moveBladeToBlade");
        public static readonly string ReturnToTray = GetConfig<string>("ReturnToTray", "returnToTrayHGWD");
        //public static readonly string OpenCloseDrawer = GetConfig<string>("OpenCloseDrawer", "openCloseDrawer");
        public static readonly string DeployFact = GetConfig<string>("DeployFact", "deployFact");
        public static readonly string GUIInitForTest = GetConfig<string>("GUIInitForTest", "GUIInitForTest");
        public static readonly string SetSliderInfo = GetConfig<string>("SetSliderInfo", "setSliderInfo");
        public static readonly string GuiSetBladeInfoFromJAS = GetConfig<string>("GUISetBladeInfoFromJAS", "GUISetBladeInfoFromJAS");
        public static readonly string SetJadeInfo = GetConfig<string>("SetJadeInfo", "setJadeInfo");
        public static readonly string MoveTrayToTray = GetConfig<string>("MoveTrayToTray", "movePairToPair");
        public static readonly string HomeStages = GetConfig<string>("HomeStages", "moveToClear");
        public static readonly string MoveToClear = GetConfig<string>("MoveToClear", "moveToClear");
        public static readonly string JadeLoadUnloadPart = GetConfig<string>("JadeLoadUnloadPart", "jadeLoadUnloadPart");
        public static readonly string GUIOneTimeInit = GetConfig<string>("GUIOneTimeInit", "GUIOneTimeInit");
        //public static readonly string RunGradePart = GetConfig<string>("RunGradePart", "GUIRunGradePart");
        public static readonly string BCFCollectionControl = GetConfig<string>("BCFCollectionControl", "BCFCollectionControl");
        public static readonly string GUIPrepareBladesForTest = GetConfig<string>("GUIPrepareBladesForTest", "GUIPrepareBladesForTest");
        public static readonly string TestCylinder = GetConfig<string>("TestCylinder", "$JATest(testLocation)");
        public static readonly string TestHead = GetConfig<string>("TestHead", "0");
        public static readonly string FactTestHead = GetConfig<string>("FactTestHead", "$FACTTest(testHead)");
        public static readonly string FactTestLoc = GetConfig<string>("FactTestLoc", "$FACTTest(testLocation)");
        public static readonly string GUIUpdateInsiteInfoToBlade = GetConfig<string>("GUIUpdateInsiteInfoToBlade", "GUIUpdateInsiteInfoToBlade");
        public static readonly string GUIReportParametricData = GetConfig<string>("GUIReportParametricData", "GUIReportParametricData");
        public static readonly string GUIGetFACTData = GetConfig<string>("GUIGetFACTData", "GUIGetFACTData");
        public static readonly string SetupGradeFile = GetConfig<string>("SetupGradeFile", "setupGradeFile");
        public static readonly string SetupGradeTable = GetConfig<string>("SetupGradeTable", "setupGradeTable");
        public static readonly string MoveCameraToBlade = GetConfig<string>("MoveCameraToBlade", "moveCameraToBlade");
        public static readonly string MoveCameraToTray = GetConfig<string>("MoveCameraToTray", "moveCameraToTray");
        public static readonly string RUNNUM = GetConfig<string>("RUNNUM", "RUNNUM");
        public static readonly string RETESTNUM = GetConfig<string>("RETESTNUM", "RETESTNUM");
        public static readonly string HOLDER = GetConfig<string>("HOLDER", "HOLDER");
        public static readonly string HOLDERNUM = GetConfig<string>("HOLDERNUM", "HOLDERNUM");
        public static readonly string SITE = GetConfig<string>("SITE", "SITE");
        public static readonly string GUIInitForBladeLoad = GetConfig<string>("GUIInitForBladeLoad", "GUIInitForBladeLoad");
        public static readonly string GUIClearConsecutiveCounters = GetConfig<string>("clearConsecutiveCounters", "clearConsecutiveCounters");
        public static readonly string VarToEvent = GetConfig<string>("VarToEvent", "varToEvent");
        public static readonly string OurTmpVar = GetConfig<string>("OurTmpVar", "oUrtcLTmpvAr");
        public static readonly string GUIInitFlags = GetConfig<string>("GUIInitFlags", "GUIInitFlags");
        public static readonly string GUITable = GetConfig<string>("GUITable", "GUITable");
        public static readonly string GUISN = GetConfig<string>("GUISN", "GUISN");
        public static readonly string GUIProductName = GetConfig<string>("GUIProductName", "GUIProductName");
        public static readonly string GridViewNameColumn = GetConfig<string>("GridViewNameColumn", "GridViewNameColumn");
        public static readonly string GridViewValueColumn = GetConfig<string>("GridViewNameColumn", "GridViewValueColumn");
        public static readonly string ColumnJadePlusName = GetConfig<string>("ColumnJadePlusName", "ColumnJadePlusName");
        public static readonly string ColumnJadePlusValue = GetConfig<string>("ColumnJadePlusValue", "ColumnJadePlusValue");
        public static readonly string ColumnJadeLocalName = GetConfig<string>("ColumnJadeLocalName", "ColumnJadeLocalName");
        public static readonly string ColumnJadeLocalValue = GetConfig<string>("ColumnJadeLocalValue", "ColumnJadeLocalValue");
        public static readonly string JadePlusFlags = GetConfig<string>("JadePlusFlags", "JadePlusFlags");
        public static readonly string JadeLocalFlags = GetConfig<string>("JadeLocalFlags", "JadeLocalFlags");
        public static readonly string JasConfigDir = GetConfig<string>("JasConfigDir", "c:\\JASConfig");
        public static readonly string JadeCountsPath = GetConfig<string>("JadeCountsPath", "c:\\jadeCounts");
        public static readonly string SaveTesterConfig = GetConfig<string>("SaveTesterConfig", "saveTesterConfig");
        public static readonly string RestoreTesterConfig = GetConfig<string>("RestoreTesterConfig", "restoreTesterConfig");

        public static readonly string GradeToken = GetConfig<string>("GRADE_", "GRADE_");
        public static readonly string RankToken = GetConfig<string>("GRADE_R_", "GRADE_R_");
        public static readonly string ErrorToken = GetConfig<string>("ERRORCODE_", "ERRORCODE_");
        public static readonly string ConsecutiveToken = GetConfig<string>("CONSECUTIVE_", "CONSECUTIVE_");
        public static readonly string InitializeAttrib = GetConfig<string>("InitializeAttrib", "initialize");
        public static readonly string ContactAttrib = GetConfig<string>("ContactAttrib", "contact");
        public static readonly string MeasureAttrib = GetConfig<string>("MeasureAttrib", "measure");
        public static readonly string PassAttrib = GetConfig<string>("PassAttrib", "pass");
        public static readonly string FailAttrib = GetConfig<string>("FailAttrib", "fail");


        public static readonly string BladePickFail = GetConfig<string>("BladePickFail", "BladePickFail");
        public static readonly string TrayPickFail = GetConfig<string>("TrayPickFail", "TrayPickFail");

        public static readonly string NotCleanTray = GetConfig<string>("NotCleanTray", "NotCleanTray");
        public static readonly string CleanCount = GetConfig<string>("CleanCount", "CleanCount");

        public static readonly string DGRCount = GetConfig<string>("DGRCount", "DGRCount");
        //public static readonly string DGRPlease = GetConfig<string>("DGRPlease", "DGRPlease");
        public static readonly string StatsExt = GetConfig<string>("StatsExt", "BladeStats");

        public static readonly string GUIDispositionCandidateSkip = GetConfig<string>("GUIDispositionCandidateSkip", "GUIDispositionCandidateSkip");
        public static readonly string GUICheckLimits = GetConfig<string>("GUICheckLimits", "GUICheckLimits");



        public static readonly string StopIt = GetConfig<string>("StopIt", "StopIt!");
        public static readonly string StoppedIt = GetConfig<string>("StoppedIt", "StoppedIt!");
        public static readonly string ErrorLimFile = GetConfig<string>("ErrorLimFile", "_ErrorLim.txt");

        public static readonly string AccessDenied = GetConfig<string>("AccessDenied", "Access denied");

        public static readonly string BarCodeCamera = GetConfig<string>("BarCodeCamera", "10.10.6.100");

        public static readonly string Blade1IpAddr = GetConfig<string>("Blade1IpAddr", "10.10.6.151");
        public static readonly string Blade2IpAddr = GetConfig<string>("Blade2IpAddr", "10.10.6.152");
        public static readonly string Blade3IpAddr = GetConfig<string>("Blade3IpAddr", "10.10.6.153");
        public static readonly string Blade4IpAddr = GetConfig<string>("Blade4IpAddr", "10.10.6.154");
        public static readonly string Blade5IpAddr = GetConfig<string>("Blade5IpAddr", "10.10.6.155");

        public static readonly string AddressRIO1 = GetConfig<string>("AddressRIO1", "10.10.6.111");
        public static readonly string AddressRIO2 = GetConfig<string>("AddressRIO2", "10.10.6.112");
        public static readonly string AddressRIO3 = GetConfig<string>("AddressRIO3", "10.10.6.110");
        public static readonly string AddressRIO4 = GetConfig<string>("AddressRIO4", "10.10.6.113");

        // Jade files 
        public static readonly string ParametricStorageFile = GetConfig<string>("ParametricStorageFile", "parametricStorageFile.dat");
        public static readonly string TrayMapFile = GetConfig<string>("TrayMapFile", "JadeTrays.dat");
        public static readonly string JadeFlagsFile = GetConfig<string>("JadeFlagsFile", "JadeFlags.dat");
        public static readonly string EndTag = GetConfig<string>("EndTag", "end");
        public static readonly string UnitHeaderFile = GetConfig<string>("UnitHeaderFile", "UnitHeaderFile.dat");
        public static readonly string CommTestString = GetConfig<string>("CommTestString", "Testing Testing Testing");
        public static readonly string KeepAliveString = GetConfig<string>("KeepAliveString", "Keep alive event");
        public static readonly string ParameterFile = GetConfig<string>("ParameterFile", "TesterClient.dat");
        //public static readonly string TesterCntFile = GetConfig<string>("TesterCntFile", "TesterCounts.txt");
        public static readonly string MaintenanceDefs = GetConfig<string>("MaintenanceDefs", "MaintenanceDefs.xml");
        public static readonly string BladeSNTXT = GetConfig<string>("BladeSNTXT", "BladeSN.TXT");
        public static readonly string BladeCapabilityTXT = GetConfig<string>("BladeCapabilityTXT", "BladeCapability.TXT");
        public static readonly string MotorBaseplateTXT = GetConfig<string>("MotorBaseplateTXT", "MotorBaseplate.TXT");
        public static readonly string ActuatorTXT = GetConfig<string>("ActuatorTXT", "Actuator.TXT");
        public static readonly string DiskSNTXT = GetConfig<string>("DiskSNTXT", "DiskSN.TXT");
        public static readonly string PcbaSNTXT = GetConfig<string>("PcbaSNTXT", "PcbaSN.TXT");
        public static readonly string MemsSNTXT = GetConfig<string>("MemsSNTXT", "MemsSN.TXT");
        public static readonly string MemsRampTXT = GetConfig<string>("MemsRampTXT", "MemsRamp.TXT");
        public static readonly string MemsOpenSensorTXT = GetConfig<string>("MemsOpenSensorTXT", "MemsOpenSensor.TXT");
        public static readonly string MemsCloseSensorTXT = GetConfig<string>("MemsCloseSensorTXT", "MemsCloseSensor.TXT");
        public static readonly string MemsOpenDelayTXT = GetConfig<string>("MemsOpenDelayTXT", "MemsOpenDelay.TXT");
        public static readonly string MemsCloseDelayTXT = GetConfig<string>("MemsCloseDelayTXT", "MemsCloseDelay.TXT");
        public static readonly string FlexSNTXT = GetConfig<string>("FlexSNTXT", "FlexSN.TXT");
        public static readonly string TesterObjectDAT = GetConfig<string>("TesterObjectDAT", "TesterObject.DAT");
        public static readonly string TesterCountsTXT = GetConfig<string>("TesterCountsTXT", "TesterCounts.TXT");
        public static readonly string MotorSNTXT = GetConfig<string>("MotorSNTXT", "MotorSN.TXT");
        public static readonly string JadeSNTXT = GetConfig<string>("JadeSNTXT", "JadeSN.TXT");
        public static readonly string BladeLocTXT = GetConfig<string>("BladeLocTXT", "BladeLoc.TXT");
        public static readonly string WhichPinMotionFileNameTXT = GetConfig<string>("WhichPinMotionFileNameTXT", "WhichPinMotion.TXT");
        //public static readonly string MaintenanceCodeFileTXT = GetConfig<string>("MaintenanceCodeFileTXT", "MaintenanceCodeFile.TXT");
        //public static readonly string MaintenanceDateFileTXT = GetConfig<string>("MaintenanceDateFileTXT", "MaintenanceDateFile.TXT");
        public static readonly string ConfigurationXml = GetConfig<string>("ConfigurationXml", "Configuration.xml");

        public static readonly string FACTCmdErrorString = GetConfig<string>("FACTCmdErrorString", "sendCommand driver error");
        public static readonly string SetOutputInfo = GetConfig<string>("SetOutputInfo", "setOutputInfo");

        public static readonly string BladeLocationDat = GetConfig<string>("BladeLocationDat", "BladeLocation.Dat");
        public static readonly string TestSequenceXml = GetConfig<string>("TestSequenceXml", "TestSequence.xml");
        public static readonly string TestSequenceXsd = GetConfig<string>("TestSequenceXsd", "TestSequence.xsd");
        public static readonly string AllSpecTableXml = GetConfig<string>("AllSpecTableXml", "allSpecTable.xml");
        public static readonly string AllSpecTableXsd = GetConfig<string>("AllSpecTableXsd", "allSpecTable.xsd");
        public static readonly string NewXml = GetConfig<string>("NewXml", "New.xml");
        public static readonly string NewSpecTableXml = GetConfig<string>("NewSpecTableXml", "newSpecTable.xml");
        public static readonly string AlignFailTxt = GetConfig<string>("AlignFailTxt", "alignFail.txt");
        public static readonly string PerformanceParametersDat = GetConfig<string>("PerformanceParametersDat", "PerformanceParameters.dat");
        public static readonly string TesterModuleDat = GetConfig<string>("TesterModuleDat", "TesterModule.dat");

        // HICAP files
        public static readonly string AutoTeachBladeINI = GetConfig<string>("AutoTeachBladeINI", "c:\\HicapTest\\AutoTeachBlade.ini");
        public static readonly string AutoTeachBladeAlignerINI = GetConfig<string>("AutoTeachBladeAlignerINI", "c:\\HicapTest\\AutoTeachBladeAligner.ini");
        public static readonly string AutoTeachTrayINI = GetConfig<string>("AutoTeachTrayINI", "c:\\HicapTest\\AutoTeachTray.ini");
        public static readonly string ErrorFlagINI = GetConfig<string>("ErrorFlagINI", "c:\\HicapTest\\ErrorFlag.ini");
        public static readonly string StopTXT = GetConfig<string>("StopTXT", "c:\\Stop.txt");
        public static readonly string JadePlusINI = GetConfig<string>("JadePlusINI", "C:\\JadeData\\BladePCNum#.txt");
        public static readonly string TDAcsvPath = GetConfig<string>("TDAcsvPath", @"C:\JadeData\temp\");
        public static readonly string TDAcsvSrvPath = GetConfig<string>("TDAcsvPath", @"C:\JadeData\srv\");
        public static readonly string TDAcsvTemPath = GetConfig<string>("TDAcsvPath", @"C:\JadeData\temp\");
        public static readonly string HicapCountersPath = GetConfig<string>("HicapCountersPath", @"C:\Counters\");
        public static readonly string safeEjectINI = GetConfig<string>("safeEjectINI", @"C:\JadeData\BladeEjected.ini");
        public static readonly string PromptUserINI = GetConfig<string>("PromptUserINI", @"C:\JadeAutomationScripts\TCLUtils\prompt.ini");

        // WD files
        public static readonly string WdSliderLogs = GetConfig<string>("WdSliderLogs", "WdSliderLogs");
    
        // BladeRunner Files
        public static readonly string ErrorCodeMap = GetConfig<string>("ErrorCodeMap", "ErrorCodeMap.xml");
        public static readonly string weBeDoneString = GetConfig<string>("weBeDoneString", "thE1prOcwAfInnedeSu");


        // Local Password database path.
        //public static readonly string PasswordDatFile = GetConfig<string>("PasswordDatFile", @"c:\Program Files\Hitachi\Tester\PasswordManager\TesterServerPassword.dat");
        // Erwin's Password database path.
        //public static readonly string PasswordDatFile = GetConfig<string>("PasswordDatFile", @"\\hchasspspecs.hicapmfg.mfg.hgst.com\DETJade\password\TesterServerPassword.dat");
        // Raffy's Password database path.
        //public static readonly string PasswordDatFile = GetConfig<string>("PasswordDatFile", @"ftp://phwindbprd05.hitachigst.global/jade/GUI/TesterServerPassword.dat");
        public static readonly string PasswordUserName = GetConfig<string>("PasswordUserName", "jadeuser01");
        public static readonly string PasswordPassword = GetConfig<string>("PasswordPassword", "Usr01jade");

       
       public static readonly string MisTestChar = GetConfig<string>("MisTestChar", "b");

       public static readonly string MinInputTrays = GetConfig<string>("MinInputTrays", "MinInputTrays"); 
       public static readonly string MaxInputTrays = GetConfig<string>("MaxInputTrays", "MaxInputTrays");
       public static readonly string MinSliders = GetConfig<string>("MinSliders", "MinSliders");
        public static readonly string MinBlades = GetConfig<string>("MinBlades", "MinBlades");
       

       public static readonly string SeqTimeout = GetConfig<string>("SeqTimeout", "SeqTimeout");
        public static readonly string OffLine = GetConfig<string>("Off-Line", "Off-Line");
        public static readonly string OnLine = GetConfig<string>("On-Line", "On-Line");

        public static readonly string InvalidTrayMap = GetConfig<string>("InvalidTrayMap", "InvalidTrayMap");

        public static readonly string SiteList = GetConfig<string>("SiteList", "SJ,PHO,THO,SZ,HB,ATE,FUJI,WDB,HICAP");
        public static readonly string SiteHicap = GetConfig<string>("SiteHicap", "PHO,HB,HICAP");
        //public static readonly string SiteSanJose = GetConfig<string>("SiteSanJose", "SJ");

        public static readonly string PickFromTrayErrorCode = GetConfig<string>("PickFromTrayErrorCode", "8400");
        public static readonly string PickFromBladeErrorCode = GetConfig<string>("PickFromBladeErrorCode", "8500");

        public static readonly string OCRCheck = GetConfig<string>("OCRCheck", "OCRCheck");
        public static readonly string UpCheck = GetConfig<string>("UpCheck", "UpCheck");
        public static readonly string DnCheck = GetConfig<string>("DnCheck", "DnCheck");
        public static readonly string ExpChange = GetConfig<string>("ExpChange", "ExpChange::");

        public static readonly string SkipItSkipIt = GetConfig<string>("SkipItSkipIt", "SkipItSkipIt");
       
       private static T GetConfig<T>(string key, T defaultValue)
       {
          if (ConfigurationManager.AppSettings[key] == null) return defaultValue;
          try
          {
              return (T)Convert.ChangeType(ConfigurationManager.AppSettings[key], typeof(T));
          }
          catch (System.FormatException) { return defaultValue; }
          catch (System.NullReferenceException) { return defaultValue; }
          catch (System.OverflowException) { return defaultValue; }
          catch (System.InvalidCastException) { return defaultValue; }
          catch (System.Exception e) { throw e; }
       }
    }
}
