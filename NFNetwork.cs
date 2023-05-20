using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;




namespace NetworkOffsetWidget
{
    class NFNetwork
    {
        public List<NFNode> nodes = new List<NFNode>();
        public List<NFEdge> edges = new List<NFEdge>();
        int CurrentNodeID = 0;
        int CurrentEdgeID = 0;
        public double NodeRadius;
        Random random;

        public NFNetwork(List<Line> EdgeLines, List<double> widths, double _NodeRadius, Random _random)
        {
            NodeRadius = _NodeRadius;
            random = _random;
            for (int i = 0; i < EdgeLines.Count; i++)
            {
                AddEdge(EdgeLines[i], widths[i]);
            }
        }

        public void AddEdge(Line line, double width)
        {
            if (nodes.Count == 0)
            {
                NFNode FromNode = new NFNode(line.From, CurrentNodeID, NodeRadius);
                FromNode.AddConnectivity(CurrentEdgeID);
                nodes.Add(FromNode);
                CurrentNodeID += 1;



                NFNode ToNode = new NFNode(line.To, CurrentNodeID, NodeRadius);
                ToNode.AddConnectivity(CurrentEdgeID);
                nodes.Add(ToNode);
                CurrentNodeID += 1;


                edges.Add(new NFEdge(line, CurrentEdgeID, CurrentNodeID - 2, CurrentNodeID - 1, width));
                CurrentEdgeID += 1;
            }
            else
            {
                bool HasFrom = false;
                bool HasTo = false;
                int FromNodeID = -1;
                int ToNodeID = -1;


                foreach (NFNode node in nodes)
                {
                    if (node.IsIdentical(line.From))
                    {
                        FromNodeID = node.ID;
                        node.AddConnectivity(CurrentEdgeID);
                        HasFrom = true;
                    }

                    if (node.IsIdentical(line.To))
                    {
                        ToNodeID = node.ID;
                        node.AddConnectivity(CurrentEdgeID);
                        HasTo = true;
                    }

                    if (HasFrom && HasTo)
                    {
                        break;
                    }

                }

                if (!HasFrom)
                {
                    NFNode Node = new NFNode(line.From, CurrentNodeID, NodeRadius);
                    Node.AddConnectivity(CurrentEdgeID);
                    nodes.Add(Node);
                    FromNodeID = CurrentNodeID;
                    CurrentNodeID += 1;
                }

                if (!HasTo)
                {
                    NFNode Node = new NFNode(line.To, CurrentNodeID, NodeRadius);
                    Node.AddConnectivity(CurrentEdgeID);
                    nodes.Add(Node);
                    ToNodeID = CurrentNodeID;
                    CurrentNodeID += 1;
                }

                edges.Add(new NFEdge(line, CurrentEdgeID, FromNodeID, ToNodeID, width));
                CurrentEdgeID += 1;

            }

        }

        public List<Line> GetGeometries()
        {
            List<Line> Geometries = new List<Line>();
            foreach (NFNode node in nodes)
            {
                Geometries.AddRange(node.Geometry(edges));
            }
            return Geometries;
        }

        public void GetEdgeGeometries(out NFGeometryCollector collector)
        {
            collector = new NFGeometryCollector();
            // try turning ref into out? and new() evry collctr
            List<Line> Geometries = new List<Line>();
            foreach (NFEdge edge in edges)
            {
                //placeholder 0519
                edge.AddGeometry(SGSemanticsRules.RetailFront , this, ref collector, random);


            }
        }
    }

    class NFNode
    {
        public Point3d point;
        public int ID;
        public List<int> EdgeConnectivities = new List<int>();
        public List<bool> EdgeConInverted = new List<bool>();
        double tolerance = 0.01;
        public double radius;



        public NFNode(Point3d _point, int _ID, double _radius)
        {
            point = _point;
            ID = _ID;
            radius = _radius;
        }

        public bool AddConnectivity(int EdgeID)
        {
            if (EdgeConnectivities.Contains(EdgeID))
            {
                return false;
            }
            else
            {
                EdgeConnectivities.Add(EdgeID);
                return true;
            }
        }

