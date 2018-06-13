using System;
using System.Collections.Generic;
using System.Text;

namespace Hitachi.Tester.Enums
{
    public enum BladeExceptionCode
    {
        TclshLaunchFailure = 10001,
        Undefined = 0
    }

    /// <summary>
    /// Emun for blade location structure (record) in database.
    /// </summary>
    public enum EnumBladeLocation
    {
        JADESN,
        BLADESN,
        TYPE,
        BLADENO,
        COUNTSSTRING,
        COUNT
    }

    /// <summary>
    /// Enum for test names structure (record) on database.
    /// </summary>
    public enum EnumTestNames
    {
        PRODUCTCODE,
        EC,
        EXPERIMENT,
        SEQUENCENAME,
        SEQUENCEREV,
        GRADEFILENAME,
        BINREV,
        OCRREV,
        GRADEREV,
        DISPOREV,
        RETRYREV,
        TESTCNTREV,
        TRAYDISPOREV,
        FACTFILENAME,
        FACTREV,
        FIRMFILENAME,
        FIRMREV,
        JASFILENANE,
        JASREV,
        ADDCOMPFILE,
        ADDCOMPREV,
        BLADETYPE,
        TCLSTART,
        COUNT
    }

    public enum EnumGradeFileVers
    {
        BINREV,
        OCRREV,
        GRADEREV,
        DISPOREV,
        RETRYDISPOREV,
        TESTCNTREV,
        TRAYDISPOREV,
        COUNT
    }

    /// <summary>
    /// Enum for BinObject (in Grading file).
    /// </summary>
    public enum EnumBins
    {
        BINNAME,
        IMAGE,
        COMMENT,
        BIN,
        MATCHBIN,
        SKIPVALUE,
        OCRFLAG,
        PRFLAG,
        DISPOSITION,
        COUNT
    }

    /// <summary>
    /// Enum for grade limits (withname).
    /// </summary>
    public enum GradeTestObjectRankEnum
    {
       RANKNAME,
       LOWER,
       UPPER,
       FUNCTION,
       COUNT
    }

    /// <summary>
    /// Enum for grade limits (without name).
    /// </summary>
    public enum SpecListRankSetElements
    {
       LOWER,
       UPPER,
       FUNCTION,
       COUNT
    }

    public enum GradeTestObjectCommonEnum
    {
        NAME,
        IMAGE,
        COMMENT,
        FUNCTION,
        UNITS,
        TYPES,
        USE,
        TABLENO,
        COUNT
    }

    public enum GradeRankObjectEnum
    { 
        NAME,
        IMAGE,
        COMMENT,
        RANK,
        TABLE,
        INGRADE,
        INERRORCODE,
        RUNNUMBER,
        OUTRANK,
        OUTGRADE,
        OUTERRORCODE,
        PREVALENCE,
        DISPOSITION,
        COUNT
    }

    public enum RetryColumnEnum
    {
       Name,
       Image,
       Comment,
       Rank,
       TableNo,
       ErrorCode,
       Grade,
       RunNum,
       SpecTable,
       SeqTable,
       Disposition,
       COUNT
    }


    public enum OcrRankObjectEnum
    {
        RANK,
        IMAGE,
        COMMENT,
        LOWER,
        UPPER,
        RETRIES,
        TCLSCRIPT,
        DISPOSITION,
        COUNT
    }

    public enum DispositionsBinEnum
    {
        Test,
        Sort,
        Skip,
        TestAndReturn,
        None
    }

    public enum DispositionsSkipEnum
    {
        Test,
        SkipTest,
        Scrap,
        None
    }

    public enum DispositionsOcrEnum
    {
        Retry,
        UseDB,
        UseOCR,
        Manual,
        AssignAt,
        Scrap,
        None
    }

