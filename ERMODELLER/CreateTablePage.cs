using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ERMODELLER
{
    internal class CreateTablePage : TablePageBase
    {
        /// <summary>
        /// Положение новой таблицы
        /// </summary>
        private Point newTablePos;

        public CreateTablePage() : base()
        {
            bSubmitTable.Content = "Создать";
        }

        public override void Cancel()
        {
            ((MainWindow)Application.Current.MainWindow).OverlayCreateTable.Visibility = Visibility.Collapsed;
        }

        public override void OpenPage(List<DbTableModel> foreignTables, DbTable editableTable = null, Point newTablePos = new Point())
        {
            tableItems = new List<DbTableItem>();
            this.foreignTables = new List<DbTableModel>();
            this.newTablePos = newTablePos;
            existedTables = foreignTables;
            dgMain.ItemsSource = tableItems;
            dgMain.Items.Refresh();
            TableName.Text = "table_name";
            tbColumnName.Text = "";
            tbFKName.Text = "";
            cbColumnType.SelectedIndex = 0;
            cbForeignKey.SelectedIndex = 0;
            cbForeignTable.SelectedIndex = 0;
            PkCheckbox.IsChecked = false;
            UqCheckbox.IsChecked = false;
            NotNullCheckbox.IsChecked = false;
            FkPkCheckbox.IsChecked = false;
            FkUqCheckbox.IsChecked = false;

            cbForeignTable.ItemsSource = foreignTables;
            cbForeignTable.Items.Refresh();
            cbForeignTable.DisplayMemberPath = "TableName";
            cbForeignTable.SelectedValuePath = "TableName";
            cbForeignTable.SelectedIndex = 1;

            //Если нет других таблиц, то добавление внешнего ключа недоступно
            if (cbForeignTable.Items.IsEmpty)
            {
                cbForeignTable.IsEnabled = false;
                cbForeignKey.IsEnabled = false;
                bAddFK.IsEnabled = false;
                return;
            }
            cbForeignTable.IsEnabled = true;
            cbForeignKey.IsEnabled = true;
            bAddFK.IsEnabled = true;
            SetForeignKeysCombobox();
        }

        public override void Submit()
        {
            if (dgMain.Items.Count <= 0)
                return;
            if (!CheckName(TableName))
                return;

            foreach (var tb in existedTables)
                if (TableName.Text == tb.TableName)
                {
                    MessageBox.Show("Такая таблица уже существует", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            ((MainWindow)Application.Current.MainWindow).AddTable(newTablePos, TableName.Text, tableItems, foreignTables);
            ((MainWindow)Application.Current.MainWindow).OverlayCreateTable.Visibility = Visibility.Collapsed;
            ((MainWindow)Application.Current.MainWindow).RedrawArrows();
        }
    }
}