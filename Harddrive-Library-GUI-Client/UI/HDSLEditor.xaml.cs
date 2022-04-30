using HDDL.UI.WPF.Converters;
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

            // bind the panels property to the busy pane's visibility
            b = new Binding("IsBusy");
            b.Source = _viewer;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            b.Converter = new HDDL.UI.WPF.Converters.BooleanToVisibilityConverter();
            BindingOperations.SetBinding(bpBusy, UserControl.VisibilityProperty, b);

            // bind the panels property to the basic editor's enabled property
            b = new Binding("IsBusy");
            b.Source = _viewer;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            b.Converter = new BooleanInversionConverter();
            BindingOperations.SetBinding(txtBasicSearchText, UserControl.IsEnabledProperty, b);

            // bind the panels property to the basic editor's enabled property
            b = new Binding("IsBusy");
            b.Source = _viewer;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            b.Converter = new BooleanInversionConverter();
            BindingOperations.SetBinding(khtAdvancedSearchText, UserControl.IsEnabledProperty, b);
            
            _viewer.Panels.CollectionChanged += Panels_CollectionChanged;

            DataContextChanged += HDSLEditor_DataContextChanged;
        }

        /// <summary>
        /// Updates the QueryViewer's connection
        /// </summary>
        private void UpdateConnectionValidationState()
        {
            var connection = DataContext as HDSLConnection;
            connection.PropertyChanged += Connection_PropertyChanged;
            if (connection?.IsValid == true)
            {
                _viewer.Connection = connection;
            }
            else
            {
                _viewer.Connection = null;
            }
        }

        private void HDSLEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null && e.OldValue is HDSLConnection)
            {
                HDSLConnection conn = e.OldValue as HDSLConnection;
                conn.PropertyChanged -= Connection_PropertyChanged;
            }

            UpdateConnectionValidationState();
        }

        private void Connection_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateConnectionValidationState();
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

        private void Panels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            lvResults.SelectedIndex = 0;
        }
    }
}