   /// <summary>
   /// Specifies the dispositions for sliders
   /// This is used in the rank disposition section of the grade/spec file.
   /// These are the items that populate the drop down box when editing grade/spec files.
   /// </summary>
   public enum DispositionsRankEnum
   {
      /// <summary>
      /// For BCF sliders.
      /// </summary>
      BCF,       
      /// <summary>
      /// For BCF sliders.
      /// </summary>
      BCF_Scrap,  
      /// <summary>
      /// For mis-test sliders, GUI will decide what to do.
      /// </summary>
      Decide,     
      /// <summary>
      /// For NG sliders.
      /// </summary>
      Scrap,      
      /// <summary>
      /// Indicates that this slider has been processed as a "Skip" slider.
      /// </summary>
      Skipped,    
      /// <summary>
      /// Specifies that this slider has been sorted.
      /// </summary>
      Sorted,     
   
       /// <summary>
       /// Specifies a missing measurement.
       /// </summary>
       Missing,

      /// <summary>
      /// Specifies that this slider has been tested/
      /// </summary>
      Tested,     // For tested sliders 
   }

   /// <summary>
   /// Predefined Retry dispositions.
   /// </summary>
   public enum DispositionsRetryEnum
   {
      /// <summary>
      /// Open and close MEMS clamp then retry.
      /// </summary>
      RetryClamp, 
      /// <summary>
      /// Retry on another Blade.
      /// </summary>
      RetryMove,  
      /// <summary>
      /// Retry Sequence immediately - do not do anything just retry.
      /// </summary>
      RetryNow,   
      /// <summary>
      /// Pick-up, Align Put down (Any blade).
      /// </summary>
      RetryAny,   
      /// <summary>
      /// This specifies Initial pretest condition.
      /// </summary>
      Initial,    
      /// <summary>
      /// For tested sliders.
      /// </summary>
      Tested,      
   }

    public enum SliderStatusEnum
    {
        Initial,
        Tested,
        Retry,
        Sorted
    }

   /// <summary>
   /// Predefined Sequence file table names.
   /// </summary>
   public enum SequenceTableTypeStringsEnum
   {
      Prime,
      Retest,
      BCF,
      Skip,
      Reclamp
   }

   /// <summary>
   /// Predefined Spec/Grade table names.
   /// </summary>
   public enum SpecTableTypeStringsEnum
   {
      Prime,
      Retest,
      BCF,
      Skip,
      Sort,
   }

    /// <summary>
    /// Specifies the major sections of the Grade/Spec XML file.
    /// Except date, each of these are one of the tabs.
    /// These are like tables in a DB.
    /// </summary>
    public enum SpecListTopItemEnum
    {
        BINDISPO,
        OCRDISPO,
        GRADESPEC,  //List of GradeTestObjects
        GRADEDISPO,
        RETRDISPOY,
        LIMITS,
        TRAYDISPO,
        COUNT
    }

    /// <summary>
    /// Every item in the Grade file has these.
    /// </summary>
    public enum SpecListAttribute
    {
       Name,
       Image,
       Comment,
       COUNT
    }

    public enum TestCountColEnum
    {
        Name,
        Image,
        Comment,
        Unit,
        Limit,
        Counter,
        Script,
        COUNT
    }

    public enum DispositionsLimitsEnum
    {
        Stop,
        Continue,
        None
    }

    public enum TrayDispoColEnum
    {
       Name,
       Image,
       Comment,
       TrayType,
       SliderType,
       LessThan,
       GreaterThan,
       Grade,
       ErrorDefectCode,
       Disposition,
       COUNT
    }

   
   public enum TrayMapListViewItems
   {
      //TRAYSN,
      LOCATION,
      TAPELOC,
      SN,
      DBSN,
      OCRSN,
      PRODUCTCODE,
      ECNUMBER,
      EXPERIMENT,
      SLDBIN,
      // JOBBIN,
      GRADE,
      ERRORCODE,
      RUNNUM,
      DEFECT,
      FAILTOPICK,
      SLIDERSTATUS,
      CLASSCODE,
      ROUTING,
      WORKFLOW,
      FUTUREACTION,
      WAFER,
      WAFERFLAG,
      JOBNUMBER,
      TABLE,
      DISPOSITION,
      ORIGTRAY,
      ORIGLOC,
      BUILDCODE,
      COUNT
   }


