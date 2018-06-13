using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hitachi.Tester.Module
{
    public class BladeEventArgs : EventArgs
    {
        #region Fields
        public int Revision;
        public int EventType;

        //BladeEventArgs               //StatusEvent   StartedEvent  CompletedEvent
        protected int _Type;                //iType,    iStartTest,   testNumber
        protected int _ConsecutiveCount;  //consec     consec       consec
        protected int _Int3;                //                        testcount
        protected string _Str1;             //Text,     SeqName,      Text 
        protected string _Str2;             //          GradeName,     
        protected bool _Bool1;
        #endregion Fields

        #region Constructors
        public BladeEventArgs(int eventType, int type, int consecutiveCount, int int3, string str1, string str2, bool bool1)
        {
            Revision = 2;
            EventType = eventType;

            _Type = type;
            _ConsecutiveCount = consecutiveCount;
            _Int3 = int3;
            _Str1 = str1;
            _Str2 = str2;
            _Bool1 = bool1;
        }

        public BladeEventArgs()
        {
            Revision = 2;
            EventType = BladeEventType.None;

            _Type = 0;
            _ConsecutiveCount = 0;
            _Int3 = 0;
            _Str1 = "";
            _Str2 = "";
            _Bool1 = false;
        }
        #endregion Constructors

        #region Properties
        public string Text
        {
            get
            {
                return _Str1;
            }
            set
            {
                _Str1 = value;
            }
        }

        public int Type
        {
            get
            {
                return _Type;
            }
            set
            {
                _Type = value;
            }
        }

        public int Consecutive
        {
            get
            {
                return _ConsecutiveCount;
            }
            set
            {
                _ConsecutiveCount = value;
            }
        }
        #endregion Properties

        #region Methods

        #endregion Methods
    }

    public class CompletedEventBladeArgs : BladeEventArgs
    {

    }

    public class SequenceStartedBladeEventArgs : BladeEventArgs
    {

    }

    public class StatusEventBladeArgs : BladeEventArgs
    {

    }

    public class BladeEventType
    {
        // Revision=1
        public const int None = 0;
        public const int TestStarted = 5;
        public const int TestCompleted = 6;
        public const int SequenceStarted = 7;
        public const int SequenceAborting = 8;
        public const int SequenceCompleted = 9;
        public const int SequenceUpdate = 10;
        public const int ProgramClosing = 12;
        public const int Bunny = 13;
        public const int Status = 14;

        /// <summary>
        /// Parse int value to string name.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static public bool TryParse(int value, out string name)
        {
            try
            {
                name = Parse(value);
                return true;
            }
            catch
            {
                name = string.Empty;
            }
            return false;
        }

        /// <summary>
        /// Parse int value to string name of event type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static private string Parse(int value)
        {
            string name;
            switch (value)
            {
                case None:
                    name = "None";
                    break;
                case TestStarted:
                    name = "TestStarted";
                    break;
                case TestCompleted:
                    name = "TestCompleted";
                    break;
                case SequenceStarted:
                    name = "SequenceStarted";
                    break;
                case SequenceAborting:
                    name = "SequenceAborting";
                    break;
                case SequenceCompleted:
                    name = "SequenceCompleted";
                    break;
                case SequenceUpdate:
                    name = "SequenceUpdate";
                    break;
                case ProgramClosing:
                    name = "BladeEventType";
                    break;
                case Bunny:
                    name = "Bunny";
                    break;
                case Status:
                    name = "Status";
                    break;
                default:
                    throw new ArgumentException("Cannot parse input to BladeEventType.");
                    //break;
            } // end switch

            return name;
        } // end TryParse

    } // end class
}

