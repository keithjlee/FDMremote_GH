using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using FDMremote.Utilities;

namespace FDMremote.Analysis
{
    internal class FDMproblem
    {
        public Network FDMnetwork;
        public Matrix<double> XYZf; //fixed node positions, dense
        public SparseMatrix C; //full connectivity matrix
        public SparseMatrix Cn; //connectivity matrix (free), sparse
        public SparseMatrix Cf; //connectivity matrix (fixed), sparse
        public SparseMatrix Q; //force density matrix, sparse diagonal
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


            List<Vector<double>> Ncols = new List<Vector<double>>();
            List<Vector<double>> Fcols = new List<Vector<double>>();

            //extract the columns of free nodes
            for (int i = 0; i < FDMnetwork.N.Count; i++)
            {
                int index = FDMnetwork.N[i];
                Ncols.Add(C.Column(index));
            }

            //extract the columns of fixed nodes
            for (int i = 0; i < FDMnetwork.F.Count; i++)
            {
                int index = FDMnetwork.F[i];
                Fcols.Add(C.Column(index));

            }

            //create sparse matrices
            Cn = (SparseMatrix)Matrix<double>.Build.SparseOfColumnVectors(Ncols);
            Cf = (SparseMatrix)Matrix<double>.Build.SparseOfColumnVectors(Fcols);
        }
        
        /// <summary>
        /// Creates the sparse diagonal force density matrix Q
        /// </summary>
        private void GetQ()
        {
            Q = new SparseMatrix(FDMnetwork.Ne, FDMnetwork.Ne);

            for (int i = 0; i < FDMnetwork.Ne; i++)
            {
                Q[i, i] = FDMnetwork.ForceDensities[i];
            }
        }


    }
}
