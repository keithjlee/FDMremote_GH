using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using FDMremote.Utilities;
using Rhino.Collections;

namespace FDMremote.Analysis
{
    internal class Solver
    {
        /// <summary>
        /// Main solver
        /// </summary>
        /// <param name="fdm"></param>
        /// <returns></returns>
        //public static Matrix<double> Solve(FDMproblem fdm)
        //{
        //    // extract variables
        //    var Cn = fdm.Cn;
        //    var Cf = fdm.Cf;
        //    var XYZf = fdm.XYZf;
        //    var Pn = fdm.Pn;
        //    var Q = fdm.Q;

        //    var A = Cn.TransposeThisAndMultiply(Q) * Cn; // LHS
        //    var b = Pn - (Cn.TransposeThisAndMultiply(Q) * Cf * XYZf); // RHS

        //    return A.Cholesky().Solve(b);
        //}

        public static Matrix<double> Solve(FDMproblem fdm, Matrix<double> P)
        {
            // extract variables
            var Cn = fdm.Cn;
            var Cf = fdm.Cf;
            var XYZf = fdm.XYZf;
            var Q = fdm.Q;

            var A = Cn.TransposeThisAndMultiply(Q) * Cn; // LHS
            var b = P - (Cn.TransposeThisAndMultiply(Q) * Cf * XYZf); // RHS

            return A.Cholesky().Solve(b);
        }

        /// <summary>
        /// Takes in a FDMproblem and returns a new network 
        /// </summary>
        /// <param name="fdm"></param>
        /// <returns></returns>
        //public static Network SolvedNetwork(FDMproblem fdm)
        //{
        //    // solve for new positions
        //    var newPositions = Solve(fdm);
        //    // Number of free nodes
        //    int nFree = fdm.FDMnetwork.N.Count;

        //    // create new points
        //    List<Point3d> newPoints = new List<Point3d>();
        //    for(int i = 0; i < nFree; i++)
        //    {
        //        var values = newPositions.Row(i);
        //        Point3d point = new Point3d(values[0], values[1], values[2]);
        //        newPoints.Add(point);
        //    }

        //    // make new list of points
        //    List<Point3d> points = NewPoints(fdm, newPositions);

        //    // update curves
        //    List<Curve> curves = NewCurves(fdm, points);

        //    // copy all anchors
        //    List<Point3d> anchors = new List<Point3d>(fdm.FDMnetwork.Anchors.Count);
        //    fdm.FDMnetwork.Anchors.ForEach((item) =>
        //    {
        //        anchors.Add(new Point3d(item));
        //    });

        //    // copy force densities
        //    List<double> q = new List<double>(fdm.FDMnetwork.ForceDensities.Count);
        //    fdm.FDMnetwork.ForceDensities.ForEach((item) =>
        //    {
        //        q.Add(item);
        //    });

        //    Network network = new Network(anchors, curves, q, fdm.Tolerance);

        //    return network;
        //}

        public static Network SolvedNetwork(FDMproblem fdm, Matrix<double> P)
        {
            // solve for new positions
            var newPositions = Solve(fdm, P);
            // Number of free nodes
            int nFree = fdm.FDMnetwork.N.Count;

            // create new points
            List<Point3d> newPoints = new List<Point3d>();
            for (int i = 0; i < nFree; i++)
            {
                var values = newPositions.Row(i);
                Point3d point = new Point3d(values[0], values[1], values[2]);
                newPoints.Add(point);
            }

            // make new list of points
            List<Point3d> points = NewPoints(fdm, newPositions);

            // update curves
            List<Curve> curves = NewCurves(fdm, points);

            // copy all anchors
            List<Point3d> anchors = new List<Point3d>(fdm.FDMnetwork.Anchors.Count);
            fdm.FDMnetwork.Anchors.ForEach((item) =>
            {
                anchors.Add(new Point3d(item));
            });

            // copy force densities
            List<double> q = new List<double>(fdm.FDMnetwork.ForceDensities.Count);
            fdm.FDMnetwork.ForceDensities.ForEach((item) =>
            {
                q.Add(item);
            });

            Network network = new Network(anchors, curves, q, fdm.Tolerance);

            return network;
        }

