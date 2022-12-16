using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDMremote.Utilities;
using Newtonsoft.Json;
using FDMremote.Analysis;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace FDMremote.Utilities
{
    internal class InformationObject
    {
        public List<double> Q; //vector of force densities
        public List<double> X;
        public List<double> Y;
        public List<double> Z;
        public double[] Px;
        public double[] Py;
        public double[] Pz;
        public List<int> Ijulia;
        public List<int> Jjulia;
        public List<int> V;
        public int Ne;
        public int Nn;
        public List<int> Njulia;
        public List<int> Fjulia;

        public InformationObject(Network network, FDMproblem problem, Matrix<double> P)
        {
            Q = network.ForceDensities; // force density vector
            ExtractXYZ(network); // node spatial positions
            ExtractPxyz(P); //force vectors for each axis
            IJVjulia(network); // CSC format of connectivity matrix
            Ne = network.Ne; // number of elements
            Nn = network.Nn; // number of nodes
            NFjulia(network); // indices of free/fixed nodes

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

        private void ExtractPxyz(Matrix<double> PXYZ)
        {
            Px = PXYZ.Column(0).ToArray();
            Py = PXYZ.Column(1).ToArray();
            Pz = PXYZ.Column(2).ToArray();
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
