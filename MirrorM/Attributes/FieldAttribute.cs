using System;

namespace MirrorM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        public string FieldName { get; }

        public FieldAttribute(string value)
        {
            FieldName = value;
        }
    }
}
