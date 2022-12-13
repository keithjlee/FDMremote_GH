using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDMremote.Optimization
{
    internal class OBJforcevariation : OBJ
    {
        public OBJforcevariation(double weight)
        {
            this.OBJID = 2;
            this.Weight = weight;
        }
    }
}
