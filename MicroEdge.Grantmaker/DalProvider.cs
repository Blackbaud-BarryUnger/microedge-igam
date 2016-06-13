using System;

namespace MicroEdge.Grantmaker
{
    public class DalProvider : Igam.Dal.DalProvider
    {
        public override T GetDalObject<T>()
        {
            Type type = typeof(T);
            //Handle grantmaker-specific dal types here

            //Allow the base dal assembly try
            return base.GetDalObject<T>();
        }
    }
}