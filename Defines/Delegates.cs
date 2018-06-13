using System;
using System.Collections.Generic;
using System.Text;

namespace Hitachi.Tester
{
    public class Delegates
    {
        // Generic delegates.  .NET Framework 2.0 provides Action<T1> only.
        // Most of then are available with .NET Framework 3.0 or later
        public delegate void Action();
        public delegate void Action<T1>(T1 arg1);
        public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
        public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
        public delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public delegate void Action<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

        public delegate TResult Func<TResult>();
        public delegate TResult Func<T1, TResult>(T1 arg1);
        public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
        public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
        public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    }
}


//Color, string, bool, bool, bool, Exception