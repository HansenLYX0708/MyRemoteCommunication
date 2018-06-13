using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Defines
{
   public partial class MessageBoxWith : Form
   {
      public MessageBoxWith()
      {
         InitializeComponent();
      }

      public MessageBoxWith(string message, string caption)
      {
         InitializeComponent();
         Text = caption;
         Message = message;
      }

      public string Caption
      {
         get
         {
            return this.Text;
         }
         set
         {
            Text = value;
         }
      }

      public string Message
      {
         get
         {
            return textBoxMessage.Text;
         }
         set
         {
            textBoxMessage.Text = value;
         }
      } 









   }
}