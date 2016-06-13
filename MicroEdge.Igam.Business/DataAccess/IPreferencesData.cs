using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroEdge.Igam.Business.DataAccess
{
    public interface IPreferencesData
    {
        string CFSRoot { get; set; }
        ReturnStatus Read();
    }
}
