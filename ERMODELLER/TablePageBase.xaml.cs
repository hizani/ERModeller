using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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

namespace ERMODELLER
{
    /// <summary>
    /// Interaction logic for TablePageBase.xaml
    /// </summary>
    public abstract partial class TablePageBase : Page
    {
        /// <summary>
        /// Список существующих таблиц
        /// </summary>
        protected List<DbTableModel> existedTables;

        /// <summary>
        /// Список внешних таблиц
        /// </summary>
        protected List<DbTableModel> foreignTables;

        /// <summary>,
        /// список атрибутов новой таблицы,
        /// </summary>
        protected List<DbTableItem> tableItems;

        protected TablePageBase()
        {
            InitializeComponent();
            var values = Enum.GetValues(typeof(SqlDbType));
            foreach (var value in values)
            {
                cbColumnType.Items.Add(value.ToString().ToLower());
            }

            dgMain.CanUserSortColumns = false;
            dgMain.AutoGeneratingColumn += dgPrimaryGrid_AutoGeneratingColumn;
        }

        /// <summary>
        /// Метод отметы взаимодействия со страницей табилцы
        /// </summary>
        public abstract void Cancel();

        /// <summary>
        /// Метод открытия страницы таблицы
        /// </summary>
        /// <param name="foreignTables">Список внешнийх таблиц</param>
        /// <param name="editableTable">Изменяемая таблица</param>
        /// <param name="newTablePos">Позиция новой таблицы</param>
        public abstract void OpenPage(List<DbTableModel> foreignTables, [Optional] DbTable editableTable, [Optional] Point newTablePos);

        /// <summary>
        /// Метод подтверждения изменений
        /// </summary>
        public abstract void Submit();

        /// <summary>
        /// Метод добавления нового атрибута таблицы
        /// </summary>
        protected void AddColumn()
        {
            if (CheckColumnFields())
                return;
            string pk = "";
            string name = tbColumnName.Text;
            string type = cbColumnType.Text;
            string typeSize = tbColumnSize.Text == "" ? "" : $"({tbColumnSize.Text})";
            typeSize = typeSize.Replace(" ", "");
            bool uq = (bool)UqCheckbox.IsChecked;
            bool nn = (bool)NotNullCheckbox.IsChecked;
            DbTableItem data;
            switch ((bool)PkCheckbox.IsChecked)
            {
                case true:
                    pk = "PK";
                    if (CheckColumnUniqueness(name))
                        break;
                    data = new DbTableItem { PK = pk, FK = "", Name = name, Type = type + typeSize, UQ = uq, NN = nn };
                    tableItems.Insert(0, data);
                    break;

                case false:
                    if (CheckColumnUniqueness(name))
                        break;
                    data = new DbTableItem { PK = pk, FK = "", Name = name, Type = type + typeSize, UQ = uq, NN = nn };
                    tableItems.Add(data);
                    break;
            }
            dgMain.Items.Refresh();
            CheckIfEmpty();
        }

        /// <summary>
        /// Метод добавления внешнего ключа
        /// </summary>
        protected void AddForeignKey()
        {
            if (CheckFKFields())
                return;
            string pk = "";
            string fk = "FK";
            string foreignTableText = cbForeignTable.Text;
            string foreignKeyText = cbForeignKey.Text;
            string foreignKeyType = ((DbTableItem)cbForeignKey.SelectedValue).Type;
            DbTableModel foreignTable = (DbTableModel)cbForeignTable.SelectedItem;
            string name = tbFKName.Text;
            bool uq = (bool)UqCheckbox.IsChecked;
            bool nn = (bool)FkNnCheckbox.IsChecked;
            DbTableItem data;
            switch ((bool)FkPkCheckbox.IsChecked)
            {
                case true:
                    pk = "PK";
                    if (CheckColumnUniqueness(name))
                        break;
                    data = new DbTableItem { PK = pk, FK = fk, Name = name, Type = foreignKeyType, UQ = uq, NN = nn, ForeignTable = foreignTableText, ForeignKey = foreignKeyText, ForeignKeyType = foreignKeyType };
                    tableItems.Insert(0, data);
                    foreignTables.Add(foreignTable);
                    break;

                case false:
                    if (CheckColumnUniqueness(name))
                        break;
                    data = new DbTableItem { PK = pk, FK = fk, Name = name, Type = foreignKeyType, UQ = uq, NN = nn, ForeignTable = foreignTableText, ForeignKey = foreignKeyText, ForeignKeyType = foreignKeyType };
                    foreignTables.Add(foreignTable);
                    tableItems.Add(data);
                    break;
            }
            dgMain.Items.Refresh();
            CheckIfEmpty();
        }

