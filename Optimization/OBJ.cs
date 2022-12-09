using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDMremote.Optimization
{
    /// <summary>
    /// Abstract class for objective functions
    /// Must include ID field and Weight field at a minimum
    /// </summary>
    internal abstract class OBJ
    {
        public int OBJID;
        public double Weight;
    }
}
