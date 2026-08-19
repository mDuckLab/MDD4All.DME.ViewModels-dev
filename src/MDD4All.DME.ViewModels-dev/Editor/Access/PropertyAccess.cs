using System.Reflection;

namespace MDD4All.ObjectGraph.Access
{
    public class PropertyAccess : Access
    {
        public PropertyAccess(PropertyInfo propertyInfo)
        {
            this.PropertyInfo = propertyInfo;
        }
        public PropertyInfo PropertyInfo { get; private set; }

        public override bool CanWrite
        {
            get
            {
                return PropertyInfo.CanWrite;
            }
        }
    }
}
