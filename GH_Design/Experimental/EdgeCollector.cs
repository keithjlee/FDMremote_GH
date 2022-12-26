using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace FDMremote.GH_Design.Experimental
{
    public class EdgeCollector : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the EdgeCollector class.
        /// </summary>
        public EdgeCollector()
          : base("EdgeCollector", "Collector",
              "Description",
              "FDMremote", "Experimental")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param param = new Grasshopper.Kernel.Parameters.Param_Guid();
            pManager.AddCurveParameter("Edges", "E", "Edges of network", GH_ParamAccess.list);
            pManager.AddParameter(param, "Edge GUIDs", "GUIDs", "GUID of edges",
                GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var doc = RhinoDoc.ActiveDoc; // current rhino document

            var rhinocurves = doc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.Curve); // get all curves

            //var rhinopoints = doc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.Point);

            List<Curve> outcurves = new List<Curve>();
            //List<Point> outpoints = new List<Point>();
            List<Guid> outids = new List<Guid>();

            foreach (RhinoObject obj in rhinocurves)
            {
                Curve edge = (Curve)obj.Geometry;
                outcurves.Add(edge);

                Guid id = obj.Id;
                outids.Add(id);
            }

            //foreach (RhinoObject obj in rhinopoints)
            //{
            //    Point point = (Point)obj.Geometry;
            //    outpoints.Add(point);
            //}

            DA.SetDataList(0, outcurves);
            DA.SetDataList(1, outids);
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
            get { return new Guid("18B9231E-DF65-4F65-8F6B-0BF4814B8BE3"); }
        }
    }
}