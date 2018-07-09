using System;

namespace Module.Blade
{
    // The following sections serve callbacks.
    #region EventHander
    public delegate void TextReceivedEventHandler(object sender, TextReceivedEventArgs e);
    public delegate void ParametricResultReceivedEventHandler(object sender, ParametricResultReceivedEventArgs e);
    public delegate void ResultReceivedEventHandler(object sender, ResultReceivedEventArgs e);
    public delegate void BladeEventReceivedEventHandler(object sender, BladeEventReceivedEventArgs e);
    #endregion EventHander

    #region EventArgs
    public class TextReceivedEventArgs : EventArgs
    {
        public string Text { get; protected set; }
        public TextCategory Category { get; protected set; }
        public TextReceivedEventArgs(string text, TextCategory category)
        {
            Text = text;
            Category = category;
        }
    }

    public class ParametricResultReceivedEventArgs : EventArgs
    {
        public string Result;
    }

    public class ResultReceivedEventArgs : EventArgs
    {
        public string Result;
    }

    public class BladeEventReceivedEventArgs : EventArgs
    {
        public string Name;
        public string SubName;
        public string Value;
    }
    #endregion EventArgs
}
