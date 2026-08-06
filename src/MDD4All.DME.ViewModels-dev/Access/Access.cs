namespace MDD4All.ObjectGraph.Access
{
    public abstract class Access
    {
        public Access()
        {
        }

        public virtual bool CanWrite
        {
            get
            {
                return true;
            }
        }
    }
}
