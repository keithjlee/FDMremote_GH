using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Optimization;
using FDMremote.Analysis;
using Newtonsoft.Json;
using WebSocketSharp;
using FDMremote.Bengesht;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace FDMremote.GH_Optimization
{
    /// <summary>
    /// The main functionality of this class is from Bengesht by Behrooz Tahanzadeh
    /// Modifications are made to directly convert FDMremote objects into message
    /// </summary>
    public class FDMsend : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Optimize class.
        /// </summary>
        public FDMsend()
          : base("Remote Send", "FDMsend",
              "Send data to server; Bengesht design",
              "FDMremote", "Optimization")
        {
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Websocket Objects", "WSC", "websocket objects", GH_ParamAccess.item);
            pManager.AddGenericParameter("FDM Network", "Network", "Network to Optimize", GH_ParamAccess.item); // Network
            pManager.AddGenericParameter("Optimization Parameters", "Params", "Objective functions, tolerances, etc.", GH_ParamAccess.item); // all other parameters
            pManager.AddVectorParameter("Load", "P", "Applied load", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Close", "Close", "Close Server; must restart on server side if reconnecting", GH_ParamAccess.item, false);

            Param_GenericObject paramobj = (Param_GenericObject)pManager[2];
            OBJParameters p = new OBJParameters(0.1, 100, 1e-3, 1e-3, new List<OBJ> { new OBJNull() }, true, 20, 400);
            paramobj.PersistentData.Append(new GH_ObjectWrapper(p));

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "Msg", "Sent message", GH_ParamAccess.item) ; 
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            //Network network = new Network();
            OBJParameters objparams = new OBJParameters();
            List<Vector3d> loads = new List<Vector3d>();
            bool close = false;
            WsObject wscObj = new WsObject();
            Network network = new Network();

            if (!DA.GetData(0, ref wscObj)) return;
            if (!DA.GetData(1, ref network)) return;
            if (!DA.GetData(2, ref objparams)) return;
            if (!DA.GetDataList(3, loads)) return;
            if (!DA.GetData(4, ref close)) return;

            //check that load count is correct
            if (loads.Count != 1)
            {
                if (loads.Count != network.N.Count)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of loads must be 1 or equal to number of free nodes");
                }
            }

            OptimizationProblem optimprob = new OptimizationProblem(network, objparams, loads);
            if (!close) wscObj.send(JsonConvert.SerializeObject(optimprob));
            else wscObj.send("CLOSE");

            DA.SetData(0, JsonConvert.SerializeObject(optimprob));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.send;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("99C37348-2761-466F-BD63-2B2381150116"); }
        }
    }
}