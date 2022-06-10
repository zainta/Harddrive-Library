using HDDL.Language.HDSL.Results;
using HDDLC.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
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

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                var oldPanel = e.OldValue as QueryViewPanel;
                if (oldPanel != null)
                {
                    oldPanel.Refreshed -= DataRefreshed;
                }
            }

            var panel = DataContext as QueryViewPanel;
            if (panel != null)
            {
                panel.Refreshed += DataRefreshed;
                udJumpPage.Value = Convert.ToInt32(panel.CurrentPageIndex);
            }
        }

        private void DataRefreshed(QueryViewPanel sender)
        {
            udJumpPage.Value = Convert.ToInt32(sender.CurrentPageIndex);
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            var panel = DataContext as QueryViewPanel;
            if (panel != null)
            {
                if (udJumpPage.Value.HasValue)
                {
                    panel.SetPage(udJumpPage.Value.Value);
                }
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            var panel = DataContext as QueryViewPanel;
            if (panel != null)
            {
                panel.PreviousPage();
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            var panel = DataContext as QueryViewPanel;
            if (panel != null)
            {
                panel.NextPage();
            }
        }

        private void dgData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var panel = DataContext as QueryViewPanel;
            if (panel != null)
            {
                var mapping = (from m in panel.RelevantMappings where m.Alias == e.PropertyName select m).SingleOrDefault();
                if (mapping != null)
                {
                    if (mapping.Name.ToLower() == "size")
                    {
                        DataGridTemplateColumn sizeColumn = new DataGridTemplateColumn();
                        sizeColumn.Header = mapping.Alias;
                        sizeColumn.CellTemplate = FindResource("SizeColumnCellTemplate") as DataTemplate;
                        e.Column = sizeColumn;
                    }
                    else if (mapping.Name.ToLower() == "path")
                    {
                        DataGridHyperlinkColumn linkColumn = new DataGridHyperlinkColumn();
                        linkColumn.Header = mapping.Alias;
                        linkColumn.Binding = new Binding(mapping.Alias);
                        e.Column = linkColumn;
                    }
                }
            }
        }

        private void dgData_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)e.OriginalSource;
            Process.Start("explorer.exe", link.NavigateUri.AbsoluteUri);            
        }
    }
}
