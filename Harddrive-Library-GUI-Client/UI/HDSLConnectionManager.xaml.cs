using HDDLC.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace HDDLC.UI
{
    /// <summary>
    /// Interaction logic for HDSLConnectionManager.xaml
    /// </summary>
    public partial class HDSLConnectionManager : UserControl
    {
        #region SelectedConnection

        /// <summary>
        /// SelectedConnection Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedConnectionProperty =
            DependencyProperty.Register("SelectedConnection", typeof(HDSLConnection), typeof(HDSLConnectionManager),
                new FrameworkPropertyMetadata((HDSLConnection)null,
                    new PropertyChangedCallback(OnSelectedConnectionChanged)));

        /// <summary>
        /// The currently selected connection
        /// </summary>
        public HDSLConnection SelectedConnection
        {
            get { return (HDSLConnection)GetValue(SelectedConnectionProperty); }
            set { SetValue(SelectedConnectionProperty, value); }
        }

        /// <summary>
        /// Handles changes to the SelectedConnection property.
        /// </summary>
        private static void OnSelectedConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HDSLConnectionManager target = (HDSLConnectionManager)d;
            HDSLConnection oldSelectedConnection = (HDSLConnection)e.OldValue;
            HDSLConnection newSelectedConnection = target.SelectedConnection;
            target.OnSelectedConnectionChanged(oldSelectedConnection, newSelectedConnection);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the SelectedConnection property.
        /// </summary>
        protected virtual void OnSelectedConnectionChanged(HDSLConnection oldSelectedConnection, HDSLConnection newSelectedConnection)
        {
        }

        #endregion

        #region Connections

        /// <summary>
        /// Connections Dependency Property
        /// </summary>
        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.Register("Connections", typeof(ObservableCollection<HDSLConnection>), typeof(HDSLConnectionManager),
                new FrameworkPropertyMetadata(new ObservableCollection<HDSLConnection>(),
                    new PropertyChangedCallback(OnConnectionsChanged)));

        /// <summary>
        /// The set of managed connections
        /// </summary>
        public ObservableCollection<HDSLConnection> Connections
        {
            get { return (ObservableCollection<HDSLConnection>)GetValue(ConnectionsProperty); }
            set { SetValue(ConnectionsProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Connections property.
        /// </summary>
        private static void OnConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HDSLConnectionManager target = (HDSLConnectionManager)d;
            ObservableCollection<HDSLConnection> oldConnections = (ObservableCollection<HDSLConnection>)e.OldValue;
            ObservableCollection<HDSLConnection> newConnections = target.Connections;
            target.OnConnectionsChanged(oldConnections, newConnections);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Connections property.
        /// </summary>
        protected virtual void OnConnectionsChanged(ObservableCollection<HDSLConnection> oldConnections, ObservableCollection<HDSLConnection> newConnections)
        {
        }

        #endregion

        public HDSLConnectionManager()
        {
            InitializeComponent();
        }

        private void Add_Connection(object sender, RoutedEventArgs e)
        {
            var connection = new HDSLConnection();
            connection.WasDeleted += Connection_WasDeleted;
            Connections.Add(connection);
        }

        private void Connection_WasDeleted(HDSLConnection target)
        {
            target.WasDeleted -= Connection_WasDeleted;
            Connections.Remove(target);
        }

        private void Remove_Connection(object sender, RoutedEventArgs e)
        {
            if (SelectedConnection != null)
            {
                Connections.Remove(SelectedConnection);
            }
        }
    }
}
