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

namespace FDMremote.Analysis
{
    internal class FDMproblem
    {
        public Network FDMnetwork;
        public Matrix<double> XYZf; //fixed node positions, dense
        public Matrix<double> C; //full connectivity matrix
        public Matrix<double> Cn; //connectivity matrix (free), sparse
        public Matrix<double> Cf; //connectivity matrix (fixed), sparse
        public Matrix<double> Q; //force density matrix, sparse diagonal
        public double Tolerance; 
        
        public FDMproblem(Network fdm)
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
            XYZf = Matrix<double>.Build.DenseOfRowArrays(xyzf);
        }

        /// <summary>
        /// Generates the connectivity matrix C, and partitioned columns Cn, Cf
        /// </summary>
        private void GetC()
        {
            // Initialize
            C = Solver.GetC(FDMnetwork);

            //var Ccols = C.ToColumnArrays();

            //initialize rows for N
            //double[][] Ncols = new double[FDMnetwork.N.Count][];
            //double[][] Fcols = new double[FDMnetwork.F.Count][];

            List<Vector<double>> Ncols = new List<Vector<double>>();
            List<Vector<double>> Fcols = new List<Vector<double>>();
            //extract the columns of free nodes
            for (int i = 0; i < FDMnetwork.N.Count; i++)
            {
                int index = FDMnetwork.N[i];
                //Ncols[i] = Ccols[index];
                //Ncols[i] = C.Column(index).ToArray();
                Ncols.Add(C.Column(index));
            }

            //extract the columns of fixed nodes
            for (int i = 0; i < FDMnetwork.F.Count; i++)
            {
                int index = FDMnetwork.F[i];
                //Fcols[i] = Ccols[index];
                //Fcols[i] = C.Column(index).ToArray();
                Fcols.Add(C.Column(index));

            }

            //create sparse matrices
            Cn = Matrix<double>.Build.SparseOfColumnVectors(Ncols);
            Cf = Matrix<double>.Build.SparseOfColumnVectors(Fcols);
        }
        
        /// <summary>
        /// Creates the sparse diagonal force density matrix Q
        /// </summary>
        private void GetQ()
        {
            Q = Matrix<double>.Build.SparseOfDiagonalArray(FDMnetwork.ForceDensities.ToArray());
        }


    }
}
