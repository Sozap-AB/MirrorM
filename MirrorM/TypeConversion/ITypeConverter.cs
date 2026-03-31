using MirrorM.Common;
using System.Data;

namespace MirrorM.TypeConversion
{
    public interface ITypeConverter
    {
        bool TryConvertOnRead(IDataReader reader, int index, out object? result);
        bool TryConvertToWrite(object obj, out SqlParameterValue result);
    }
}
