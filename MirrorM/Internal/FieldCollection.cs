using System;
using System.Collections.Generic;
using System.Linq;

namespace MirrorM.Internal
{
    internal class FieldCollection : IFields, IFieldsInternal
    {
        private Dictionary<string, object?> Storage { get; set; }

        public FieldCollection(IReadOnlyDictionary<string, object?> dic)
        {
            Storage = new Dictionary<string, object?>(ConvertDbFields(dic));
        }

        public FieldCollection(FieldCollection fields) : this(fields.Storage)
        {
        }

        public T GetValue<T>(string field)
        {
            //TODO: check required + exisiting type against converters from outer serivce (datetime conversions we can register in library, JsonNode conversion should be added in user app)

            return (T)Storage[field]!;
        }

        public void SetValue<T>(string field, T value)
        {
            Storage[field] = value;
        }

        public bool ContainsField(string field)
        {
            return Storage.ContainsKey(field);
        }

        public object? GetRawValue(string key)
        {
            return Storage[key];
        }

        public void CopyFields(IFieldsInternal fields)
        {
            Storage.Clear();

            foreach (var field in fields.GetEnumerable())
            {
                Storage[field.Key] = field.Value;
            }
        }

        public void CopyFields(IFieldsInternal fields, IEnumerable<string> fieldNames)
        {
            foreach (var name in fieldNames)
            {
                Storage[name] = fields.GetRawValue(name);
            }
        }

        public IEnumerable<KeyValuePair<string, object?>> GetEnumerable()
        {
            return Storage.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value));
        }

        public IReadOnlyDictionary<string, object?> GetStorage()
        {
            return Storage;
        }

        private static IReadOnlyDictionary<string, object?> ConvertDbFields(IReadOnlyDictionary<string, object?> dic)
        {
            return dic.ToDictionary(x => x.Key, x =>
            {
                switch (x.Value)
                {
                    case DBNull _:
                        return null;
                    default:
                        return x.Value;
                }
            });
        }
    }
}
