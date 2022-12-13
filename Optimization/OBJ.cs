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

    internal class OBJTarget : OBJ
    {
        public OBJTarget(double weight)
        {
            this.OBJID = 0;
            this.Weight = weight;
        }

    }

    internal class OBJlengthvariation : OBJ
    {
        public OBJlengthvariation(double weight)
        {
            this.OBJID = 1;
            this.Weight = weight;
        }
    }

    internal class OBJforcevariation : OBJ
    {
        public OBJforcevariation(double weight)
        {
            this.OBJID = 2;
            this.Weight = weight;
        }

    }

    internal class OBJPerformance : OBJ
    {
        public OBJPerformance(double weight)
        {
            this.OBJID = 3;
            this.Weight = weight;
        }
    }

    internal class OBJMinlength : OBJ
    {
        public double Value;

        public OBJMinlength(double value, double weight)
        {
            this.OBJID = 4;
            Value = value;
            this.Weight = weight;
        }
    }

    internal class OBJMaxlength : OBJ
    {
        public double Value;

        public OBJMaxlength(double value, double weight)
        {
            this.OBJID = 5;
            Value = value;
            this.Weight = weight;
        }
    }
}
