// ****************************************************************
// This class lets us communicate between the server and the remote object.
// This came from the CodeProject.
//
// Hitachi Parametrics tester
// Copyright 2007
// Robert L. Kimball
// ****************************************************************
using System;

namespace Hitachi.Tester.Module
{

    public class StaticServerTalker
    {
        public static IObserver Observer;

        private StaticServerTalker()
        {

        }

        public static void Attach(IObserver observer)
        {
            Observer = observer;
        }
    
        /// <summary>
        /// Calls exit and cleanup routine.
        /// </summary>
        /// <param name="thisOne"></param>
        public static void OnExitDelegate(Delegate thisOne)
        {
            Observer.OnExitDelegate(thisOne);
        }

        /// <summary>
        /// Sends messages to server status Window.
        /// </summary>
        public static string MessageString
        {
            set
            {
                try
                {
                    Observer.Notify(value);
                }
                catch
                { }
            }
        }
        /// <summary>
        /// Sends messages to server status Window.
        /// </summary>
        public static string MessageStringContent
        {
            set
            {
                Observer.NotifyContent(value);
            }
        }

       public static string getCurrentCultureString(string whichStr)
       {
           try
           {
               return Observer.GetCurrentCultureString(whichStr);
           }
           catch
           {
               return whichStr;
           }
       }

       public static Hitachi.Tester.Enums.ReturnValues LogInValueProp
       {
          set
          {
             Observer.LogInValueProp = value;
          }
       }

       public static void BladeEvent(object[] BladeEventArgsObject)
       {
           if(Observer!= null) Observer.BladeEvent(BladeEventArgsObject);
       }

       private static string Line1 = "";
       private static string Line2 = "";

       /// <summary>
       /// Sends Text to LCD one line at a time.
       /// Scrolls first line to second line each call.
       /// </summary>
       /// <param name="Text"></param>
       public static void SendLineToLCD(string Text)
       {
           if (Text.Length > 16)
           {
               Text = Text.Substring(0, 16);
           }
           else
           {
               Text = Text.PadRight(16);
           }
           Line1 = Line2;
           Line2 = Text;
           SetLCDtext(Line1 + Line2);
       }

       public delegate int IntIntStringDelegate(int device, string text);
       public static event IntIntStringDelegate SendTextToLcdEvent;

       /// <summary>
       /// Set LCD text.
       /// </summary>
       /// <param name="state"></param>
       private static void SetLCDtext(string text)
       {
           IntIntStringDelegate handler = SendTextToLcdEvent;
           // send message to bunny
           if (handler != null)
           {
               handler(0, text);
           }
       }

    

    }
}
