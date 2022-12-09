using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDMremote.Utilities;

namespace FDMremote.Optimization
{
    internal class OBJMinLength : OBJ
    {
        public double MinLength;

        public OBJMinLength(double minlength, double weight)
        {
            this.OBJID = 1;
            this.MinLength = minlength;
            this.Weight = weight;
        }
    }
}
