using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using Rhino;
using System.Linq;

namespace FDMremote.GH_Design
{
    public class PByLayer : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PByLayer class.
        /// </summary>
        public PByLayer()
          : base("PByLayer", "LayerP",
              "Assign a force magnitude value per layer",
              "FDMremote", "Design")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Network", "Network", "Network to apply values to", GH_ParamAccess.item);
            pManager.AddTextParameter("LayerNames", "Layers", "Names of layers", GH_ParamAccess.list, "Layer 01");
            pManager.AddNumberParameter("Values", "Vals", "Values to assign to points in layers", GH_ParamAccess.list, 1.0);
            pManager.AddNumberParameter("DefaultValues", "DefVals", "Default value of unassigned points", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Tolerance", "Tol", "Geometric tolerance for matching point to index", GH_ParamAccess.item, 0.1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Values", "Vals", "Values in order of node indices", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            Network network = new Network();
            List<string> names = new List<string>();
            List<double> values = new List<double>();
            double def = 0.0;
            double tol = 0.1;

            //assign
            if (!DA.GetData(0, ref network)) return;
            DA.GetDataList(1, names);
            DA.GetDataList(2, values);
            DA.GetData(3, ref def);
            DA.GetData(4, ref tol);

            //check inputs
            if (names.Count != values.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length of layer names must equal length of values");
            }

            //link to rhino document
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            //initialize output i
            List<double> outval = Enumerable.Repeat(def, network.N.Count).ToList();
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
            get { return new Guid("BB0181BF-FC23-4861-B6CA-E0DDB08AE6D6"); }
        }
    }
}