using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace NetworkOffsetWidget
{
    public class NetworkOffsetWidgetInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "NetworkOffsetWidget";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("141457c1-e050-4b02-aa7e-63b32a237889");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
