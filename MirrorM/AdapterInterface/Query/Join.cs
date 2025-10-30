using System;

namespace MirrorM.AdapterInterface.Query
{
    public class Join
    {
        public Type ForeignType { get; }
        public string OwnField { get; }
        public string ForeignField { get; }

        public Join(Type foreignType, string ownField, string foreignField)
        {
            ForeignType = foreignType;
            OwnField = ownField;
            ForeignField = foreignField;
        }
    }
}
