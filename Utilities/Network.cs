using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino.Collections;
using Newtonsoft.Json;

namespace FDMremote.Utilities
{
    internal class Network
    {
        public Network()
        {

        }
        /// <summary>
        /// Geometric constructor
        /// </summary>
        /// <param name="anchors"></param>
        /// <param name="curves"></param>
        public Network(List<Point3d> anchors, List<Curve> curves, double tol)
        {
            Anchors = anchors;
            Curves = curves;
            Tolerance = tol;
            ForceDensities = new List<double>();

            //default force density
            for (int i = 0; i < curves.Count; i++)
            {
                ForceDensities.Add(1.0);
            }

            GetNF(Tolerance);
            ValidCheck();
        }

        /// <summary>
        /// single q constructor
        /// </summary>
        /// <param name="points"></param>
        /// <param name="curves"></param>
        /// <param name="q"></param>
        public Network(List<Point3d> anchors, List<Curve> curves, double q, double tol)
        {
            Anchors = anchors;
            Curves = curves;
            Tolerance = tol;
            ForceDensities = new List<double>();

            //default force density
            for (int i = 0; i < curves.Count; i++)
            {
                ForceDensities.Add(q);
            }

            GetNF(Tolerance);
            ValidCheck();
        }

        /// <summary>
        /// Unique constructor
        /// </summary>
        /// <param name="points"></param>
        /// <param name="curves"></param>
        /// <param name="q"></param>
        /// <exception cref="ArgumentException"></exception>
        public Network(List<Point3d> anchors, List<Curve> curves, List<double> q, double tol)
        {
            if (curves.Count != q.Count && q.Count != 1)
            {
                throw new ArgumentException("force densities and number of elements must match");
            }

            Anchors = anchors;
            Curves = curves;
            ForceDensities = new List<double>();
            Tolerance = tol;

            if (q.Count != 1)
            {
                for (int i = 0; i < curves.Count; i++)
                {
                    ForceDensities.Add(q[i]);
                }
            }
            else
            {
                for (int i = 0; i < curves.Count; i++)
                {
                    ForceDensities.Add(q[0]);
                }
            }
            

            GetNF(Tolerance);
            ValidCheck();
        }

        /// <summary>
        /// Copy of an existing network
        /// </summary>
        /// <param name="other"></param>
        public Network(Network other)
        {
            Anchors = other.Anchors;
            Curves = other.Curves;
            ForceDensities = other.ForceDensities;
            Tolerance = other.Tolerance;


            GetNF(Tolerance);
            ValidCheck();
        }

        public List<Point3d> Anchors { get;  set; }
        public List<Curve> Curves { get; set; }
        public List<double> ForceDensities { get; set; }
        public double Tolerance { get; set; }
        public List<Point3d> Points
        {
            get
            {
                return GetPoints(Curves, Tolerance);
            }
        }
        public List<double[]> XYZ
        {
            get
            {
                return GetXYZ(Points);
            }
        }
        public List<int[]> Indices
        {
            get
            {
                return GetIndices(Curves, Points);
            }
        }
        public int Ne 
        { 
            get
            {
                return Curves.Count;
            }
        }
        public int Nn
        {
            get
            {
                return Points.Count;
            }
        }
        public List<int> N;
        public List<int> F;
        public bool Valid;

        /// <summary>
        /// Extracts the list of all points in network
        /// </summary>
        private List<Point3d> GetPoints(List<Curve> curves, double tol)
        {
            List<Point3d> duplicatedPoints = new List<Point3d>();
            foreach (Curve curve in curves)
            {
                Point3d start = curve.PointAtStart;
                Point3d end = curve.PointAtEnd;

                duplicatedPoints.Add(start);
                duplicatedPoints.Add(end);
            }
            Point3dList uniquePoints = new Point3dList(Point3d.CullDuplicates(duplicatedPoints, tol));

            List<Point3d> uniquePointsList = new List<Point3d>(uniquePoints.ToArray());

            return uniquePointsList;
        }

        /// <summary>
        /// Generates the list of start/end indices for each element w/r/t order of Points 
        /// </summary>
        private List<int[]> GetIndices(List<Curve> curves, List<Point3d> points)
        {
            List<int[]> Indices = new List<int[]>();

            foreach (Curve curve in curves)
            {
                int[] index = new int[2]; // initialize

                Point3d startPoint = curve.PointAtStart;
                int startIndex = Point3dList.ClosestIndexInList(points, startPoint);
                index[0] = startIndex;

                Point3d endPoint = curve.PointAtEnd;
                int endIndex = Point3dList.ClosestIndexInList(points, endPoint);
                index[1] = endIndex;

                Indices.Add(index);
            }

            return Indices;
        }

        /// <summary>
        /// Extracts the indices of free (N) and fixed nodes
        /// </summary>
        private void GetNF(double tol)
        {
            N = new List<int>();
            F = new List<int>();

            // Point3dList anchorList = new Point3dList(Anchors);

            for (int i = 0; i < Points.Count; i++)
            {
                Point3d point = Points[i];

                if (point.DistanceTo(Point3dList.ClosestPointInList(Anchors, point)) < tol) F.Add(i);
                else N.Add(i);

                //if (anchorList.Contains(point)) F.Add(i);
                //else N.Add(i);
            }
        }



        /// <summary>
        /// List of arrays of XYZ positions
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public List<double[]> GetXYZ(List<Point3d> points)
        {
            List<double[]> result = new List<double[]>();

            foreach (Point3d point in points)
            {
                double[] xyz = new double[] { point.X, point.Y, point.Z };
                result.Add(xyz);
            } 

            return result;
        }

        private void ValidCheck()
        {
            // must have fixed nodes
            if (this.Anchors.Count <= 1) this.Valid = false;

            // check N/F was properly captures
            else if (this.F.Count != this.Anchors.Count || this.F.Count + this.N.Count != Nn) this.Valid = false;

            // all curves have indices
            else if (this.Ne != this.Indices.Count) this.Valid = false;

            // all conditions pass
            else this.Valid = true;

            //try
            //{
            //    bool validity = F.Count == Anchors.Count && F.Count + N.Count == Nn;
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("Something went wrong when capturing N and F indices");
            //}
        }

        /// <summary>
        /// checks for sufficient number of anchors
        /// </summary>
        /// <returns></returns>
        public bool AnchorCheck()
        {
            if (this.Anchors.Count < 3) return false;
            else return true ;
        }

        /// <summary>
        /// Checks that N and F were properly generated
        /// </summary>
        /// <returns></returns>
        public bool NFCheck()
        {
            if (this.F.Count != this.Anchors.Count || this.F.Count + this.N.Count != Nn) return false;
            else return true ;
        }

        /// <summary>
        /// Checks if each element is given a valid node index
        /// </summary>
        /// <returns></returns>
        public bool IndexCheck()
        {
            if (this.Ne != this.Indices.Count) return false;
            else return true ;
        }


    }
}
