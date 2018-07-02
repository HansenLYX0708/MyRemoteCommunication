using System;
using System.Threading;

namespace Hitachi.Tester.Module
{
    public class CountsStatsClass : CountsClass
    {
        #region Fields
        public event StatusEventHandler StatisticsValueEvent;
        public event StatusEventHandler StatisticsMinMaxEvent;
        public event StatusEventHandler StatisticsFromToTimeEvent;
        public event StatusEventHandler StatisticsDGREvent;
        private StatsClass theStats;
        #endregion Fields

        #region Constructors
        public CountsStatsClass(string CountsPath, string ApplicationStartupPath)
        {
            theStats = new StatsClass(CountsPath, ApplicationStartupPath);
            theStats.StatisticsMinMaxEvent += new StatusEventHandler(TheStats_StatisticsMinMaxEvent);
            theStats.StatisticsValueEvent += new StatusEventHandler(TheStats_StatisticsValueEvent);
            theStats.StatisticsFromToTimeEvent += new StatusEventHandler(TheStats_StatisticsFromToTimeEvent);
            theStats.StatisticsDGREvent += new StatusEventHandler(TheStats_StatisticsDGREvent);
        }
        #endregion Constructors

        #region Methods
        private void TheStats_StatisticsDGREvent(object sender, StatusEventArgs e)
        {
            ObjectStatusEventArgsClass argObj = new ObjectStatusEventArgsClass(sender, e);
            Thread eventThread = new Thread(SendStatisticsDGREvent)
            {
                IsBackground = true
            };
            eventThread.Start(argObj);
        }

        private void SendStatisticsDGREvent(object argObj)
        {
            ObjectStatusEventArgsClass ArgObj = (ObjectStatusEventArgsClass)argObj;
            StatisticsDGREvent?.Invoke(ArgObj.Sender, ArgObj.Args);
        }

        private void TheStats_StatisticsFromToTimeEvent(object sender, StatusEventArgs e)
        {
            ObjectStatusEventArgsClass argObj = new ObjectStatusEventArgsClass(sender, e);
            Thread eventThread = new Thread(SendStatisticsValueEvent)
            {
                IsBackground = true
            };
            eventThread.Start(argObj);
        }

        private void SendStatisticsFromToEvent(object argObj)
        {
            ObjectStatusEventArgsClass ArgObj = (ObjectStatusEventArgsClass)argObj;
            StatisticsFromToTimeEvent?.Invoke(ArgObj.Sender, ArgObj.Args);
        }

        private void TheStats_StatisticsValueEvent(object sender, StatusEventArgs e)
        {
            ObjectStatusEventArgsClass argObj = new ObjectStatusEventArgsClass(sender, e);
            Thread eventThread = new Thread(SendStatisticsValueEvent)
            {
                IsBackground = true
            };
            eventThread.Start(argObj);
        }

        private void SendStatisticsValueEvent(object argObj)
        {
            ObjectStatusEventArgsClass ArgObj = (ObjectStatusEventArgsClass)argObj;
            StatisticsValueEvent?.Invoke(ArgObj.Sender, ArgObj.Args);
        }

        private void TheStats_StatisticsMinMaxEvent(object sender, StatusEventArgs e)
        {
            ObjectStatusEventArgsClass argObj = new ObjectStatusEventArgsClass(sender, e);
            Thread eventThread = new Thread(SendStatisticsMinMaxEvent)
            {
                IsBackground = true
            };
            eventThread.Start(argObj);
        }

        private void SendStatisticsMinMaxEvent(object argObj)
        {
            ObjectStatusEventArgsClass ArgObj = (ObjectStatusEventArgsClass)argObj;
            StatisticsMinMaxEvent?.Invoke(ArgObj.Sender, ArgObj.Args);
        }

        public override void SetValue(string name, int value)
        {
            base.SetValue(name, value);
            theStats.AddAnItemToFile(name, DateTime.Now, GetValue(name), value);
        }

        public override void IncValue(string name)
        {
            base.IncValue(name);
            theStats.AddAnItemToFile(name, DateTime.Now, GetValue(BladeDataName.TestCount), GetValue(name));
        }

        public void ClearStats()
        {
            theStats.ClearStats();
        }

        public void UpdateStatValuesViaEvent(string what, Int64 from, Int64 to)
        {
            theStats.GetAverage(what, from, to);
        }

        public void UpdateDgrValuesViaEvent(TimeSpan timeSpan)
        {
            theStats.GetDGRValues(timeSpan);
        }

        public void GetFirstLastIndexViaEvent()
        {
            theStats.GetFirstLastIndexViaEvent();
        }

        public void GetFormToViaEvent(Int64 from, Int64 to)
        {
            theStats.StartStopTime(from, to);
        }
        #endregion Methods
    } // end class
} // end namespace
