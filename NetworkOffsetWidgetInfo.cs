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


            /*                                                                  +
 * V1(Point3D) + ----- | ----- | --- | ----- | ----- + V2(Point3D)  | w
 *                 X     W/A/D    S     W/A      Y                  +
 *
 * Suggested Parameter Ranges (in metres):
 * w = 0.3
 * X >= 0.4; Y >= 0.4
 * 0.4 <= W <= 1.6
 * 0.8 <= A <= 4.8
 * D = 0.9
 * S = 0.4
 *
 * Structure for different Edge Types:
 * SOLID = X-S-Y
 * WINDOW = X-{WSW}-Y
 * TRANSP = X-{WSW}-Y 1.2 <= W <= 3
 * PORTAL = X-{ASA}-Y
 * DOOR = X-D-Y
 * F = X-A-Y A:Max
 */
    }
}
