using System;
using System.Collections.Generic;

namespace ERMODELLER
{
    [Serializable]
    public class DbTableModel
    {
        /// <summary>
        /// Список связей таблиц
        /// </summary>
        public List<DbTableModel> Connections { get; set; }

        /// <summary>
        /// Атрибуты таблицы
        /// </summary>
        public List<DbTableItem> Attributes { get; set; }

        public string TableName { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        /// <summary>
        /// Метод получения первичных ключей
        /// </summary>
        /// <returns>Список первичных ключей</returns>
        public List<DbTableItem> GetPrimaryKeys()
        {
            List<DbTableItem> pks = new List<DbTableItem>();
            foreach (var attr in Attributes)
            {
                if (attr.PK == "PK")
                    pks.Add(attr);
            }
            return pks;
        }

        /// <summary>
        /// Метод генерации скрипта создания таблицы
        /// </summary>
        /// <returns>Текст скрипта</returns>
        public string GenerateCreateTableScript()
        {
            string createScript = $"CREATE TABLE [{TableName}] (";
            string constraintsPK = $"\nCONSTRAINT PK_{TableName} PRIMARY KEY (";
            string constraintsUQ = $"\nCONSTRAINT UQ_{TableName} UNIQUE (";

            bool hasPK = false;
            bool hasUnique = false;
            foreach (var attr in Attributes)
            {
                string constraintsFK = "";

                if (attr.PK == "PK")
                {
                    constraintsPK += $"[{attr.Name}],";
                    attr.NN = true;
                    hasPK = true;
                }
                if (attr.UQ == true)
                {
                    constraintsUQ += $"[{attr.Name}],";
                    hasUnique = true;
                }
                if (attr.FK == "FK")
                {
                    constraintsFK += $"\nCONSTRAINT FK_{TableName}_{attr.ForeignTable} FOREIGN KEY ([{attr.Name}]) REFERENCES [{attr.ForeignTable}]([{attr.ForeignKey}]),";
                }

                createScript += $"\n[{attr.Name}] {attr.Type} {NotNullString(attr.NN)},";
                createScript += constraintsFK;
            }

            if (constraintsPK.LastIndexOf(',') >= 0)
            {
                constraintsPK = constraintsPK.Remove(constraintsPK.LastIndexOf(','));
                constraintsPK += "),";
            }

            constraintsUQ = hasUnique ? constraintsUQ.Remove(constraintsUQ.LastIndexOf(',')) + ")," : "";

            if (!hasPK)
                constraintsPK = "";

            createScript += $"{constraintsPK}{constraintsUQ}";
            createScript += "\n)\nGO";
            return createScript;
        }

        /// <summary>
        /// Метод трансформации булевого значения NN в текст
        /// </summary>
        /// <param name="NN">NOT NULL</param>
        /// <returns>Текст свойства</returns>
        public string NotNullString(bool NN)
        {
            switch (NN)
            {
                case true:
                    return "NOT NULL";

                case false:
                    return "NULL";
            }

            return "";
        }
    }
}