    /// <summary>
    /// ENUM to index into Listview columns
    /// </summary>
    public enum CatListViewItems
    {
        TRAY,
        TRAYSN,
        CATEGORY,
        PRODUCTCODE,
        EC,
        EXPERIMENT,
        BIN,
        JOBBIN,
        GRADE,
        CLASSCODE,
        ROUTING,
        WORKFLOW,
        FUTUREACTION,
        WAFER,
        WAFERFLAG,
        FROMTRAY,
        FIRST,
        LAST,
        TYPE,
        SIZE,
        BLADEMASK,
        BUILDCODE,
        FACTORY,
        OPERATION,
        SEGMENT,
        TRAVELER,
        JOBNUMBER,
        OWNER,
        STARTREASON,
        HOLDFOR,
        ADJUSTMENTQUANTITY,
        COUNT
    }

    /// <summary>
    /// JadeWindow's Yield display type
    /// </summary>
    public enum YieldDisplayGroup
    {
        Grades,
        Rank,
        ErrorCode,
        General,
        Consecutive,
        Picks,
        DGR,
        COUNT
    }

   public enum YieldDisplayType
   {
      Raw,
      Average,
      RunningAverage,
      SlidingAverage,
      COUNT
   }

    public enum YieldDisplayTypeDgr
    {
        ABA_COMMENT,
        BBA_ID,          // Blade SN
        BLADE_TYPE,
        FLEX_ID,
        ABA_ID,          // Actuator ID
        PCBA_ID,
        MEMS_ID,
        MBA_COMMENT,
        MBA_ID,
        DISK_ID,
        MOTOR_ID,
        COUNTS_COMMENT,
        TEST_COUNT,
        FLAW_SCAN_COUNT,
        PATROL_SEEK_COUNT,
        DISK_LOAD_COUNT,
        MEMS_COUNT,
        MEMS_CONFIG_COMMENT,
        MEMS_TYPE,
        START_POSITION,
        OPEN_POSITION,
        OPEN_VELOCITY,
        OPEN_ACCEL,
        CLOSE_POSITION,
        CLOSE_VELOCITY,
        CLOSE_ACCEL,
        MAINTENANCE_COMMENT,
        BSDC,
        BSDC_DATETIME,
        JADE_ID,
        JADE_POSITION,
        Dgr24Hour,
        DgrOneHour,
        DgrHalfHour,
        DgrProjected,
        COUNT
   }

   public enum BunnyEvents
   {
      UsbReset,    // Sent on Reset 
      Init,
      Uninit,
      Pwr12V,
      Pwr5V,
      Aux0Out,
      Aux1Out,
      MemsOpenClose,
      SolenoidRamp,
      LcdText,
      BackLight,
      FirmwareVer,
      DriverVer,
      BunnyStatus,
      MemsType,
      Neutral,
      Position,
      SetSaveServo,
      MotorBaseSN,
      ActuatorSN,
      DiskSN,
      PcbaSN,
      MemsSN,
      FlexSN,
      MotorSN,
      BladeType,
      BladeSN,
      JadeSN,
      BladeLoc,
      ServoEnable,
      Broke,
      Counts,
      DiskLoadCount,
      TestCount,
      MemsCount,
      ScanCount,
      PatrolCount,
      MemsOpenSensor,
      MemsCloseSensor,
      BunnyFixed,
      MemsOpenDelay,
      MemsCloseDelay,
       Solenoid,
       Aux0In,
       Aux1In,
       VoltageCheckBlade,
       TclPath,
       BladePath,
       FactPath,
       GradePath,
       FirmwarePath,
       ResultPath,
       LogPath,
       DebugPath,
       CountsPath,
       TclStart,
       BladeRunnerPath
   }

   public enum eventInts
   {
       KeepAliveEvent,
      cmdWin,
      cmdWinDone,
      error,
      sequenceResult,
      tclResult,
      eventVal,
      toTv,
      SequenceTimeout,
      CmdFinishedWithError,
      StatisticsValueEvent,
      StatisticsMinMaxEvent,
      StatisticsFromToTimeEvent,
       StatisticsDGREvent,
       Flush,    // Used by TvQueueClass to flush stale items.
       PingStatusEvent,
       Notify,
       NotifyWithContent
   }

   public enum appendStat
   {
      success,
      fail,
      duplicate
   }

