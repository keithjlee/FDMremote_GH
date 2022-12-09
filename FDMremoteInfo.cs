using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace FDMremote
{
    public class FDMremoteInfo : GH_AssemblyInfo
    {
        public override string Name => "FDMremote";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("9E10DF56-D71A-4BF5-891E-09855CFFE373");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}