        public bool IsIdentical(Point3d testPoint)
        {
            if (testPoint.DistanceTo(point) < tolerance)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public void SortConnectivity(List<NFEdge> Edges)
        {
            List<int> sorted = new List<int>();
            sorted.Add(EdgeConnectivities[0]);
            EdgeConnectivities.RemoveAt(0);

            while (EdgeConnectivities.Count > 0)
            {
                int a = EdgeConnectivities[0];
                bool added = false;
                EdgeConnectivities.RemoveAt(0);
                for (int i = 0; i < sorted.Count; i++)
                {
                    if (!EdgeCompare(Edges[a], Edges[sorted[i]]))
                    {
                        sorted.Insert(i, a);
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    sorted.Add(a);
                }
            }

            EdgeConnectivities = sorted;

        }


        public bool EdgeCompare(NFEdge EdgeA, NFEdge EdgeB)
        {
            Vector3d vec1, vec2;
            double a1, a2;

            vec1 = VectorFromEdge(EdgeA);
            vec2 = VectorFromEdge(EdgeB);






            if (vec1.Y > 0)
            {
                a1 = Vector3d.VectorAngle(vec1, Vector3d.XAxis);
            }
            else if (vec1.Y < 0)
            {
                a1 = -Vector3d.VectorAngle(vec1, Vector3d.XAxis) + 2 * Math.PI;
            }
            else if (vec1.X > 0)
            {
                a1 = 0.0;
            }
            else
            {
                a1 = Math.PI;
            }


            if (vec2.Y > 0)
            {
                a2 = Vector3d.VectorAngle(vec2, Vector3d.XAxis);
            }
            else if (vec2.Y < 0)
            {
                a2 = -Vector3d.VectorAngle(vec2, Vector3d.XAxis) + 2 * Math.PI;
            }
            else if (vec2.X > 0)
            {
                a2 = 0.0;
            }
            else
            {
                a2 = Math.PI;
            }

            return (a1 >= a2);

        }

        public List<Line> Geometry(List<NFEdge> Edges)
        {
            List<Line> Geometries = new List<Line>();
            SortConnectivity(Edges);

            if (EdgeConnectivities.Count == 1)
            {
                Vector3d vec = VectorFromEdge(Edges[EdgeConnectivities[0]]);
                Point3d leftEntry, rightEntry, leftShoulder, rightShoulder;

                EdgeEntryPoints(vec, Edges[EdgeConnectivities[0]].width, out leftEntry, out rightEntry);
                EdgeShoulderPoints(vec, Edges[EdgeConnectivities[0]].width, out leftShoulder, out rightShoulder);

                Geometries.AddRange(new List<Line>{
          new Line(leftEntry, leftShoulder),
          new Line(leftShoulder, rightShoulder),
          new Line(rightShoulder, rightEntry)
          });
            }
            else
            {
                NFEdge EdgeA, EdgeB;
                Vector3d vec1, vec2;
                Point3d leftEntry1, rightEntry1, leftShoulder1, rightShoulder1;
                Point3d leftEntry2, rightEntry2, leftShoulder2, rightShoulder2;
                for (int i = 0; i < EdgeConnectivities.Count; i++)
                {
                    EdgeA = Edges[EdgeConnectivities[i]];
                    if (i + 1 == EdgeConnectivities.Count)
                    {
                        EdgeB = Edges[EdgeConnectivities[0]];
                    }
                    else
                    {
                        EdgeB = Edges[EdgeConnectivities[i + 1]];
                    }

                    vec1 = VectorFromEdge(EdgeA);
                    vec2 = VectorFromEdge(EdgeB);

                    EdgeEntryPoints(vec1, EdgeA.width, out leftEntry1, out rightEntry1);
                    EdgeShoulderPoints(vec1, EdgeA.width, out leftShoulder1, out rightShoulder1);
                    EdgeEntryPoints(vec2, EdgeB.width, out leftEntry2, out rightEntry2);
                    EdgeShoulderPoints(vec2, EdgeB.width, out leftShoulder2, out rightShoulder2);

                    if (Math.Abs(Vector3d.VectorAngle(vec1, vec2) - Math.PI) <= tolerance)
                    {
                        if (EdgeA.width == EdgeB.width)
                        {
                            return new List<Line>{
                new Line(leftEntry1, rightEntry2),
                new Line(rightEntry1, leftEntry2)
                };
                        }
                        else
                        {
                            Geometries.AddRange(new List<Line>{
                new Line(leftEntry1, leftShoulder1),
                new Line(leftShoulder1, rightShoulder2),
                new Line(rightShoulder2, rightEntry2) /*,
                new Line(leftEntry2, leftShoulder2),
                new Line(leftShoulder2, rightShoulder1),
                new Line(rightShoulder1, rightEntry1)
                */
                });
                        }
                    }
                    else
                    {
                        Point3d Intersection = EdgePairIntersection(vec1, vec2, EdgeA.width, EdgeB.width);
                        Geometries.AddRange(new List<Line>{
              new Line(leftEntry1, Intersection),
              new Line(Intersection, rightEntry2)
              });
                    }


                }


            }


            return Geometries;
        }

        public Vector3d VectorFromEdge(NFEdge edge)
        {
            if (edge.FromNodeID == ID)
            {
                return new Vector3d(edge.line.To - point);
            }
            else
            {
                return new Vector3d(edge.line.From - point);
            }
        }

        public void EdgeEntryPoints(Vector3d vec, double width, out Point3d leftE, out Point3d rightE)
        {
            double u, v;
            vec = new Vector3d(vec);
            vec.Unitize();
            u = vec.X;
            v = vec.Y;

            leftE = point + radius * vec + new Vector3d(-v, u, 0) * width / 2;
            rightE = point + radius * vec + new Vector3d(v, -u, 0) * width / 2;
        }

        public void EdgeShoulderPoints(Vector3d vec, double width, out Point3d leftS, out Point3d rightS)
        {
            double u, v;
            vec = new Vector3d(vec);
            vec.Unitize();
            u = vec.X;
            v = vec.Y;

            leftS = point + (new Vector3d(-v, u, 0)) * width / 2;
            rightS = point + (new Vector3d(v, -u, 0)) * width / 2;
        }




        public Point3d EdgePairIntersection(Vector3d vec1, Vector3d vec2, double w1, double w2)
        {
            double u1, u2, v1, v2, lam1, lam2;

            vec1.Unitize();
            vec2.Unitize();
            u1 = vec1.X;
            v1 = vec1.Y;
            u2 = vec2.X;
            v2 = vec2.Y;



            lam1 = (1.0 / 2.0) * 1.0 / (-u1 * v2 + u2 * v1) * (-w1 * v1 * v2 - w2 * v2 * v2 - w1 * u1 * u2 - w2 * u2 * u2);
            lam2 = (1.0 / 2.0) * 1.0 / (-u1 * v2 + u2 * v1) * (-w1 * v1 * v1 - w2 * v1 * v2 - w1 * u1 * u1 - w2 * u1 * u2);

            // point + w1 / 2 * (new Vector3d(-v1, u1, 0)) + lam1 * vec1
            // point + w2 / 2 * (new Vector3d(v2, -u2, 0)) + lam2 * vec2


            return point + w1 / 2 * (new Vector3d(-v1, u1, 0)) + lam1 * vec1;



        }
    }


    class NFGeometryCollector
    {
        public List<Line> visibleSill = new List<Line>();
        public List<Line> visibleDetail = new List<Line>();
        public List<Line> visibleProjection = new List<Line>();

        public List<Line> cutColumn = new List<Line>();
        public List<Line> cutWall = new List<Line>();
        public List<Line> cutWindow = new List<Line>();
        public List<Line> cutDoor = new List<Line>();

        public List<Line> aboveProjection = new List<Line>();
        public List<Line> dashed = new List<Line>();

        public NFGeometryCollector()
        {

        }

        public void Add(SGSemanticsSubElement elem, Line line, bool isEntry, bool isExit)
        {
            double width = elem.drawingSetting.width;
            if (elem.name == "F")
            {
                visibleSill.AddRange(Rectangle(line, width));
                cutWindow.AddRange(Rectangle(line, 0.01));
            }
            else if (elem.name == "A")
            {
                //visibleSill.AddRange(Rectangle(line, width));
                visibleSill.AddRange(Rectangle(line, 0.1));
                cutWindow.AddRange(Rectangle(line, 0.01));

            }
            else if (elem.name == "V")
            {
                aboveProjection.AddRange(Parallel(line, width));

            }
            else if (elem.name == "D")
            {
                cutDoor.AddRange(Door(line));
                aboveProjection.AddRange(Rectangle(line, width));

            }
            else if (elem.name == "W")
            {
                cutWall.AddRange(Rectangle(line, width));
                /*
                cutWall.AddRange(Parallel(line, width));
                if (isEntry)
                {
                    cutWall.AddRange(Entry(line, width));
                }
                else if (isExit)
                {
                    cutWall.AddRange(Exit(line, width));
                } else
                {
                    cutWall.AddRange(Entry(line, width));
                    cutWall.AddRange(Exit(line, width));
                }
                */
            }
            else if (elem.name == "C")
            {
                cutColumn.AddRange(Rectangle(line, line.Length));
            }
        }

        protected List<Line> Rectangle(Line line, double width)
        {

            List<Line> geos = new List<Line>();
            Vector3d vec = new Vector3d(line.To - line.From);
            vec.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
            vec.Unitize();
            vec = vec * (width / 2.0);
            geos.Add(new Line(line.From + vec, line.To + vec));
            geos.Add(new Line(line.From - vec, line.To - vec));
            geos.Add(new Line(line.From - vec, line.From + vec));
            geos.Add(new Line(line.To - vec, line.To + vec));
            return geos;
        }

        protected List<Line> Parallel(Line line, double width)
        {
            List<Line> geos = new List<Line>();
            Vector3d vec = new Vector3d(line.To - line.From);
            vec.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
            vec.Unitize();
            vec = vec * (width / 2.0);
            geos.Add(new Line(line.From + vec, line.To + vec));
            geos.Add(new Line(line.From - vec, line.To - vec));
            return geos;
        }

        protected List<Line> Entry(Line line, double width)
        {
            List<Line> geos = new List<Line>();
            Vector3d vec = new Vector3d(line.To - line.From);
            vec.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
            vec.Unitize();
            vec = vec * (width / 2.0);
            geos.Add(new Line(line.To - vec, line.To + vec));
            return geos;
        }

        protected List<Line> Exit(Line line, double width)
        {
            List<Line> geos = new List<Line>();
            Vector3d vec = new Vector3d(line.To - line.From);
            vec.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
            vec.Unitize();
            vec = vec * (width / 2.0);
            geos.Add(new Line(line.From - vec, line.From + vec));
            return geos;
        }

        protected List<Line> Door(Line line)
        {
            List<Line> geos = new List<Line>();
            Vector3d vec = new Vector3d(line.To - line.From);
            Vector3d vec2 = new Vector3d(vec);
            vec2.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
            vec2.Unitize();
            geos.Add(new Line(line.From, line.From + 0.1 * vec));
            geos.Add(new Line(line.From + 0.1 * vec, line.From + 0.1 * vec + line.Length * vec2));
            geos.Add(new Line(line.From + 0.1 * vec + line.Length * vec2, line.From + line.Length * vec2));
            geos.Add(new Line(line.From + line.Length * vec2, line.From));
            return geos;
        }

        public void Bake()
        {
            SGSemanticsRules rules = new SGSemanticsRules();
;           int visibleSillLayer = rules.visibleSillLayer;
            int visibleDetailLayer = rules.visibleDetailLayer;
            int visibleProjectionLayer = rules.visibleProjectionLayer;
            int cutColumnLayer = rules.cutColumnLayer;
            int cutWallLayer = rules.cutWallLayer;
            int cutWindowLayer = rules.cutWindowLayer;
            int cutDoorLayer = rules.cutDoorLayer;
            int aboveProjectionLayer = rules.aboveProjectionLayer;
            int dashedLayer = rules.dashedLayer;

            var objs = RhinoDoc.ActiveDoc.Objects;
            foreach (Line line in visibleSill)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = visibleSillLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in visibleDetail)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = visibleDetailLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in visibleProjection)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = visibleProjectionLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in cutColumn)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = cutColumnLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in cutWall)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = cutWallLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in cutWindow)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = cutWindowLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in cutDoor)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = cutDoorLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in aboveProjection)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = aboveProjectionLayer;

                objs.AddLine(line, attr);
            }
            foreach (Line line in dashed)
            {
                var attr = new Rhino.DocObjects.ObjectAttributes();
                attr.LayerIndex = dashedLayer;

                objs.AddLine(line, attr);
            }


        }
    }


    class NFEdge
    {
        public Line line;
        public int ID;
        public int FromNodeID;
        public int ToNodeID;
        public double width;
        public int iterations = 0;

        public NFEdge(Line _line, int _ID, int _FromNodeID, int _ToNodeID, double _width)
        {
            line = _line;
            ID = _ID;
            FromNodeID = _FromNodeID;
            ToNodeID = _ToNodeID;
            width = _width;
        }

        public void AddGeometry(SGSemanticsElement elem, NFNetwork network, ref NFGeometryCollector collector, Random random)
        {

            double outDim;
            SGSemanticsElement elemCopy = elem.ShallowCopy();
            elemCopy.isLast = true;
            Vector3d vec = new Vector3d(line.To - line.From);
            vec.Unitize();
            vec = vec * network.NodeRadius;
            Line newLine = new Line(line.From + vec, line.To - vec);


            if (elemCopy.Converge(newLine.Length, random, out outDim))
            {
                List<double> spans = new List<double>();
                List<string> types = new List<string>();
                List<SGSemanticsSubElement> subElems = new List<SGSemanticsSubElement>();
                elemCopy.Summary(ref types, ref spans, ref subElems);
                double sumDim = MathHelper.Sum(spans);
                if (sumDim < newLine.Length)
                {
                    spans.Insert(0, (newLine.Length - sumDim) / 2.0);
                    types.Insert(0, "W");
                    subElems.Insert(0, SGSemanticsRules.W.ShallowCopy());
                    spans.Insert(spans.Count, (newLine.Length - sumDim) / 2.0);
                    types.Insert(types.Count, "W");
                    subElems.Insert(subElems.Count, SGSemanticsRules.W.ShallowCopy());
                }
                List<Line> sliced = Slice(newLine, spans);
                for (int i = 0; i < sliced.Count; i++)
                {
                    collector.Add(subElems[i], sliced[i], i == 0, i == sliced.Count - 1);
                }

            }
            else
            {
                iterations += 1;
                if (iterations > 8)
                {
                    return;
                }
                else if (iterations <= 2)
                {
                    AddGeometry(SGSemanticsRules.ChainedWindow, network, ref collector, random);
                }
                else if (iterations <= 3)
                {
                    if (random.Next() < 5)
                    {
                        AddGeometry(SGSemanticsRules.CurtainWall, network, ref collector, random);
                    } else
                    {
                        AddGeometry(SGSemanticsRules.CurtainWall, network, ref collector, random);
                    }
                }
                else if (iterations <= 6)
                {
                    AddGeometry(SGSemanticsRules.ChainedWindow, network, ref collector, random);
                } else
                {
                    AddGeometry(SGSemanticsRules.W, network, ref collector, random);
                }    

            }




        }

        protected List<Line> Slice(Line line, List<double> spans)
        {
            Vector3d vec = line.To - line.From;
            vec.Unitize();
            List<Line> result = new List<Line>();
            Point3d start = line.From;
            Point3d end = line.From;
            foreach (double span in spans)
            {
                end = end + span * vec;
                result.Add(new Line(start, end));
                start = start + span * vec;
            }
            return result;
        }


    }
}


