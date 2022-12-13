using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FDMremote.Optimization
{
    /// <summary>
    /// Class to directly parse output JSON messages from server
    /// </summary>
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
    }
}
