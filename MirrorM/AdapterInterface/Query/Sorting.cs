using System;

namespace MirrorM.AdapterInterface.Query
{
    public class Sorting
    {
        public string PropertyName { get; }
        public bool Ascending { get; }

        public Sorting(string propertyName, bool ascending)
        {
            PropertyName = propertyName;
            Ascending = ascending;
        }

        public override bool Equals(object obj)
        {
            if (obj is Sorting s)
                return PropertyName == s.PropertyName && Ascending == s.Ascending;

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PropertyName, Ascending);
        }
    }
}