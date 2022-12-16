using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Optimization;
using Newtonsoft.Json;
using FDMremote.Utilities;
using WebSocketSharp;

namespace FDMremote.GH_Optimization
{
    public class FDMsend : GH_Component
    {
        bool Finished = false;
        int Iter = 0;
        double Loss = 0.0;
        List<double> Q = new List<double>();
        List<Curve> Curves = new List<Curve>();
        Network network = new Network();
        Network outNetwork = new Network();
        string optiminfo = "";
        /// <summary>
        /// Initializes a new instance of the Optimize class.
        /// </summary>
        public FDMsend()
          : base("FDMremote Send", "FDMSend",
              "send data to FDMremote.jl server",
              "FDMremote", "Optimization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Network to Optimize", GH_ParamAccess.item); // Network
            pManager.AddGenericParameter("Optimization Parameters", "Params", "Objective functions, tolerances, etc.", GH_ParamAccess.item); // all other parameters
            pManager.AddVectorParameter("Load", "P", "Applied load", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Connect", "Connect", "Open/Close connection", GH_ParamAccess.item, true);
            pManager.AddTextParameter("Host", "Host", "Host address", GH_ParamAccess.item, "127.0.0.1");
            pManager.AddTextParameter("Port", "Port", "Port ID", GH_ParamAccess.item, "2000");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "Msg", "Optimization data", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            //Network network = new Network();
            //OBJParameters objparams = new OBJParameters(0.1, 100.0, 1e-3, 1e-3, new List<OBJ> { new OBJTarget(1.0) }, true, 10, 500);
            OBJParameters objparams = new OBJParameters(0.1, 100.0, 1e-3, 1e-3, new List<OBJ>(), true, 10, 500);
            List<Vector3d> loads = new List<Vector3d>();
            bool status = true;

            if (!DA.GetData(0, ref network)) return;
            DA.GetData(1, ref objparams);
            if (!DA.GetDataList(2, loads)) return;
            if (!DA.GetData(3, ref status)) return;

            //connection info
            string host = "";
            string port = "";

            DA.GetData(4, ref host);
            DA.GetData(5, ref port);

            string address = "ws://" + host + ":" + port;

            //check that load count is correct
            if (loads.Count != 1)
            {
                if (loads.Count != network.N.Count)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of loads must be 1 or equal to number of free nodes");
                }
            }

            //make problem
            OptimizationProblem optimprob = new OptimizationProblem(network, objparams, loads);

            //just testing
            if (status)
            {
                ClearData();
                string data = JsonConvert.SerializeObject(optimprob);
                DA.SetData(0, data);
            }
            else
            {
                ClearData();
                DA.SetData(0, "CLOSE");
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
            get { return new Guid("35C14E73-6FE1-447D-BCED-27980386A096"); }
        }
    }
}