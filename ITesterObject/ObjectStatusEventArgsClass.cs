using System;
using System.Collections.Generic;
using System.Text;

namespace Hitachi.Tester.Module
{
   public class ObjectStatusEventArgsClass
   {
      public ObjectStatusEventArgsClass()
      {
         Sender = new object();
         Args = new StatusEventArgs();
      }

      public ObjectStatusEventArgsClass(object sender, StatusEventArgs args)
      {
         Sender = sender;
         Args = args;
      }

      public object Sender;
      public StatusEventArgs Args;
   } // end class
} // end namespace
