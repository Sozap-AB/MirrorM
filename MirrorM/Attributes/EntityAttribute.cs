using System;

namespace MirrorM.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        public string TableName { get; }
        public Type[]? SubTypes { get; }

        public EntityAttribute(string tableName, Type[]? subTypes = null)
        {
            TableName = tableName;
            SubTypes = subTypes;
        }
    }
}
