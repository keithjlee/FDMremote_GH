using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDMremote.Utilities;
using FDMremote.Analysis;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics;
using Rhino.Geometry;

namespace FDMremote.Experimental
{
    internal class FDMproblem_exp
    {
        public Network FDMnetwork;
        public Matrix<double> XYZf;
        public Matrix<double> P;
        public Matrix<double> C;
        public Matrix<double> Cn;
        public Matrix<double> Cf;
        public Matrix<double> Q;


        /// <summary>
        /// Fixed node position matrix
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

        private void GetPn(Vector3d p)
        {
            double[] pArray = new double[] { p.X, p.Y, p.Z }; //convert Vector3d into array of doubles

            //repeat load vector for free nodes
            List<double[]> pn = new List<double[]>();
            for (int i = 0; i < FDMnetwork.N.Count; i++)
            {
                pn.Add(pArray);
            }

            //create Pn matrix
            P = Matrix<double>.Build.DenseOfRowArrays(pn);
        }

        /// <summary>
        /// Load Matrix
        /// </summary>
        /// <param name="p"></param>
        /// <exception cref="ArgumentException"></exception>
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
                P = Matrix<double>.Build.DenseOfRowArrays(pn);
            }

        }
    }
}
