using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace NetworkOffsetWidget
{
    public class NetworkOffsetWidgetComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public NetworkOffsetWidgetComponent()
          : base("NetworkOffsetWidget", "NFW",
              "Networ Offset Widget.",
              "Extra", "User")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want 
            // to import lists or trees of values, modify the ParamAccess flag.

            pManager.AddGeometryParameter("EdgesAsList", "E", "Edges", GH_ParamAccess.list);
            pManager.AddNumberParameter("WeightsAsList", "W", "Widths", GH_ParamAccess.list);
            pManager.AddNumberParameter("NodeRadius", "R", "Node Radius", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Bake", "B", "Bake", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Seed", "S", "Seed", GH_ParamAccess.item);


            // If you want to change properties of certain parameters, 
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddLineParameter("NodesGeometry", "NG", "Nodes Geometry", GH_ParamAccess.list);
            pManager.AddLineParameter("EdgesGeometry", "EG", "Edges Geometry", GH_ParamAccess.list);
            pManager.AddTextParameter("Message", "msg", "Message", GH_ParamAccess.item);
            

            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            // We'll start by declaring variables and assigning them starting values.
            List<Line> Edges = new List<Line>();
            List<Line> NodesGeometry = new List<Line>();
            List<Line> EdgesGeometry = new List<Line>();
            bool bake = false;
            int seed = 0;
            
            List<double> Widths = new List<double>();
            double NodeRadius = 0.3;

            // Then we need to access the input parameters individually. 
            // When data cannot be extracted from a parameter, we should abort this method.
            
            if (!DA.GetDataList(0, Edges)) return;
            if (!DA.GetDataList(1, Widths)) return;
            if (!DA.GetData(2, ref NodeRadius)) return;
            if (!DA.GetData(3, ref bake)) return;
            if (!DA.GetData(4, ref seed)) return;





            // We should now validate the data and warn the user if invalid data is supplied.

            // Main

            NFNetwork network = new NFNetwork(Edges, Widths, NodeRadius, new Random(seed));

            NodesGeometry = network.GetGeometries();

            NFGeometryCollector collector;
            network.GetEdgeGeometries(out collector);
            EdgesGeometry = collector.cutWindow;










            if (bake)
            {
                collector.Bake();
                DA.SetData(2, "Have a nice baking!");
            } else
            {
                DA.SetData(2, "wabawababubu");
            }
            

            // Finally assign the spiral to the output parameter.
            DA.SetDataList(0, NodesGeometry);
            DA.SetDataList(1, EdgesGeometry);

        }


        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a1ebc0d2-1b58-40da-8a05-5e179423a2da"); }
        }
    }
}
