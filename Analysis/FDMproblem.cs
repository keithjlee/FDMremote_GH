using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using FDMremote.Utilities;

namespace FDMremote.Analysis
{
    internal class FDMproblem
    {
        public Network FDMnetwork;
        public Matrix<double> XYZf; //fixed node positions, dense
        public Matrix<double> Pn; //load matrix, dense
        public Matrix<double> C; //full connectivity matrix
        public Matrix<double> Cn; //connectivity matrix (free), sparse
        public Matrix<double> Cf; //connectivity matrix (fixed), sparse
        public Matrix<double> Q; //force density matrix, sparse diagonal
        public double Tolerance; 
        
        public FDMproblem(Network fdm, Vector3d p, double tol)
        {
            //copy input network
            FDMnetwork = new Network(fdm);

            if (!FDMnetwork.Valid) throw new Exception("FDM network is invalid");

            Tolerance = tol;
            // problem geometry
            //int nFree = fdm.N.Count;
            //int nFixed = fdm.F.Count;
            //int nNodes = fdm.Nn;
            //int nElements = fdm.Ne;

            //Build XYZf matrix
            GetXYZf();

            //build Pn matrix
            GetPn(p);

            //build C
            GetC();

            //build Q
            GetQ();

        }

        public FDMproblem(Network fdm, List<Vector3d> p, double tol)
        {
            //copy input network
            FDMnetwork = new Network(fdm);

            if (!FDMnetwork.Valid) throw new Exception("FDM network is invalid");

            //Intersection tolerance
            Tolerance = tol;

            //Build XYZf matrix
            GetXYZf();

            //build Pn matrix
            GetPn(p);

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
        /// Generates the Nx3 load matrix
        /// </summary>
        /// <param name="p"></param>
        private void GetPn(Vector3d p)
        {
            double[] pArray = new double[] {p.X, p.Y, p.Z}; //convert Vector3d into array of doubles

            //repeat load vector for free nodes
            List<double[]> pn = new List<double[]>();
            for (int i = 0; i < FDMnetwork.N.Count; i++)
            {
                pn.Add(pArray);
            }

            //create Pn matrix
            Pn = Matrix<double>.Build.DenseOfRowArrays(pn);
        }

        private void GetPn(List<Vector3d> p)
        {
            if (p.Count != FDMnetwork.N.Count && p.Count != 1) throw new ArgumentException("Length of force vectors must be 1 or match length of free nodes.");

            if (p.Count == 1) //use the single vector method
            {
                GetPn(p[0]); 
            }
            else //assign individual loads
            {
                //repeat load vector for free nodes
                List<double[]> pn = new List<double[]>();
                for (int i = 0; i < FDMnetwork.N.Count; i++)
                {
                    Vector3d currentP = p[i];
                    double[] pArray = new double[] { currentP.X, currentP.Y, currentP.Z }; //convert Vector3d into array of doubles
                    pn.Add(pArray);
                }

                //create Pn matrix
                Pn = Matrix<double>.Build.DenseOfRowArrays(pn);
            }
            
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
            double[][] Ncols = new double[FDMnetwork.N.Count][];
            double[][] Fcols = new double[FDMnetwork.F.Count][];

            //extract the columns of free nodes
            for (int i = 0; i < FDMnetwork.N.Count; i++)
            {
                int index = FDMnetwork.N[i];
                //Ncols[i] = Ccols[index];
                Ncols[i] = C.Column(index).ToArray();
            }

            //extract the columns of fixed nodes
            for (int i = 0; i < FDMnetwork.F.Count; i++)
            {
                int index = FDMnetwork.F[i];
                //Fcols[i] = Ccols[index];
                Fcols[i] = C.Column(index).ToArray();
            }

            //create sparse matrices
            Cn = Matrix<double>.Build.SparseOfColumnArrays(Ncols);
            Cf = Matrix<double>.Build.SparseOfColumnArrays(Fcols);

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
