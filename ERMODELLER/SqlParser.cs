using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Windows;

namespace ERMODELLER
{
    internal static class SqlParser
    {
        /// <summary>
        /// Строка подключения к промежуточной базе данных
        /// </summary>
        private static string connectionString = ConfigurationManager.ConnectionStrings["BufferDbConnectionString"].ConnectionString;

        private static SqlConnection conn;

        /// <summary>
        /// Метод преобразования T-SQL скрипта в модель
        /// </summary>
        /// <param name="queries">список запросов скрипта</param>
        /// <returns></returns>
        public static List<DbTableModel> ScriptToModel(List<string> queries)
        {
            conn = new SqlConnection(connectionString);
            var tableModels = new List<DbTableModel>();
            try
            {
                conn.Open();
                //Создаем базу
                foreach (var query in queries)
                {
                    new SqlCommand(query, conn).ExecuteNonQuery();
                }
                //Получаем имена таблиц
                SqlDataReader dr = GetTableNames();
                int multiplier = 0;
                while (dr.Read())
                {
                    multiplier += 4;
                    var tableModel = new DbTableModel();
                    tableModel.X = new Random().Next(0, 200 * multiplier);
                    tableModel.Y = new Random().Next(0, 100 * multiplier);
                    tableModel.TableName = dr[0].ToString();
                    tableModels.Add(tableModel);
                }
                dr.Close();
                //Создаем временную таблицу
                CreateTempTables();
                FillTempTables(tableModels);
                //Задаем атрибуты
                AddAttributes(tableModels);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //Очищение базы
            CleanDB();
            conn.Close();
            return tableModels;
        }

        /// <summary>
        /// Метод добавления атрибутов в таблицу
        /// </summary>
        /// <param name="tableModels">Список моделей создаваемых таблиц</param>
        private static void AddAttributes(List<DbTableModel> tableModels)
        {
            foreach (var model in tableModels)
            {
                var query = "SELECT COLUMNS.TABLE_NAME, COLUMNS.COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE, #PKTemp.PK_NAME, #UQTemp.CONSTRAINT_NAME 'UQ_NAME', #FKTemp.PKTABLE_NAME 'FK_TABLE', #FKTemp.PKCOLUMN_NAME 'FK_COLUMN' from INFORMATION_SCHEMA.COLUMNS" +
                    " LEFT JOIN #FKTemp on COLUMNS.COLUMN_NAME = #FKTemp.FKCOLUMN_NAME and COLUMNS.COLUMN_NAME <> #FKTemp.PKCOLUMN_NAME" +
                    " LEFT JOIN #PKTemp on COLUMNS.COLUMN_NAME = #PKTemp.COLUMN_NAME" +
                    " LEFT JOIN #UQTemp on COLUMNS.COLUMN_NAME = #UQTemp.COLUMN_NAME" +
                    " WHERE COLUMNS.TABLE_NAME = " + "'" + model.TableName + "'";
                SqlDataReader dr = new SqlCommand(query, conn).ExecuteReader();
                model.Attributes = new List<DbTableItem>();
                model.Connections = new List<DbTableModel>();
                while (dr.Read())
                {
                    DbTableItem attribute = new DbTableItem();
                    for (int i = 1; i < dr.FieldCount; i++)
                    {
                        ColumnName column = (ColumnName)i;
                        switch (column)
                        {
                            case ColumnName.COLUMN_NAME:
                                attribute.Name = dr[i].ToString();
                                break;

                            case ColumnName.DATA_TYPE:
                                //Первая буква заглавная
                                attribute.Type = dr[i].ToString().ToLower();
                                break;

                            case ColumnName.CHARACTER_MAXIMUM_LENGTH:
                                if (dr[i].ToString() == "")
                                    continue;
                                attribute.Type += "(" + dr[i].ToString() + ")";
                                break;

                            case ColumnName.NUMERIC_PRECISION:
                                if (dr[i].ToString() == "" || attribute.Type == "int")
                                    continue;
                                attribute.Type += "(" + dr[i].ToString() + ")";
                                break;

                            case ColumnName.NUMERIC_SCALE:
                                if (!attribute.Type.Contains("decimal"))
                                    continue;
                                if (dr[i].ToString() == "")
                                    continue;
                                attribute.Type = attribute.Type.Replace(")", "," + dr[i].ToString() + ")");
                                break;

                            case ColumnName.IS_NULLABLE:
                                if (dr[i].ToString().Contains("NO"))
                                {
                                    attribute.NN = true;
                                    continue;
                                }
                                if (dr[i].ToString().Contains("YES"))
                                {
                                    attribute.NN = false;
                                    continue;
                                }
                                break;

                            case ColumnName.PK_NAME:
                                if (dr[i].ToString() == "")
                                    continue;
                                attribute.PK = "PK";
                                break;

                            case ColumnName.UQ_NAME:
                                if (dr[i].ToString() == "")
                                    continue;
                                attribute.UQ = true;
                                break;

                            case ColumnName.FK_TABLE:
                                if (dr[i].ToString() == "")
                                    continue;
                                attribute.ForeignTable = dr[i].ToString();
                                attribute.FK = "FK";
                                model.Connections.Add(tableModels.Find(x => x.TableName.Contains(attribute.ForeignTable)));
                                break;

                            case ColumnName.FK_COLUMN:
                                if (dr[i].ToString() == "")
                                    continue;
                                attribute.ForeignKey = dr[i].ToString();
                                attribute.ForeignKeyType = attribute.Type;
                                break;
                        }
                    }
                    var item = model.Attributes.Find(x => x.Name.Contains(attribute.Name));
                    if (item != null && item.Name == attribute.Name)
                        continue;
                    model.Attributes.Add(attribute);
                }
                dr.Close();
            }
        }

        /// <summary>
        /// Метод создания временных таблиц для извлечения из них
        /// информации с помощью системных запросов
        /// </summary>
        private static void CreateTempTables()
        {
            var query = "CREATE TABLE #FKTemp( " +
                "PKTABLE_QUALIFIER VARCHAR(100), " +
                "PKTABLE_OWNER VARCHAR(100), " +
                "PKTABLE_NAME VARCHAR(100), " +
                "PKCOLUMN_NAME VARCHAR(100), " +
                "FKTABLE_QUALIFIER VARCHAR(100), " +
                "FKTABLE_OWNER VARCHAR(100), " +
                "FKTABLE_NAME VARCHAR(100), " +
                "FKCOLUMN_NAME VARCHAR(100), " +
                "KEY_SEQ INT, " +
                "UPDATE_RULE INT, " +
                "DELETE_RULE INT, " +
                "FK_NAME VARCHAR(100), " +
                "PK_NAME VARCHAR(100), " +
                "DEFERRABILITY INT, " +
                 ") ";
            new SqlCommand(query, conn).ExecuteNonQuery();

            query = "CREATE TABLE #PKTemp( " +
                "TABLE_QUALIFIER VARCHAR(100), " +
                "TABLE_OWNER VARCHAR(100), " +
                "TABLE_NAME VARCHAR(100), " +
                "COLUMN_NAME VARCHAR(100), " +
                "KEY_SEQ INT, " +
                "PK_NAME VARCHAR(100) " +
                ") ";
            new SqlCommand(query, conn).ExecuteNonQuery();

            //Процедура получения unique constraint
            query = "CREATE PROCEDURE get_unique" +
                " @table_name varchar(100)" +
                " AS" +
                " BEGIN" +
                " select TC.TABLE_NAME, TC.CONSTRAINT_NAME, CC.COLUMN_NAME from information_schema.table_constraints TC" +
                " left join information_schema.constraint_column_usage CC on TC.Constraint_Name = CC.Constraint_Name" +
                " where TC.constraint_type = 'Unique' and TC.TABLE_NAME = @table_name" +
                " END;";
            new SqlCommand(query, conn).ExecuteNonQuery();

            query = "CREATE TABLE #UQTemp(" +
                " TABLE_NAME VARCHAR(100)," +
                " CONSTRAINT_NAME VARCHAR(100)," +
                " COLUMN_NAME VARCHAR(100)" +
                " )";
            new SqlCommand(query, conn).ExecuteNonQuery();
        }

        /// <summary>
        /// Метод заполнения временных таблиц
        /// данными о создаваемых таблицах
        /// </summary>
        /// <param name="tableModels">Список моделей создаваемых таблиц</param>
        private static void FillTempTables(List<DbTableModel> tableModels)
        {
            foreach (var model in tableModels)
            {
                var query = "INSERT INTO #FKTemp EXEC sys.sp_fkeys [" + model.TableName + "]";
                new SqlCommand(query, conn).ExecuteNonQuery();
                query = "INSERT INTO #PKTemp EXEC sys.sp_pkeys [" + model.TableName + "]";
                new SqlCommand(query, conn).ExecuteNonQuery();
                query = "INSERT INTO #UQTemp EXEC get_unique [" + model.TableName + "]";
                new SqlCommand(query, conn).ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Метод удаления временных таблиц
        /// </summary>
        private static void RemoveTempTables()
        {
            var query = "DROP TABLE #FKTemp";
            new SqlCommand(query, conn).ExecuteNonQuery();
            query = "DROP TABLE #PKTemp";
            new SqlCommand(query, conn).ExecuteNonQuery();
        }

        /// <summary>
        /// Получение навзаний таблиц через запросы к системным таблицам
        /// </summary>
        /// <returns>Запрос на получение имени таблицы</returns>
        private static SqlDataReader GetTableNames()
        {
            string query = "SELECT [name], [create_date]" +
                "FROM sys.Tables where name <> 'sysdiagrams' ORDER BY[create_date]";
            return new SqlCommand(query, conn).ExecuteReader();
        }

        /// <summary>
        /// Очищение временной базы данных
        /// </summary>
        private static void CleanDB()
        {
            RemoveTempTables();
            string query = "DECLARE @Sql NVARCHAR(500) DECLARE @Cursor CURSOR" +
                           " SET @Cursor = CURSOR FAST_FORWARD FOR" +
                           " SELECT DISTINCT sql = 'ALTER TABLE [' + tc2.TABLE_SCHEMA + '].[' + tc2.TABLE_NAME + '] DROP [' + rc1.CONSTRAINT_NAME + '];'" +
                           " FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc1" +
                           " LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc2 ON tc2.CONSTRAINT_NAME = rc1.CONSTRAINT_NAME" +
                           " OPEN @Cursor FETCH NEXT FROM @Cursor INTO @Sql" +
                           " WHILE(@@FETCH_STATUS = 0)" +
                           " BEGIN" +
                           " Exec sp_executesql @Sql" +
                           " FETCH NEXT FROM @Cursor INTO @Sql" +
                           " END" +
                           " CLOSE @Cursor DEALLOCATE @Cursor" +
                           " EXEC sp_MSforeachtable 'DROP TABLE ?'";

            new SqlCommand("drop proc get_unique", conn).ExecuteNonQuery();
            new SqlCommand(query, conn).ExecuteNonQuery();
        }

        /// <summary>
        /// Перечисление с кодами для каждого столбца системной таблицы
        /// </summary>
        private enum ColumnName
        {
            TABLE_NAME = 0,
            COLUMN_NAME = 1,
            DATA_TYPE = 2,
            CHARACTER_MAXIMUM_LENGTH = 3,
            NUMERIC_PRECISION = 4,
            NUMERIC_SCALE = 5,
            IS_NULLABLE = 6,
            PK_NAME = 7,
            UQ_NAME = 8,
            FK_TABLE = 9,
            FK_COLUMN = 10
        }
    }
}