        /// <summary>
        /// Moves the end points of a collection of curves to reflect solved positions
        /// </summary>
        /// <param name="fdm"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        private static List<Curve> NewCurves(FDMproblem fdm, List<Point3d> points)
        {
            // make curves
            List<Curve> curves = new List<Curve>();
            var indices = fdm.FDMnetwork.Indices;
            for (int i = 0; i < fdm.FDMnetwork.Ne; i++)
            {
                var index = indices[i];
                var p1 = points[index[0]];
                var p2 = points[index[1]];

                var line = new LineCurve(p1, p2);

                curves.Add(line);
            }

            return curves;
        }

        public static List<Curve> NewCurves(Network fdm, List<Point3d> points)
        {
            // make curves
            List<Curve> curves = new List<Curve>();
            var indices = fdm.Indices;
            for (int i = 0; i < fdm.Ne; i++)
            {
                var index = indices[i];
                var p1 = points[index[0]];
                var p2 = points[index[1]];

                var line = new LineCurve(p1, p2);

                curves.Add(line);
            }

            return curves;
        }

        /// <summary>
        /// Generates a complete list of ordered points to reflect a solved network
        /// </summary>
        /// <param name="fdm"></param>
        /// <param name="xyzN"></param>
        /// <returns></returns>
        private static List<Point3d> NewPoints(FDMproblem fdm, Matrix<double> xyzN)
        {
            //fdm is the FDMproblem under analysis
            //xyzN is the new free positions from Analysis.Solve()
            // Number of free nodes
            int nFree = fdm.FDMnetwork.N.Count;

            // create new points
            List<Point3d> newPoints = new List<Point3d>();
            for (int i = 0; i < nFree; i++)
            {
                var values = xyzN.Row(i);
                Point3d point = new Point3d(values[0], values[1], values[2]);
                newPoints.Add(point);
            }

            // deep copy data

            // copy all points
            List<Point3d> points = new List<Point3d>(fdm.FDMnetwork.Points.Count);
            fdm.FDMnetwork.Points.ForEach((item) =>
            {
                points.Add(new Point3d(item));
            });

            // update points
            for (int i = 0; i < fdm.FDMnetwork.N.Count; i++)
            {
                var index = fdm.FDMnetwork.N[i];
                points[index] = newPoints[i];
            }

            return points;
        }

        /// <summary>
        /// Generates the sparse connectivity matrix C of a network
        /// </summary>
        /// <param name="FDMnetwork"></param>
        /// <returns></returns>
        public static Matrix<double> GetC(Network FDMnetwork)
        {
            // Initialize
            var C = Matrix<double>.Build.Sparse(FDMnetwork.Ne, FDMnetwork.Nn);

            // Populate
            for (int i = 0; i < FDMnetwork.Ne; i++)
            {
                var index = FDMnetwork.Indices[i];
                C[i, index[0]] = -1;
                C[i, index[1]] = 1;
            }

            return C;

        }

        /// <summary>
        /// Converts a list of Point3d to a list of [x,y,z] arrays
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<double[]> Point2Array(List<Point3d> points)
        {
            //initialize
            List<double[]> result = new List<double[]>();

            //populate
            foreach (Point3d point in points)
            {
                var arr = new double[] { point.X, point.Y, point.Z };
                result.Add(arr);
            }

            return result;
        }

        /// <summary>
        /// Converts a list of [x,y,z] arrays to a list of Point3d
        /// </summary>
        /// <param name="arrs"></param>
        /// <returns></returns>
        public static List<Point3d> Array2Point(List<double[]> arrs)
        {
            List<Point3d> result = new List<Point3d>();

            foreach (double[] arr in arrs)
            {
                Point3d point = new Point3d(arr[0], arr[1], arr[2]);
                result.Add(point);
            }

            return result;
        }

