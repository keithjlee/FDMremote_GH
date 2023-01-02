using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using EigenCore;
using FDMremote.Utilities;
using EigenCore.Core.Dense;
using EigenCore.Core.Sparse;
using FDMremote.Analysis;

namespace FDMremote.GH_Design.Experimental
{
    public class AnalyzeEigen : GH_Component
    {
        private MatrixXD XYZf;
        private SparseMatrixD Cn;
        private SparseMatrixD Cf;
        private SparseMatrixD Q;
        private MatrixXD P;
        private List<Curve> curves;

        /// <summary>
        /// Initializes a new instance of the AnalyzeEigen class.
        /// </summary>
        public AnalyzeEigen()
          : base("AnalyzeNetworkEigen", "AnalyzeEigen",
              "Analyze a FDM network using EigenCore",
              "FDMremote", "Experimental")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Input FDM network", GH_ParamAccess.item);
            pManager.AddVectorParameter("Load Vector", "P", "Load Vector", GH_ParamAccess.list, new Vector3d(0, 0, 0));
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Solved FDM network", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //read
            Network network = new Network();
            List<Vector3d> loads = new List<Vector3d>();

            if (!DA.GetData(0, ref network)) return;
            DA.GetDataList(1, loads);

            //copy data
            var anchors = new List<Point3d>(network.Anchors.Count);
            network.Anchors.ForEach((item) =>
            {
                anchors.Add(new Point3d(item));
            });
            var q = new List<double>(network.ForceDensities.Count);
            network.ForceDensities.ForEach((item) =>
            {
                q.Add(item);
            });

            //solve
            GetXYZf(network);
            GetQ(network);
            GetP(loads, network.N);
            GetC(network);

            Solve(network);

            Network outnetwork = new Network(anchors, curves, q, network.Tolerance);

            DA.SetData(0, outnetwork);
        }

        private void Solve(Network network)
        {
            

            var A = Cn.Transpose().Mult(Q).Mult(Cn);
            var B = P.Minus(Cn.Transpose().Mult(Q).Mult(Cf).ToDense().Mult(XYZf));

            // split up RHS matrix into vectors
            var Bx = B.Col(0);
            var By = B.Col(1);
            var Bz = B.Col(2);

            //solve
            var x1 = A.DirectSolve(Bx, EigenCore.Core.Sparse.LinearAlgebra.DirectSolverType.SparseLU);
            var x2 = A.DirectSolve(By, EigenCore.Core.Sparse.LinearAlgebra.DirectSolverType.SparseLU);
            var x3 = A.DirectSolve(Bz, EigenCore.Core.Sparse.LinearAlgebra.DirectSolverType.SparseLU);


            // new curves
            List<Point3d> points = new List<Point3d>();
            new List<Point3d>(network.Points.Count);
            network.Points.ForEach((item) =>
            {
                points.Add(new Point3d(item));
            });

            for (int i = 0; i < network.N.Count; i++)
            {
                Point3d point = new Point3d(x1.Get(i), x2.Get(i), x3.Get(i));

                int index = network.N[i];

                points[index] = point;
            }

            // new curves
            curves = new List<Curve>();
            var indices = network.Indices;
            for (int i = 0; i < network.Ne; i++)
            {
                var index = indices[i];
                var p1 = points[index[0]];
                var p2 = points[index[1]];

                var line = new LineCurve(p1, p2);

                curves.Add(line);
            }

        }

        private void GetXYZf(Network network)
        {
            double[][] xyzfArray = new double[network.F.Count][];

            for (int i = 0; i < network.F.Count; i++)
            {
                int index = network.F[i];
                xyzfArray[i] = network.XYZ[index];
            }

            XYZf = new MatrixXD(xyzfArray);
        }

        private void GetQ(Network network)
        {
            Q = SparseMatrixD.Diag(network.ForceDensities.ToArray());
        }

        private void GetP(List<Vector3d> loads, List<int> N)
        {
            double[][] pArray = new double[N.Count][];

            if (loads.Count == 1)
            {
                Vector3d load = loads[0];
                double[] p = new double[] { load.X, load.Y, load.Z };

                for (int i = 0; i < N.Count; i++)
                {
                    pArray[i] = p;
                }
            }
            else
            {
                for (int i = 0; i < N.Count; i++)
                {
                    Vector3d load = loads[i];
                    double[] p = new double[] { load.X, load.Y, load.Z };

                    pArray[i] = p;
                }
            }
            

            P = new MatrixXD(pArray);
        }

        private void GetC(Network network)
        {

            List<(int, int, double)> cn = new List<(int, int, double)>();
            List<(int, int, double)> cf = new List<(int, int, double)>();

            for (int i = 0; i < network.Ne; i++)
            {
                int row = i;

                var index = network.Indices[i];

                if (network.F.Contains(index[0]))
                {
                    int col = network.F.FindIndex(x => x == index[0]);
                    cf.Add((row, col, -1));
                }
                else
                {
                    int col = network.N.FindIndex(x => x == index[0]);
                    cn.Add((row, col, -1));
                }

                if (network.F.Contains(index[1]))
                {
                    int col = network.F.FindIndex(x => x == index[0]);
                    cf.Add((row, col, 1));
                }
                else
                {
                    int col = network.N.FindIndex(x => x == index[0]);
                    cn.Add((row, col, 1));
                }
            }

            Cn = new SparseMatrixD(cn, network.Ne, network.N.Count);
            Cf = new SparseMatrixD(cf, network.Ne, network.F.Count);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1EFD645A-DFB3-4F89-8A8F-988E0C05FB08"); }
        }
    }
}