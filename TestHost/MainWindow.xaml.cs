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

using System.ServiceModel;
using Hitachi.Tester.Module;

namespace TestHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TestStart();
        }

        ServiceHost serHost = null;
        ServiceHost streamHost = null;

        public void TestStart()
        {
            ITesterObject iTestObject = new TesterObject();
            serHost = new ServiceHost(iTestObject);
            serHost.Open();

            ITesterObjectStreaming iTestObjectStreaming = new TesterObjectStreaming();
            streamHost = new ServiceHost(iTestObjectStreaming);
            streamHost.Open();
        }
    }
}
