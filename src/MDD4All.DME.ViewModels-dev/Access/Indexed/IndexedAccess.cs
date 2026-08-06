namespace MDD4All.ObjectGraph.Access
{
    public class IndexedAccess : Access
    {
        public IndexedAccess(int index) 
        { 
            this.Index = index;
        }

        public int Index { get; set; }
    }
}