   /// <summary>
   /// This ENUM is used by Password manager (and everything that uses Password manager) for a return value.
   /// The low order bits are the permissions that a user has, the upper bits are error codes.
   /// When Jade asks Password Manager for some permission, Password manager will send back a return value
   /// with zero or more of these bits set.
   /// 
   /// The most significant bit is not used because this originally had to work with Visual Basic (which only has signed numbers).
   /// The format is inherited from the former IMES tester.  We could now change this.
   /// 
   /// Likely the errors could be sent back with exceptions instead of return values.
   /// </summary>
   [Flags]
   public enum ReturnValues
   {
       // Access bits (upper bit not set)
      operatorBit = 0x01,
      editSeqBit = 0x02,
      editOcrBit = 0x04,
      editBinBit = 0x08,
      editGradeBit = 0x10,
      editDispositionBit = 0x20,
      editTrayDispoBit = 0x40,
      editTestNamesDbBit = 0x80,
      editBladeLocationDbBit = 0x100,
      automationBit = 0x200,
      bladeAccessBit = 0x400,
      serverAccessBit = 0x800,
      passwordAdminBit = 0x1000,
      engineerBit = 0x2000,
      changedVal = 0x4000,
      editTestCountBit = 0x8000,
      OverrideLockBit = 0x10000,
      EditCategoryBit = 0x20000,
      ClearSpoRpoBit = 0x40000,
      RetryDispoBit = 0x80000,
      SuperMoveBit = 0x100000,
      SliderSetup = 0x200000,
      GraniteQuartz = 0x400000,

       // Errors (upper bit set)
      errorBit = 0x40000000,
      fileBad = 0x60000000,
      fileMissing = 0x50000000,
      passwordBad = 0x48000000,
      userIdBad = 0x44000000,
      locked = 0x42000000,
      tooManyAttempts = 0x41000000,
      connectionBad = 0x40800000,
      noChange = 0x40400000,
      cancel = 0x40200000,
      notAdmin = 0x40100000
      // Sorry sometimes this one is signed int and sometimes unsigned int.  Don't use upper bit.
   }

   /// <summary>
   /// Indicates the MEMS state.
   /// </summary>
   public enum MemsStateValues
   {
      Closed,
      Opened,
       Closing,
       Opening,
      Unknown
   }

   public enum EnableDisable
   {
      Disable,
      Enable
   }

   public enum OnOffValues
   {
      Off,
      On
   }
   public enum moveCallsEnum
   {
      Recovery,
      Home,
      MoveToClear,
      InitJade,
      InitBlade,
      Clear,
      Run,
      MoveToBlade,
      MoveToTray,
      PickerToCamera,
      CleanPicker,
   }

   /// <summary>
   /// columns
   /// </summary>
   public enum LimboListViewItems
   {
      SerialNumber,
      FromTray,
      Location,
      COUNT
   }

   /// <summary>
   /// For CategoryClass count slider func
   /// </summary>
   public enum SliderCountBits
   {
      NONDEFECT = 1,
      DEFECT = 2,
      FAILTOPICK = 4,
      TESTED = 8,
   }

   /// <summary>
   /// For Grade file tray Dispo Slider type Combo box drop down choices.
   /// </summary>
   public enum TrayDispoSliderType
   {
      All,
      NonDefect,
      Defect,
      FailToPick,
   }

   public enum HostToServiceEnums
   {
       Abort,
       CopyFileOnBlade,
       ClearStats,
       hgst_get_servo,
       hgst_get_neutral,
       hgst_set_neutral,
       hgst_move_servo,
       hgst_usb_reset,
       hgst_set_save_servo,
       GetDataViaEvent,
       SetCounts,
       SaveCounts,
       SetIntegers,
       SetMemsCloseSensorType,
       SetMemsOpenSensorType,
       SetMemsType,
       SetStrings,
       GetStrings,
       TclCommand,
       TclInput,
       ZeroCounts,
   }

    /// <summary>
    /// Which column in PasswordManager's ListView do we sort on?
    /// Same spelling as matching DB table columns.
    /// </summary>
   public enum PasswordManagerSortBy
   {
       fullname,
       userName,
   }

   public enum TweakState
   {
       START,
       APPLIED,
       CANCEL
}

} // end namespace
