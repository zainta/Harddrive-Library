using HDDLC.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HDDLC.UI
{
    /// <summary>
    /// Interaction logic for HDSLConnectionEditor.xaml
    /// </summary>
    public partial class HDSLConnectionEditor : UserControl
    {
        private Timer _validationDelayTimer;

        public HDSLConnectionEditor()
        {
            InitializeComponent();

            _validationDelayTimer = new Timer(300);
            _validationDelayTimer.Enabled = false;
            _validationDelayTimer.AutoReset = false;
            _validationDelayTimer.Elapsed += TimerTriggerDoValidation;
        }

        private void TimerTriggerDoValidation(object sender, ElapsedEventArgs e)
        {
            _validationDelayTimer.Stop();

            Dispatcher.Invoke(() =>
            {
                var connection = DataContext as HDSLConnection;
                if (connection != null)
                {
                    connection.PerformValidation();
                }
            });
        }

        private void Address_TextChanged(object sender, TextChangedEventArgs e)
        {
            var connection = DataContext as HDSLConnection;
            if (connection != null)
            {
                connection.IsValid = null;
                connection.NeedsValidation = true;

                _validationDelayTimer.Stop();
                _validationDelayTimer.Start();
            }
        }

        private void DeleteHDSLConnection(object sender, RoutedEventArgs e)
        {
            var connection = DataContext as HDSLConnection;
            connection.Delete();
        }

        private void RefreshDSLConnection(object sender, RoutedEventArgs e)
        {
            _validationDelayTimer.Start();
        }
    }
}
