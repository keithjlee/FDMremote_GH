using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using FDMremote.Utilities;
using EigenCore;
using EigenCore.Core.Dense;
using EigenCore.Core.Sparse;

namespace FDMremote.Analysis
{
    internal class FDMproblem2
    {
        public Network FDMnetwork;
        public MatrixXD XYZf; //fixed node positions, dense
        public SparseMatrixD C; //full connectivity matrix
        public SparseMatrixD Cn; //connectivity matrix (free), sparse
        public SparseMatrixD Cf; //connectivity matrix (fixed), sparse
        public SparseMatrixD Q; //force density matrix, sparse diagonal
        public double Tolerance; 
        
        public FDMproblem2(Network fdm)
        {
            //copy input network
            FDMnetwork = new Network(fdm);

            if (!FDMnetwork.Valid) throw new Exception("FDM network is invalid");

            Tolerance = fdm.Tolerance;

            //Build XYZf matrix
            GetXYZf();

            //build C
            GetC();

            //build Q
            GetQ();

        }

        /// <summary>
        /// Extracts the free node indices
        /// </summary>
        private void GetXYZf()
        {
            //extract all the XYZ arrays for free nodes
            List<double[]> xyzf = new List<double[]>();
            for (int i = 0; i < FDMnetwork.F.Count; i++)
            {
                int index = FDMnetwork.F[i];
                xyzf.Add(FDMnetwork.XYZ[index]);
            }


            //create XYZf matrix
            XYZf = new MatrixXD(xyzf.ToArray());
        }

        /// <summary>
        /// Generates the connectivity matrix C, and partitioned columns Cn, Cf
        /// </summary>
        private void GetC()
        {
            // Initialize
            var connectivity = new List<(int, int, double)>();
            var connectivity_free = new List<(int, int, double)>();
            var connectivity_fixed = new List<(int, int, double)>();


            for (int i = 0; i < FDMnetwork.Ne; i++)
            {

                var index = FDMnetwork.Indices[i];

                //main connectivity
                int i1 = index[0];
                int i2 = index[1];

                connectivity.Add((i, i1, -1));
                connectivity.Add((i, i2, 1));

                if (FDMnetwork.N.Contains(i1)){
                    connectivity_free.Add((i, i1, -1));
                }

                if (FDMnetwork.N.Contains(i2))
                {
                    connectivity_free.Add((i, i2, 1));
                }

                if (FDMnetwork.F.Contains(i1))
                {
                    connectivity_fixed.Add((i, i1, -1));
                }

                if (FDMnetwork.F.Contains(i2))
                {
                    connectivity_fixed.Add((i, i2, 1));
                }



            }

            C = new SparseMatrixD(connectivity, FDMnetwork.Ne, FDMnetwork.Nn);
            Cn = new SparseMatrixD(connectivity_free, FDMnetwork.Ne, FDMnetwork.N.Count());
            Cf = new SparseMatrixD(connectivity_fixed, FDMnetwork.Ne, FDMnetwork.F.Count());
        }

        /// <summary>
        /// Creates the sparse diagonal force density matrix Q
        /// </summary>
        private void GetQ()
        {
            Q = SparseMatrixD.Diag(FDMnetwork.ForceDensities.ToArray());
        }


    }
}