        /// <summary>
        /// Gets the ordered element forces for a given network; assumes existing edge lengths are at stressed state
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public static List<double> Forces(Network network)
        {
            //connectivity matrix
            var C = GetC(network);
            var xyz_list = Point2Array(network.Points); 
            var xyz_matrix = Matrix<double>.Build.DenseOfRowArrays(xyz_list);

            //elemental vectors (per row)
            var CXYZ = C * xyz_matrix;

            //get member lengths
            List<double> lengths = new List<double>();
            for (int i = 0; i < CXYZ.RowCount; i++)
            {
                var length = CXYZ.Row(i).L2Norm();
                lengths.Add(length);
            }

            //F = q * l
            List<double> forces = new List<double>();
            for (int i = 0; i < network.Ne; i++)
            {
                var force = lengths[i] * network.ForceDensities[i];
                forces.Add(force);
            }

            return forces;

        }

        public static List<double> Forces(List<Curve> curves, List<Double> densities)
        {
            if (curves.Count != densities.Count) return null;

            List<double> forces = new List<double>();

            for (int i = 0; i < curves.Count; i++)
            {
                forces.Add(curves[i].GetLength() * densities[i]);
            }

            return forces;
        }

        public static Matrix<double> PMaker(Vector3d load, List<int> N)
        {
            double[] pArray = new double[] { load.X, load.Y, load.Z };
            List<double[]> pn = new List<double[]>();

            for (int i = 0;i < N.Count; i++)
            {
                pn.Add(pArray);
            }

            return Matrix<double>.Build.DenseOfRowArrays(pn);
        }

        public static Matrix<double> PMaker(List<Vector3d> loads, List<int> N)
        {
            if (loads.Count != N.Count && loads.Count != 1) throw new ArgumentException("Length of force vectors must be 1 or match length of free nodes.");

            if (loads.Count == 1) return PMaker(loads[0], N);

            else
            {
                //repeat load vector for free nodes
                List<double[]> pn = new List<double[]>();
                for (int i = 0; i < N.Count; i++)
                {
                    Vector3d currentP = loads[i];
                    double[] pArray = new double[] { currentP.X, currentP.Y, currentP.Z }; //convert Vector3d into array of doubles
                    pn.Add(pArray);
                }

                //create Pn matrix
                return Matrix<double>.Build.DenseOfRowArrays(pn);
            }
        }

        public static List<Vector3d> Reactions(Network network)
        {
            List<double> internalforces = Forces(network.Curves, network.ForceDensities);
            Point3dList startpoints = new Point3dList();
            Point3dList endpoints = new Point3dList();

            List<Vector3d> anchorforces = new List<Vector3d>();

            foreach (Curve curve in network.Curves)
            {
                startpoints.Add(curve.PointAtStart);
                endpoints.Add(curve.PointAtEnd);
            }

            for (int i = 0; i < network.F.Count; i++)
            {
                var index = network.F[i];
                var point = network.Points[index];

                for (int j = 0; j < startpoints.Count; j++)
                {
                    if (point.DistanceTo(startpoints[j]) <= network.Tolerance)
                    {
                        Vector3d force = new Vector3d(network.Curves[j].PointAtEnd - network.Curves[j].PointAtStart);

                        if (anchorforces.Count > i && anchorforces[i] != null) anchorforces[i] -= internalforces[j] * force / force.Length;
                        else anchorforces.Add(-internalforces[j] * force / force.Length);

                    }

                    if (point.DistanceTo(endpoints[j]) <= network.Tolerance)
                    {
                        Vector3d force = new Vector3d(network.Curves[j].PointAtEnd - network.Curves[j].PointAtStart);

                        if (anchorforces.Count > i && anchorforces[i] != null) anchorforces[i] += internalforces[j] * force / force.Length;
                        else anchorforces.Add(internalforces[j] * force / force.Length);

                    }
                }
            }

            return anchorforces;

        }
    }
}
