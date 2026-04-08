using System;

namespace MirrorM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        public string FieldName { get; }
        public bool Indexed { get; }
        public string? TypeOverride { get; }
        public bool Queriable { get; }

        public FieldAttribute(string value, bool indexed = false, string? typeOverride = null, bool queriable = true)
        {
            FieldName = value;
            Indexed = indexed;
            TypeOverride = typeOverride;
            Queriable = queriable;
        }
    }
}
