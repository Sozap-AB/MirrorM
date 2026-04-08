using System;

namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionConnectionMatch : ExpressionBase
    {
        /*
        
        Plan:

        1) "GetOrSaveEntitiesToStorage" should save connections to connection storage
        1.1) We need to add connection column to SELECT after all other columns and read it in "GetOrSaveEntitiesToStorage"
        2) When filtering by conidtion "BoolExpressionConnectionMatch", we use connection storage

         */

        public string ConnectionTable { get; }
        public string OwnerKey { get; }
        public string ForeignKey { get; }
        public Guid Value { get; }

        internal ExpressionConnectionMatch(string connectionTable, string ownerKey, string foreignKey, Guid value)
        {
            ConnectionTable = connectionTable;
            OwnerKey = ownerKey;
            ForeignKey = foreignKey;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionConnectionMatch ecm)
                return
                    ConnectionTable == ecm.ConnectionTable &&
                    OwnerKey == ecm.OwnerKey &&
                    ForeignKey == ecm.ForeignKey &&
                    Value == ecm.Value;

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ConnectionTable, OwnerKey, ForeignKey, Value);
        }
    }
}
