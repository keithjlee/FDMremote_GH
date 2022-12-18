using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Collections;

namespace FDMremote.Experimental
{
    public class Topologize : GH_Component
    {
        List<Curve> Curves = new List<Curve>();
        Point3dList Nodes = new Point3dList();
        List<int[]> Indices = new List<int[]>();
        List<int> N = new List<int>();
        List<int> F = new List<int>();
        double tol = 0.1;

        /// <summary>
        /// Initializes a new instance of the Topologize class.
        /// </summary>
        public Topologize()
          : base("Topologize", "Top",
              "Efficient extraction of adjacency",
              "FDMremote", "Experimental")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Anchors", "A", "Anchor points", GH_ParamAccess.list);
            pManager.AddCurveParameter("Edges", "E", "Edges of network", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "tol", "Intersection detection tolerance", GH_ParamAccess.item, 0.1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "Nodes", "All nodes in network", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Fixed Indices", "F", "Indices of fixed nodes", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Free Indices", "N", "Indices of free nodes", GH_ParamAccess.list);
            pManager.AddTextParameter("Indices", "E", "Element indices", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curves", "C", "edges of network", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            List<Curve> edges = new List<Curve>();
            List<Point3d> anchors = new List<Point3d>();

            if (!DA.GetDataList(0, anchors)) return;
            if (!DA.GetDataList(1, edges)) return;
            DA.GetData(2, ref tol);

            Point3dList anchorlist = new Point3dList(anchors);

            GenerateTopology(anchorlist, edges);

            DA.SetDataList(0, Nodes);
            DA.SetDataList(1, F);
            DA.SetDataList(2, N);
            DA.SetData(3, Indices.ToString());
            DA.SetDataList(4, Curves);
            
        }

        private void GenerateTopology(Point3dList anchorlist, List<Curve> edges)
        {

            Nodes.Clear();
            Curves.Clear();
            N.Clear();
            F.Clear();
            Indices.Clear();
            //ExpireSolution(true);
            // Generate points
            foreach (Curve edge in edges)
            {
                
                // add to Curves
                Curves.Add(edge);

                Point3d start = edge.PointAtStart;
                int istart;
                int iend;
                Point3d end = edge.PointAtEnd;

                // if 'start' is a brand new point
                if (!Nodes.Contains(start))
                {
                    //add to pointlist
                    Nodes.Add(start);
                    //index of start point is length of list - 1
                    istart = Nodes.Count - 1;

                    //if new point, check if anhor
                    if (start.DistanceTo(Point3dList.ClosestPointInList(anchorlist, start)) < tol) F.Add(istart);
                    else N.Add(istart);

                }
                else //else assign the index for start
                {
                    istart = Nodes.ClosestIndex(start);
                }

                //check if anchor
                //check if anchor
                //if (anchorlist.Contains(start)) F.Add(istart);
                //else N.Add(istart);

                

                // same for end point
                if (!Nodes.Contains(end))
                {
                    Nodes.Add(end);
                    iend = Nodes.Count - 1;

                    //if new point, check if it is an anchor point
                    if (end.DistanceTo(Point3dList.ClosestPointInList(anchorlist, end)) < tol) F.Add(iend);
                    else N.Add(iend);
                }
                else
                {
                    iend = Nodes.ClosestIndex(end);
                }

                // add element indices
                Indices.Add(new int[] { istart, iend });
            }
           

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
            get { return new Guid("3C5DC76E-C35D-430A-9A24-99AD269FC458"); }
        }
    }
}