        protected void bAddColumn_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckName(tbColumnName))
                return;
            AddColumn();
        }

        protected void bAddFK_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckName(tbFKName))
                return;
            AddForeignKey();
        }

        protected void bCancelTable_Click(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        protected void bColumnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (tableItems.Count == 0)
                return;
            if (dgMain.SelectedItem != null)
            {
                tableItems.RemoveAt(dgMain.SelectedIndex);
                dgMain.Items.Refresh();
                return;
            }
            tableItems.RemoveAt(0);
            dgMain.Items.Refresh();
            CheckIfEmpty();
        }

        protected void bColumnDown_Click(object sender, RoutedEventArgs e)
        {
            ColumnDown();
        }

        protected void bColumnUp_Click(object sender, RoutedEventArgs e)
        {
            ColumnUp();
        }

        protected void BSubmitTable_OnClick(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        protected void cbColumnType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (e.AddedItems[0])
            {
                case "binary":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "char":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "decimal":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "nchar":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "numeric":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "nvarchar":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "time":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "varbinary":
                    tbColumnSize.IsEnabled = true;
                    break;

                case "varchar":
                    tbColumnSize.IsEnabled = true;
                    break;

                default:
                    tbColumnSize.IsEnabled = false;
                    tbColumnSize.Text = "";
                    break;
            }
        }

        protected void cbForeignTable_SelectionChanged(object sender, RoutedEventArgs e)
        {
            SetForeignKeysCombobox();
        }

        /// <summary>
        /// Метод проверки корректности заполнения полей атрибута
        /// </summary>
        /// <returns>True если введено неправильно</returns>
        protected bool CheckColumnFields()
        {
            if (tbColumnName.Text.Length <= 0 || cbColumnType.Text.Length <= 0)
            {
                MessageBox.Show("Не все поля заполнены", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            if (tbColumnSize.IsEnabled == true && !Regex.IsMatch(tbColumnSize.Text, @"(^\s*\d{0,5}\s*$)") && !(cbColumnType.Text == "decimal" && (Regex.IsMatch(tbColumnSize.Text, @"^\s*-?\s*\d{1,2}\s*(,\s*\d{1,3}\s*){0,1}$") || Regex.IsMatch(tbColumnSize.Text, @"(^\s*-?\s*\d{0,2}\s*$)"))))
            {
                MessageBox.Show("Размерность введена неверно", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            tbColumnSize.Text.Replace(" ", "");
            return false;
        }

        /// <summary>
        /// Проверка уникальности имени атрибута
        /// </summary>
        /// <param name="name">Имя атрибута</param>
        /// <returns>True если введено неправильно</returns>
        protected bool CheckColumnUniqueness(string name)
        {
            if (tableItems.Any(item => item.Name == name))
            {
                MessageBox.Show("Такое поле уже существует", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Проверка на корректность заполнения полей внешнего ключа
        /// </summary>
        /// <returns></returns>
        protected bool CheckFKFields()
        {
            if (tbFKName.Text.Length <= 0 || cbForeignKey.Text.Length <= 0 || cbForeignTable.Text.Length <= 0)
            {
                MessageBox.Show("Не все поля заполнены", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Метод активации стрелок смены положения атрибута
        /// Если таблица не пуста
        /// </summary>
        protected void CheckIfEmpty()
        {
            switch (dgMain.Items.IsEmpty)
            {
                case true:
                    bColumnUp.IsEnabled = false;
                    bColumnDown.IsEnabled = false;
                    bColumnDelete.IsEnabled = false;
                    break;

                case false:
                    bColumnUp.IsEnabled = true;
                    bColumnDown.IsEnabled = true;
                    bColumnDelete.IsEnabled = true;
                    break;
            }
        }

        /// <summary>
        /// Проверка имени на соответствие
        /// конвенции
        /// </summary>
        /// <param name="textContainer">Контейнер имени</param>
        /// <returns>True если ввод неверный</returns>
        protected bool CheckName(TextBox textContainer)
        {
            bool isMatch = Regex.IsMatch(textContainer.Text, @"^[_\p{L}][\p{L}\d_#$@]{1,128}$");
            if (isMatch)
                return true;
            MessageBox.Show(textContainer.Text + " не соответствует соглашению о наименованиях", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        /// <summary>
        /// Изменение положения атрибута вниз
        /// </summary>
        protected void ColumnDown()
        {
            if (dgMain.SelectedItem == null || dgMain.SelectedIndex == dgMain.Items.Count - 1)
                return;
            int selectedIndex = dgMain.SelectedIndex;
            var temp = tableItems[selectedIndex];
            tableItems[selectedIndex] = tableItems[selectedIndex + 1];
            tableItems[selectedIndex + 1] = temp;
            dgMain.Items.Refresh();
        }

        /// <summary>
        /// Изменение положения атрибута вверх
        /// </summary>
        protected void ColumnUp()
        {
            if (dgMain.SelectedItem == null || dgMain.SelectedIndex == 0)
                return;
            int selectedIndex = dgMain.SelectedIndex;
            var temp = tableItems[selectedIndex];
            tableItems[selectedIndex] = tableItems[selectedIndex - 1];
            tableItems[selectedIndex - 1] = temp;
            dgMain.Items.Refresh();
        }

        protected void dgMain_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            CheckIfEmpty();
        }

        protected void dgMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged();
        }

        protected void dgPrimaryGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var desc = e.PropertyDescriptor as PropertyDescriptor;
            var att = desc.Attributes[typeof(ColumnAttributeName)] as ColumnAttributeName;
            if (att != null)
            {
                e.Column.Header = att.Name;
            }
        }

        /// <summary>
        /// Метод обработки изменения выбранного атрибута таблицы
        /// </summary>
        protected void SelectionChanged()
        {
            var selectedItem = (DbTableItem)dgMain.SelectedItem;

            if (selectedItem == null)
                return;

            string columnType = selectedItem.Type;
            string columnSize = "";
            if (selectedItem.Type.Contains("("))
            {
                columnType = Regex.Match(selectedItem.Type, @".*?(?=\()").Value;
                columnSize = Regex.Match(selectedItem.Type, @"\(.*\)").Value.Trim('(', ')');
            }

            if (selectedItem.FK != "FK")
            {
                tbColumnName.Text = selectedItem.Name;
                tbColumnSize.Text = columnSize;
                NotNullCheckbox.IsChecked = selectedItem.NN;
                UqCheckbox.IsChecked = selectedItem.UQ;
                PkCheckbox.IsChecked = selectedItem.PK == "PK" ? true : false;
                cbColumnType.SelectedItem = columnType;
            }
            else
            {
                tbFKName.Text = selectedItem.Name;
                FkNnCheckbox.IsChecked = selectedItem.NN;
                FkUqCheckbox.IsChecked = selectedItem.UQ;
                FkPkCheckbox.IsChecked = selectedItem.PK == "PK" ? true : false;
                cbForeignTable.SelectedItem =
                    ((List<DbTableModel>)cbForeignTable.ItemsSource).Find(i => i.TableName == selectedItem.ForeignTable);
                SetForeignKeysCombobox();
                cbForeignKey.SelectedItem = selectedItem;
            }
        }

        /// <summary>
        /// Заполнение селектора внешних таблиц
        /// </summary>
        protected void SetForeignKeysCombobox()
        {
            if (cbForeignTable.SelectedItem == null)
                return;
            cbForeignKey.ItemsSource = ((DbTableModel)cbForeignTable.SelectedItem).GetPrimaryKeys();
            cbForeignKey.DisplayMemberPath = "Name";
            cbForeignKey.Items.Refresh();
            if (cbForeignKey.Items.Count <= 0)
            {
                cbForeignKey.IsEnabled = false;
                return;
            }
            cbForeignKey.IsEnabled = true;
            cbForeignKey.SelectedIndex = 0;
        }
    }
}