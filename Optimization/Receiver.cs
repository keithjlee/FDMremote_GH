using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FDMremote.Optimization
{
    internal class Receiver
    {
        public bool Finished;
        public int Iter;
        public double Loss;
        public List<double> Q;
        public List<double> X;
        public List<double> Y;
        public List<double> Z;
        public List<double> Losstrace;
        //public double[] Q;
        //public double[] X;
        //public double[] Y;
        //public double[] Z;
        //public double[] Losstrace;
    }
}
