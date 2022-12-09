using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDMremote.Utilities;
using Rhino.Geometry;

namespace FDMremote.Optimization
{
    internal class OptimizationProblem
    {
        //Objective functions
        public List<int> OBJids;
        public List<double> OBJweights;
        public double MinLength;

        //optimization parameters
        public double LowerBound;
        public double UpperBound;
        public double AbsTolerance;
        public double RelTolerance;
        public bool ShowIterations;
        public int UpdateFrequency;
        public int MaxIterations;

        //initial values
        public List<double> Q;

        //node positions (also the target)
        public List<double> X;
        public List<double> Y;
        public List<double> Z;

        //general information
        public int Ne;
        public int Nn;

        //load components
        public double[] Px;
        public double[] Py;
        public double[] Pz;

        //topology
        public List<int> Ijulia;
        public List<int> Jjulia;
        public List<int> V;

        //free/fixed
        public List<int> Njulia;
        public List<int> Fjulia;

        public OptimizationProblem(Network network, OBJParameters objparams, List<Vector3d> P)
        {
            Q = network.ForceDensities;
            ExtractXYZ(network);
            IJVjulia(network);
            Ne = network.Ne;
            Nn = network.Nn;
            NFjulia(network);
            ExtractPXYZ(P);
            ExtractOBJs(objparams);
        }

        private void ExtractXYZ(Network network)
        {
            List<double[]> xyz = network.XYZ;

            X = new List<double>();
            Y = new List<double>();
            Z = new List<double>();

            for (int i = 0; i < xyz.Count; i++)
            {
                double[] point = xyz[i];
                X.Add(point[0]);
                Y.Add(point[1]);
                Z.Add(point[2]);
            }
        }

        private void ExtractOBJs(OBJParameters objparams)
        {
            //copying values
            LowerBound = objparams.LowerBound;
            UpperBound = objparams.UpperBound;
            AbsTolerance = objparams.AbsTolerance;
            RelTolerance = objparams.RelTolerance;
            ShowIterations = objparams.ShowIterations;
            UpdateFrequency = objparams.UpdateFrequency;
            MaxIterations = objparams.MaxIterations;

            //objectives and weights
            OBJids = new List<int>();
            OBJweights = new List<double>();

            for (int i = 0; i < objparams.Objectives.Count; i++)
            {
                var obj = objparams.Objectives[i];
                OBJids.Add(obj.OBJID);
                OBJweights.Add(obj.Weight);

                if (obj.OBJID == 1)
                {
                    var obj0 = (OBJMinLength)obj;
                    MinLength = obj0.MinLength;
                }
            }
        }

        private void ExtractPXYZ(List<Vector3d> Loads)
        {
            Px = new double[Njulia.Count];
            Py = new double[Njulia.Count];
            Pz = new double[Njulia.Count];

            if (Loads.Count == 1)
            {
                Vector3d load = Loads[0];
                for (int i = 0; i < Njulia.Count; i++)
                {
                    Px[i] = load.X;
                    Py[i] = load.Y;
                    Pz[i] = load.Z;
                }
            }
            //else if (Loads.Count == Njulia.Count)
            else
            {
                for (int i = 0; i < Njulia.Count; i++)
                {
                    Px[i] = Loads[i].X;
                    Py[i] = Loads[i].Y;
                    Pz[i] = Loads[i].Z;
                }
            }
        }

        private void IJVjulia(Network network)
        {
            Ijulia = new List<int>();
            Jjulia = new List<int>();
            V = new List<int>();

            for (int i = 0; i < network.Ne; i++)
            {
                var index = network.Indices[i];
                Ijulia.Add(i + 1);
                Jjulia.Add(index[0] + 1);
                V.Add(-1);

                Ijulia.Add(i + 1);
                Jjulia.Add(index[1] + 1);
                V.Add(1);
            }
        }

        private void NFjulia(Network network)
        {
            Njulia = new List<int>();
            Fjulia = new List<int>();

            for (int i = 0; i < network.N.Count; i++)
            {
                Njulia.Add(network.N[i] + 1);
            }

            for (int i = 0; i < network.F.Count; i++)
            {
                Fjulia.Add(network.F[i] + 1);
            }
        }

    }


}
