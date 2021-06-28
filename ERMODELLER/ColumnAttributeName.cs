using System;

namespace ERMODELLER
{
    [Serializable]
    public sealed class ColumnAttributeName : System.Attribute
    {
        public ColumnAttributeName(string Name)
        {
            this.Name = Name;
        }

        public string Name { get; set; }
    }
}