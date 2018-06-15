using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            OnStart();
        }

        ServiceHost serviceHost = null;
        protected void OnStart()
        {
            serviceHost = new System.ServiceModel.ServiceHost(typeof(Hitachi.Tester.Module.TesterObject));
            if (serviceHost.State != System.ServiceModel.CommunicationState.Opened)
            {
                serviceHost.Open();
            }
        }

    }
}
