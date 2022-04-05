using HDDL.Language.HDSL.Results;
using HDDLC.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
    /// Interaction logic for QueryViewPanelDisplay.xaml
    /// </summary>
    public partial class QueryViewPanelDisplay : UserControl
    {
        public QueryViewPanelDisplay()
        {
            InitializeComponent();

            //DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if (e.OldValue != null)
            //{
            //    (e.OldValue as ObservableCollection<HDSLRecord>).CollectionChanged -= DataRefreshed;
            //}

            //var records = (DataContext as QueryViewPanel)?.Records;
            //if (records != null)
            //{
            //    // generate the columns and bind to the event
            //    dgData.Columns.Clear();
            //    var first = records.FirstOrDefault();
            //    if (first != null)
            //    {
            //        foreach (var colItem in first.Data)
            //        {
            //            // Create Bound Columns
            //            var col = new DataGridTextColumn();
            //            col.Header = colItem.Column;
            //            col.Binding = new Binding("Data");
            //            dgData.Columns.Add(col);
            //        }
            //    }

            //    records.CollectionChanged += DataRefreshed;
            //}
        }

        private void DataRefreshed(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            
        }
    }
}
