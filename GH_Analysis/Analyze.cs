﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Analysis;
using Newtonsoft.Json;
using System.IO;

namespace FDMremote.GH_Analysis
{
    public class Analyze : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Analyze()
          : base("AnalyzeNetwork", "Analyze",
              "Analyze a FDM network",
              "FDMremote", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Input FDM network", GH_ParamAccess.item);
            pManager.AddVectorParameter("Load Vector", "P", "Load Vector", GH_ParamAccess.list);
            pManager.AddNumberParameter("IntersectionTolerance", "tol", "Geometric tolerance for connecting geometry", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Solved FDM network", GH_ParamAccess.item);
            pManager.AddTextParameter("Data", "Data", "Network Data", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //read
            Network fdmNetwork = new Network();
            List<Vector3d> loads = new List<Vector3d>();
            double tol = 1.0;
            
            if (!DA.GetData(0, ref fdmNetwork)) return;
            if (!DA.GetDataList(1, loads)) return;
            if (!DA.GetData(2, ref tol)) return;

            //analysis
            FDMproblem prob = new FDMproblem(fdmNetwork, loads, tol);

            //solving
            Network fdmSolved = Solver.SolvedNetwork(prob);

            //rough test
            InformationObject info = new InformationObject(fdmSolved, prob);
            string data = JsonConvert.SerializeObject(info);

            //return
            DA.SetData(0, fdmSolved);
            DA.SetData(1, data);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Analyze;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CECC5AD0-C2EF-4901-893A-F922617DF103"); }
        }
    }
}