using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace FDMremote.GH_Design.Experimental
{
    public class QByLayer : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the QByLayer class.
        /// </summary>
        public QByLayer()
          : base("QByLayer", "LayerQ",
              "Assign a force density value per layer",
              "FDMremote", "Experimental")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param param = new Grasshopper.Kernel.Parameters.Param_Guid();
            pManager.AddParameter(param, "Edge GUIDs", "GUIDs", "GUIDs of input network edges", GH_ParamAccess.list);
            pManager.AddTextParameter("LayerNames", "Layers", "Names of layers", GH_ParamAccess.list, "Default");
            pManager.AddNumberParameter("ForceDensities", "Q", "Force density of layer", GH_ParamAccess.list, 1);
            pManager.AddNumberParameter("DefaultQ", "Qdefault", "Force densities of unselected edges", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("ForceDensities", "Q", "Force densities of network edges", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Guid> guids = new List<Guid>();
            List<string> names = new List<string>();
            List<double> values = new List<double>();
            double def = 0.0;

            if (!DA.GetDataList(0, guids)) return ;
            if (!DA.GetDataList(1, names)) return ;
            if (!DA.GetDataList(2, values)) return ;
            DA.GetData(3, ref def);

            if (names.Count != values.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length of layer names must equal length of values");
            }

            //rhino document
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;

            //initialize list
            List<double> outQ = Enumerable.Repeat(def, guids.Count).ToList();

            for (int i = 0; i < names.Count; i++)
            {
                //select all in layer
                Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(names[i]);

                //select q value
                double q = values[i];

                //replace outQ value with desired value
                foreach (RhinoObject obj in rhobjs)
                {
                    Guid id = obj.Id;
                    
                    int index = guids.IndexOf(id);

                    outQ[index] = q;
                }
            }

            DA.SetDataList(0, outQ);
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
            get { return new Guid("4A4E0389-4665-483B-8572-DB19628D1369"); }
        }
    }
}