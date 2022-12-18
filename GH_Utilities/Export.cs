using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Newtonsoft.Json;
using FDMremote.Utilities;
using FDMremote.Analysis;
using MathNet.Numerics.LinearAlgebra;

namespace FDMremote.GH_Utilities
{
    public class Export : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Export class.
        /// </summary>
        public Export()
          : base("Export Data", "Export",
              "Export network information to JSON format",
              "FDMremote", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Network", "Network", "FDM network to export", GH_ParamAccess.item);
            pManager.AddVectorParameter("Load", "P", "Load vector", GH_ParamAccess.list, new Vector3d(0, 0, 0));
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Export Data", "JSON", "JSON text file of FDM network", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Network network = new Network();
            List<Vector3d> loads = new List<Vector3d>();

            if (!DA.GetData(0, ref network)) return;
            DA.GetDataList(1, loads);

            Matrix<double> P = Solver.PMaker(loads, network.N);

            InformationObject obj = new InformationObject(network, P, 0);
            string data = JsonConvert.SerializeObject(obj);

            DA.SetData(0, data);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.export;
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D5140A2A-28EA-47A6-A898-25E217E12129"); }
        }
    }
}