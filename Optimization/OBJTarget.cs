using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using FDMremote.Utilities;

namespace FDMremote.Optimization
{
    /// <summary>
    /// Objective to match a target network
    /// </summary>
    internal class OBJTarget : OBJ
    {
        public OBJTarget(double weight)
        {
            this.OBJID = 0;
            this.Weight = weight;
        }

    }
}
