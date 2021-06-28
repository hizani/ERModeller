using System;

namespace ERMODELLER
{
    [Serializable]
    public sealed class DbTableItem
    {
        [ColumnAttributeName("PK")]
        public string PK { get; set; }

        [ColumnAttributeName("FK")]
        public string FK { get; set; }

        [ColumnAttributeName("Наименование")]
        public string Name { get; set; }

        [ColumnAttributeName("Тип")]
        public string Type { get; set; }

        [ColumnAttributeName("UQ")]
        public bool UQ { get; set; }

        [ColumnAttributeName("NN")]
        public bool NN { get; set; }

        [ColumnAttributeName("Внешняя таблица")]
        public string ForeignTable { get; set; }

        [ColumnAttributeName("Внешний ключ")]
        public string ForeignKey { get; set; }

        [ColumnAttributeName("Тип внешнего ключа")]
        public string ForeignKeyType { get; set; }
    }
}