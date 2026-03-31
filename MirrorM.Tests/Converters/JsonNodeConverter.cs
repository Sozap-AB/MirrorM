using MirrorM.Common;
using MirrorM.TypeConversion;
using System.Data;
using System.Text.Json.Nodes;

namespace MirrorM.Tests.Converters
{
    internal class JsonNodeConverter : ITypeConverter
    {
        private const string JSONB_PG_DATA_TYPE_NAME = "jsonb";

        public bool TryConvertOnRead(IDataReader reader, int index, out object? result)
        {
            var pgTypeName = reader.GetDataTypeName(index);

            result = null;

            if (pgTypeName != JSONB_PG_DATA_TYPE_NAME)
                return false;

            result = JsonNode.Parse(reader.GetString(index))!;

            return true;
        }

        public bool TryConvertToWrite(object obj, out SqlParameterValue result)
        {
            result = SqlParameterValue.Empty;

            switch (obj)
            {
                case JsonNode node:
                    result = new SqlParameterValue(node.ToJsonString(), SqlFieldType.Jsonb);

                    return true;
                default:
                    return false;
            }
        }
    }
}
