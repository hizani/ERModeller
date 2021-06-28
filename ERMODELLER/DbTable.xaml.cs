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
using System.Data;
using System.ComponentModel;

namespace ERMODELLER
{
    /// <summary>
    /// Таблица баз данных
    /// </summary>

    public partial class DbTable : UserControl
    {
        public DbTableModel TableModel { get; set; }

        public DbTable(DbTableModel tableModel)
        {
            InitializeComponent();
            dgTable.AutoGeneratingColumn += dgPrimaryGrid_AutoGeneratingColumn;

            this.TableModel = tableModel;

            dgTable.ItemsSource = this.TableModel.Attributes;
            dgTable.CanUserSortColumns = false;

            tbTableName.Text = tableModel.TableName;

            Loaded += delegate
            {
                TableModel.Width = ActualHeight;
                TableModel.Height = ActualHeight;
                ((MainWindow)Application.Current.MainWindow).RedrawArrows();
            };

            dgTable.Items.Refresh();
        }

        private void MenuItem_EditTable_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            List<DbTableModel> tablesData = new List<DbTableModel>();

            var tables = ((MainWindow)Application.Current.MainWindow).CanvasMain.Children.OfType<DbTable>().ToList();

            for (int i = 0; i < tables.Count(); i++)
                tablesData.Add(tables[i].TableModel);

            ((MainWindow)Application.Current.MainWindow).OverlayEditTable.Visibility = Visibility.Visible;
            ((EditTablePage)((MainWindow)Application.Current.MainWindow).Frame_EditPageLoader.Content).OpenPage(tablesData, this);
        }

        public void ChangeTable(List<DbTableItem> attrs, string name, List<DbTableModel> connections)
        {
            this.TableModel.Attributes = attrs;
            this.TableModel.Connections = connections;
            dgTable.ItemsSource = this.TableModel.Attributes;
            dgTable.CanUserSortColumns = false;
            this.TableModel.TableName = name;
            this.tbTableName.Text = this.TableModel.TableName;

            dgTable.Items.Refresh();
        }

        private void MenuItem_DeleteTable_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).DeleteTable(TableModel, this);
        }

        private void dgPrimaryGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var desc = e.PropertyDescriptor as PropertyDescriptor;
            var att = desc.Attributes[typeof(ColumnAttributeName)] as ColumnAttributeName;
            if (att != null)
            {
                e.Column.Header = att.Name;
            }
        }

        private void MenuItem_ClipboardCopy_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetData(DataFormats.Text, (Object)TableModel.GenerateCreateTableScript());
        }
    }
}