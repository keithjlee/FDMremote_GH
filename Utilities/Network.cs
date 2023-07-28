using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino.Collections;

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

            Topologize(Anchors, Curves, Tolerance);
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

            Topologize(Anchors, Curves, Tolerance);
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


            Topologize(Anchors, Curves, Tolerance);
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


            Topologize(Anchors, Curves, Tolerance);
            ValidCheck();
        }

        public List<Point3d> Anchors;//input
        public List<Curve> Curves;//input
        public List<double> ForceDensities; // input
        public double Tolerance; //input
        public List<Point3d> Points; //Topologize
        public List<double[]> XYZ; //Topologize
        public List<int[]> Indices; //Topologize
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
        public List<int> N; //Topologize
        public List<int> F; //Topologize
        public bool Valid; //ValidCheck()

        private void Topologize(List<Point3d> anchors, List<Curve> edges, double tolerance)
        {
            //initialize fields
            Points = new List<Point3d>();
            XYZ = new List<double[]>();
            Indices = new List<int[]>();
            N = new List<int>();
            F = new List<int>();

            //temporary variables
            Point3dList anchorlist = new Point3dList(anchors);
            Point3dList pointlist = new Point3dList();

            foreach (Curve edge in edges)
            {
                Point3d start = edge.PointAtStart;
                Point3d end = edge.PointAtEnd;
                int istart;
                int iend;

                // analyze starting point
                //if (!pointlist.Contains(start))
                if (!WithinTolerance(pointlist, start, tolerance))
                {
                    pointlist.Add(start);
                    istart = pointlist.Count - 1;

                    if (WithinTolerance(anchorlist, start, tolerance)) F.Add(istart);
                    else N.Add(istart);

                    XYZ.Add(new double[] { start.X, start.Y, start.Z });
                }
                else
                {
                    istart = pointlist.ClosestIndex(start);
                }

                // analyze end point
                //if (!pointlist.Contains(end))
                if (!WithinTolerance(pointlist, end, tolerance))
                {
                    pointlist.Add(end);
                    iend = pointlist.Count - 1;

                    if (WithinTolerance(anchorlist, end, tolerance)) F.Add(iend);
                    else N.Add(iend);

                    XYZ.Add(new double[] {end.X, end.Y, end.Z });
                }
                else
                {
                    iend = pointlist.ClosestIndex(end);
                }

                Indices.Add(new int[] { istart, iend });

            }

            this.Points = new List<Point3d>(pointlist.ToArray());
        }

        private bool WithinTolerance(Point3dList points, Point3d point, double tolerance)
        {
            try
            {
                double dist = point.DistanceTo(Point3dList.ClosestPointInList(points, point));

                if (dist < tolerance) return true;
                else return false;
            }
            catch
            {
                return false;
            }
            
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
        }

        /// <summary>
        /// checks for sufficient number of anchors
        /// </summary>
        /// <returns></returns>
        public bool AnchorCheck()
        {
            if (this.Anchors.Count < 2) return false;
            else return true;
        }

        /// <summary>
        /// Checks that N and F were properly generated
        /// </summary>
        /// <returns></returns>
        public bool NFCheck()
        {
            if (this.F.Count != this.Anchors.Count || this.F.Count + this.N.Count != Nn) return false;
            else return true;
        }

        /// <summary>
        /// Checks if each element is given a valid node index
        /// </summary>
        /// <returns></returns>
        public bool IndexCheck()
        {
            if (this.Ne != this.Indices.Count) return false;
            else return true;
        }
    }
}
