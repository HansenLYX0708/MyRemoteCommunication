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

using Hitachi.Tester.Client;

namespace TestClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            remoteConnectLib = new RemoteConnectLib();

        }

        RemoteConnectLib remoteConnectLib = null;

        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            remoteConnectLib.Connect("10.113.201.113", string.Empty, string.Empty);
        }
    }
}
