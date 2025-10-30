using System.Collections.Generic;

namespace MirrorM.Internal
{
    internal interface IFieldsInternal
    {
        T GetValue<T>(string field);
        void SetValue<T>(string field, T value);

        object? GetRawValue(string key);
        IEnumerable<KeyValuePair<string, object?>> GetEnumerable();
        IReadOnlyDictionary<string, object?> GetStorage();
        void CopyFields(IFieldsInternal fields);
        void CopyFields(IFieldsInternal fields, IEnumerable<string> fieldNames);
    }
}
