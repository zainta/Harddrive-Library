using HDDL.Web;
using HDDLC.Data;
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

namespace HDDLC.UI
{
    /// <summary>
    /// Interaction logic for HDSLEditor.xaml
    /// </summary>
    public partial class HDSLEditor : UserControl
    {
        #region ShowAdvancedMode

        /// <summary>
        /// ShowAdvancedMode Dependency Property
        /// </summary>
        public static readonly DependencyProperty ShowAdvancedModeProperty =
            DependencyProperty.Register("ShowAdvancedMode", typeof(bool), typeof(HDSLEditor),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnShowAdvancedModeChanged)));

        /// <summary>
        /// Whether or not to display the advanced editor or the basic editor
        /// </summary>
        public bool ShowAdvancedMode
        {
            get { return (bool)GetValue(ShowAdvancedModeProperty); }
            set { SetValue(ShowAdvancedModeProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ShowAdvancedMode property.
        /// </summary>
        private static void OnShowAdvancedModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HDSLEditor target = (HDSLEditor)d;
            bool oldShowAdvancedMode = (bool)e.OldValue;
            bool newShowAdvancedMode = target.ShowAdvancedMode;
            target.OnShowAdvancedModeChanged(oldShowAdvancedMode, newShowAdvancedMode);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the ShowAdvancedMode property.
        /// </summary>
        protected virtual void OnShowAdvancedModeChanged(bool oldShowAdvancedMode, bool newShowAdvancedMode)
        {
            if (ShowAdvancedMode)
            {
                TextRange textRange = new TextRange(khtAdvancedSearchText.Document.ContentStart, khtAdvancedSearchText.Document.ContentEnd);
                _viewer.SearchQuery = textRange.Text;
            }
            else
            {
                _viewer.SearchQuery = txtBasicSearchText.Text;
            }
        }

        #endregion

        private QueryViewer _viewer;

        public HDSLEditor()
        {
            InitializeComponent();

            _viewer = new QueryViewer();

            // bind ShowAdvancedMode to the query viewer
            var b = new Binding("ShowAdvancedMode");
            b.Source = this;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(_viewer, QueryViewer.IsAdvancedQueryProperty, b);

            // bind Connection to the query viewer
            b = new Binding("DataContext");
            b.Source = this;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(_viewer, QueryViewer.ConnectionProperty, b);

            // bind the panels property on the query viewer to the result list view
            b = new Binding("Panels");
            b.Source = _viewer;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(lvResults, ListView.ItemsSourceProperty, b);

            // bind the panels property on the query viewer to the result list view
            b = new Binding("IsAdvancedSearchAvailable");
            b.Source = _viewer;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(btnInitiateAdvancedSearch, Button.IsEnabledProperty, b);

            DataContextChanged += HDSLEditor_DataContextChanged;
        }

        private void HDSLEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var connection = DataContext as HDSLConnection;
            if (connection?.IsValid == true)
            {
                _viewer.Connection = connection;
            }
            else
            {
                _viewer.Connection = null;
            }
        }

        private void OnAdvancedSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (ShowAdvancedMode)
            {
                TextRange textRange = new TextRange(khtAdvancedSearchText.Document.ContentStart, khtAdvancedSearchText.Document.ContentEnd);
                _viewer.SearchQuery = textRange.Text;
            }
        }

        private void OnBasicSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (!ShowAdvancedMode)
            {
                _viewer.SearchQuery = txtBasicSearchText.Text;
            }
        }

        private void InitiateAdvancedSearch_Click(object sender, RoutedEventArgs e)
        {
            _viewer.ExecuteQuery();
        }
    }
}
