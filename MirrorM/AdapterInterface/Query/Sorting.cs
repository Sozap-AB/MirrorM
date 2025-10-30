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
    }
}
