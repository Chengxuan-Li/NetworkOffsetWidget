using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace NetworkOffsetWidget
{


    class SGSemanticsSetting
    {
        public double step = 0.05;
        public List<double> vals = new List<double>();
        public int priority;
        public bool access = false;
        public bool visualAccess = false;

        // Settings Zone
        public static int maxIterations = 10;
        public static double tolerance = 0.25;
        public static double maxSurplus = 1.0;
        public static int arrayBufferLimit = 5;
        public static int forceableThreshold = 90;



        public static SGSemanticsSetting Addition(SGSemanticsSetting settingA, SGSemanticsSetting settingB)
        {
            if (settingA.priority < 0)
            {
                return settingB;
            }
            else if (settingB.priority < 0)
            {
                return settingA;
            }
            SGSemanticsSetting newSetting = new SGSemanticsSetting(
              settingA.vals[0] + settingB.vals[0],
              settingA.vals[settingA.vals.Count - 1] + settingB.vals[settingB.vals.Count - 1],
              Math.Min(settingA.step, settingB.step),
              Math.Max(settingA.priority, settingB.priority)
              );
            newSetting.GrantAllAccess(settingA);
            newSetting.GrantAllAccess(settingB);
            return newSetting;
        }

        public static SGSemanticsSetting Multiplication(SGSemanticsSetting setting, double factor)
        {
            List<double> newVals = new List<double>();
            foreach (double v in setting.vals)
            {
                newVals.Add(v * factor);
            }
            SGSemanticsSetting newSetting = new SGSemanticsSetting(newVals, setting.priority);
            newSetting.GrantAllAccess(setting);
            return newSetting;
        }

        public static SGSemanticsSetting Union(SGSemanticsSetting settingA, SGSemanticsSetting settingB)
        {
            if (settingA.priority < 0)
            {
                return settingB;
            }
            else if (settingB.priority < 0)
            {
                return settingA;
            }
            SGSemanticsSetting newSetting = new SGSemanticsSetting(
              Math.Min(settingA.vals[0], settingB.vals[0]),
              Math.Max(settingA.vals[settingA.vals.Count - 1], settingB.vals[settingB.vals.Count - 1]),
              Math.Min(settingA.step, settingB.step),
              Math.Max(settingA.priority, settingB.priority)
              );
            newSetting.GrantAllAccess(settingA);
            newSetting.GrantAllAccess(settingB);
            return newSetting;
        }

        public static SGSemanticsSetting Intersection(SGSemanticsSetting settingA, SGSemanticsSetting settingB)
        {
            if (settingA.priority < 0)
            {
                return settingB;
            }
            else if (settingB.priority < 0)
            {
                return settingA;
            }
            SGSemanticsSetting newSetting = new SGSemanticsSetting(
              Math.Max(settingA.vals[0], settingB.vals[0]),
              Math.Min(settingA.vals[settingA.vals.Count - 1], settingB.vals[settingB.vals.Count - 1]),
              Math.Max(settingA.step, settingB.step),
              Math.Max(settingA.priority, settingB.priority)
              );
            newSetting.GrantAllAccess(settingA);
            newSetting.GrantAllAccess(settingB);
            return newSetting;
        }

        public SGSemanticsSetting()
        {
            vals.Add(0);
            priority = -1;
        }
        public SGSemanticsSetting(double val, int _priority)
        {
            vals.Add(val);
            priority = _priority;
        }

        public SGSemanticsSetting(double minVal, double maxVal, int _priority)
        {
            priority = _priority;
            if (minVal >= maxVal)
            {
                vals.Add((minVal + maxVal) / 2);
            }
            else
            {
                Slice(minVal, maxVal, step);
            }

        }

        public SGSemanticsSetting(List<double> _vals, int _priority)
        {
            vals = _vals;
            priority = _priority;
        }

        public SGSemanticsSetting(double minVal, double maxVal, double _step, int _priority)
        {
            step = _step;
            priority = _priority;
            if (minVal >= maxVal)
            {
                vals.Add((minVal + maxVal) / 2);
            }
            else
            {
                Slice(minVal, maxVal, step);
            }
        }

        public void GrantAccess(bool _access)
        {
            access = _access | access;
        }

        public void GrantVisualAccess(bool _access)
        {
            visualAccess = _access | access;
        }


        public void GrantAllAccess(SGSemanticsSetting setting)
        {
            GrantAccess(setting.access);
            GrantVisualAccess(setting.visualAccess);
        }

        public SGSemanticsSetting Addition(SGSemanticsSetting setting)
        {
            return SGSemanticsSetting.Addition(this, setting);
        }

        public SGSemanticsSetting Multiplication(double factor)
        {
            return SGSemanticsSetting.Multiplication(this, factor);
        }

        public SGSemanticsSetting Union(SGSemanticsSetting setting)
        {
            return SGSemanticsSetting.Union(this, setting);
        }

        public SGSemanticsSetting Intersection(SGSemanticsSetting setting)
        {
            return SGSemanticsSetting.Intersection(this, setting);
        }

        void Slice(double minVal, double maxVal, double step)
        {
            vals = new List<double>();
            double currentVal = minVal;
            while (currentVal < maxVal)
            {
                vals.Add(currentVal);
                currentVal += step;
            }
            vals.Add(maxVal);
        }

        public double GenVal(Random random)
        {
            if (vals.Count == 1)
            {
                return vals[0];
            }
            else
            {
                double t = (double)vals.Count * random.NextDouble();
                return vals[(int)Math.Floor(t)];
            }
        }

        public bool Includes(double testVal)
        {
            if (vals.Count == 1 && Math.Abs(testVal - vals[0]) <= SGSemanticsSetting.tolerance)
            {
                return true;
            }
            else if (vals[0] <= testVal && testVal <= vals[vals.Count - 1])
            {
                foreach (double val in vals)
                {
                    if (Math.Abs(val - testVal) <= SGSemanticsSetting.tolerance)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public double GenVal(double forcedSpan, Random random)
        {
            if (priority < SGSemanticsSetting.forceableThreshold && Includes(forcedSpan))
            {
                return forcedSpan;
            }
            else
            {
                return GenVal(random);
            }
        }

        public double GenCappedVal(double cap, Random random)
        {
            return MathHelper.PickWithCap(vals, cap, random);
        }

    }

    class SGSemanticsRules
    {

        // Parameters

        // X = FromNodeAdjustment
        // Y = ToNodeAdjustment

        // F = Window Fenestration
        // A = Curtain Wall Unit/French Window
        // V = Void
        // D = Door

        // W = Wall (Solid Separator)
        // C = Column (Solid Separator)
        public int visibleSillLayer, visibleDetailLayer, visibleProjectionLayer, cutColumnLayer, cutWallLayer, cutWindowLayer, cutDoorLayer, aboveProjectionLayer, dashedLayer;





        // Sub Element Presets

        //static SGSemanticsSetting F_SSetting = new SGSemanticsSetting(0.2, 2, 0.2, 99);
        static SGSemanticsSetting F_SSetting = new SGSemanticsSetting(0.8, 1.2, 0.2, 99);
        static SGSemanticsSetting A_SSetting = new SGSemanticsSetting(1.2, 3, 0.6, 50);
        static SGSemanticsSetting V_SSetting = new SGSemanticsSetting(0.5, 6, 0.1, 5);
        static SGSemanticsSetting D_SSetting = new SGSemanticsSetting(0.7, 1.2, 0.05, 105);

        static SGSemanticsSetting W_SSetting = new SGSemanticsSetting(0.0, 15, 0.5, 10);
        static SGSemanticsSetting C_SSetting = new SGSemanticsSetting(0.3, 0.5, 0.1, 102);

        static SGSemanticsSetting G_SSetting = new SGSemanticsSetting(1.2, 2.0, 0.1, 106);
        static SGSemanticsSetting M_SSetting = new SGSemanticsSetting(0.05, 0.2, 0.05, 103);

        static SGDrawingSetting F_DSetting = SGDrawingSetting.test;
        static SGDrawingSetting A_DSetting = SGDrawingSetting.test;
        static SGDrawingSetting V_DSetting = SGDrawingSetting.test;
        static SGDrawingSetting D_DSetting = SGDrawingSetting.test;

        static SGDrawingSetting W_DSetting = SGDrawingSetting.test;
        static SGDrawingSetting C_DSetting = SGDrawingSetting.test;
        static SGDrawingSetting G_DSetting = SGDrawingSetting.test;
        static SGDrawingSetting M_DSetting = SGDrawingSetting.test;



        public static SGSemanticsSubElement F = new SGSemanticsSubElement(F_SSetting, F_DSetting, "F");
        public static SGSemanticsSubElement A = new SGSemanticsSubElement(A_SSetting, A_DSetting, "A");
        public static SGSemanticsSubElement V = new SGSemanticsSubElement(V_SSetting, V_DSetting, "V");
        public static SGSemanticsSubElement D = new SGSemanticsSubElement(D_SSetting, D_DSetting, "D");

        public static SGSemanticsSubElement W = new SGSemanticsSubElement(W_SSetting, W_DSetting, "W");
        public static SGSemanticsSubElement C = new SGSemanticsSubElement(C_SSetting, C_DSetting, "C");

        public static SGSemanticsSubElement G = new SGSemanticsSubElement(G_SSetting, G_DSetting, "G");
        public static SGSemanticsSubElement M = new SGSemanticsSubElement(M_SSetting, M_DSetting, "M");




        // Multi Element (Sub- Level) Presets

        // Chained Window
        static SGSemanticsMultiElement ChainedWindowStr = new SGSemanticsMultiElement(new List<SGSemanticsElement> { W, F });
        static SGSemanticsArrayElement ChainedWindowArr = ChainedWindowStr.ToArrayElement();
        public static SGSemanticsMultiElement ChainedWindow = new SGSemanticsMultiElement(new List<SGSemanticsElement> { ChainedWindowArr, W });

        // Curtain Wall
        static SGSemanticsMultiElement CurtainWallStr = new SGSemanticsMultiElement(new List<SGSemanticsElement> { M, A });
        static SGSemanticsArrayElement CurtainWallArr = CurtainWallStr.ToArrayElement();
        public static SGSemanticsMultiElement CurtainWall = new SGSemanticsMultiElement(new List<SGSemanticsElement> { CurtainWallArr, M });

        // Wall Window
        public static SGSemanticsMultiElement WallWindow = new SGSemanticsMultiElement(new List<SGSemanticsElement> { W, F, W });

        // External Access
        static SGSemanticsVarElement ExternalAccessStrVar = new SGSemanticsVarElement(
          new List<SGSemanticsElement> { W, WallWindow, CurtainWall },
          new List<double> { 4.0, 1.0, 2.0 }
          );
        public static SGSemanticsMultiElement ExternalAccess = new SGSemanticsMultiElement(new List<SGSemanticsElement> { ExternalAccessStrVar, D, ExternalAccessStrVar });



        // Generic Variation Element Presets

        public static List<SGSemanticsElement> GenericVarElems = new List<SGSemanticsElement>
    {
      F, A, V, D, W, C, WallWindow, ChainedWindow, CurtainWall, ExternalAccess
      };




        // Retail-Front Structure Variation Element
        public static SGSemanticsVarElement RetailFrontStrVar = new SGSemanticsVarElement(GenericVarElems, new List<double>
    {
      0.0, // F
      4.0, // A
      0.0, // V
      0.0, // D
      1.0, // W
      0.0, // C
      0.0, // Wall Window
      0.0, // Chained window
      3.0, // Curtain Wall
      0.0  // External Access
      });

        // Interior-Sep Structure Variation Element
        public static SGSemanticsVarElement InteriorSepStrVar = new SGSemanticsVarElement(GenericVarElems, new List<double>
    {
      0.0, // F
      1.0, // A
      0.0, // V
      0.0, // D
      3.0, // W
      0.0, // C
      0.0, // Wall Window
      0.0, // Chained window
      0.0, // Curtain Wall
      0.0  // External Access
      });

        public static SGSemanticsVarElement InteriorSepAccsVar = new SGSemanticsVarElement(GenericVarElems, new List<double>
    {
      0.0, // F
      0.0, // A
      1.0, // V
      5.0, // D
      0.0, // W
      0.0, // C
      0.0, // Wall Window
      0.0, // Chained window
      0.0, // Curtain Wall
      0.0  // External Access
      });


        // Multi Element Assemblage with VARs


        // RetailFront
        public static SGSemanticsMultiElement RetailFront = new SGSemanticsMultiElement(new List<SGSemanticsElement> { RetailFrontStrVar, D, CurtainWall });

        // InteriorSep
        public static SGSemanticsMultiElement InteriorSep = new SGSemanticsMultiElement(new List<SGSemanticsElement> { InteriorSepStrVar, InteriorSepAccsVar, InteriorSepStrVar });


        public static List<SGSemanticsElement> ExtendedVarElems = new List<SGSemanticsElement>
    {
      F, A, V, D, W, C, WallWindow, ChainedWindow, CurtainWall, ExternalAccess, RetailFront, InteriorSep
      };


        // Poly Column-Wall (External) Structure Variation Element
        public static SGSemanticsVarElement PlyCWExStrVar = new SGSemanticsVarElement(ExtendedVarElems, new List<double>
    {
      0.5, // F
      2.0, // A
      0.0, // V
      0.0, // D
      5.0, // W
      0.0, // C
      4.0, // Wall Window
      4.0, // Chained window
      3.0, // Curtain Wall
      2.0, // External Access
      1.0, // Retail Front
      0.0  // Interior Sep
      });

        // Poly Column-Wall (Internal) Structure Variation Element
        public static SGSemanticsVarElement PlyCWInStrVar = new SGSemanticsVarElement(ExtendedVarElems, new List<double>
    {
      0.5, // F
      2.0, // A
      0.0, // V
      0.0, // D
      6.0, // W
      0.0, // C
      0.2, // Wall Window
      0.4, // Chained window
      3.0, // Curtain Wall
      2.0, // External Access
      0.0, // Retail Front
      9.0  // Interior Sep
      });


        // PlyCWExStr
        static SGSemanticsMultiElement PlyCWExStr = new SGSemanticsMultiElement(new List<SGSemanticsElement> { C, PlyCWExStrVar });
        static SGSemanticsArrayElement PlyCWExArr = PlyCWExStr.ToArrayElement();
        public static SGSemanticsMultiElement PlyCWEx = new SGSemanticsMultiElement(new List<SGSemanticsElement> { PlyCWExArr, C });

        // PlyCWInStr
        static SGSemanticsMultiElement PlyCWInStr = new SGSemanticsMultiElement(new List<SGSemanticsElement> { C, PlyCWInStrVar });
        static SGSemanticsArrayElement PlyCWInArr = PlyCWInStr.ToArrayElement();
        public static SGSemanticsMultiElement PlyCWIn = new SGSemanticsMultiElement(new List<SGSemanticsElement> { PlyCWExStr, C });


        // TODO more types to be added


        public static SGSemanticsMultiElement T101 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                ChainedWindow, D, W, F, W
            });
        public static SGSemanticsMultiElement T102 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                W, G, W
            });
        public static SGSemanticsMultiElement T103 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                CurtainWall, G, C, A, C
            });
        public static SGSemanticsMultiElement T201 = ChainedWindow.ShallowCopy();
        public static SGSemanticsMultiElement T202 = CurtainWall.ShallowCopy();
        public static SGSemanticsMultiElement T203 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                new SGSemanticsMultiElement(new List<SGSemanticsElement> { C, W }).ToArrayElement(), C
            });
        public static SGSemanticsMultiElement T301 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                new SGSemanticsVarElement(new List<SGSemanticsElement> { T203, W })
            });
        public static SGSemanticsMultiElement T302 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                new SGSemanticsMultiElement(
                    new List<SGSemanticsElement>
                    {
                        new SGSemanticsVarElement(new List<SGSemanticsElement> { ChainedWindow, CurtainWall, W })
                    }).ToArrayElement()
            });
        public static SGSemanticsMultiElement T311 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                new SGSemanticsVarElement(new List<SGSemanticsElement> { T203, W })
            });
        public static SGSemanticsMultiElement T312 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                new SGSemanticsMultiElement(
                    new List<SGSemanticsElement>
                    {
                        new SGSemanticsVarElement(new List<SGSemanticsElement> { ChainedWindow, CurtainWall, W })
                    }).ToArrayElement()
            });
        public static SGSemanticsMultiElement T401 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                new SGSemanticsMultiElement(
                    new List<SGSemanticsElement>
                    {
                        C, V
                    }).ToArrayElement(),
                C
            });
        public static SGSemanticsMultiElement T402 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                C, V, C
            });
        public static SGSemanticsMultiElement T403 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                M, V, M
            });
        public static SGSemanticsMultiElement T501 = new SGSemanticsMultiElement(
            new List<SGSemanticsElement>
            {
                V
            });




        public SGSemanticsRules()
        {
            RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            visibleSillLayer = AddLayer("visibleSill", Color.Orange, doc);
            visibleDetailLayer = AddLayer("visibleDetail", Color.Green, doc);
            visibleProjectionLayer = AddLayer("visibleProjection", Color.Green, doc);
            cutColumnLayer = AddLayer("cutColumn", Color.Blue, doc);
            cutWallLayer = AddLayer("cutWall", Color.Red, doc);
            cutWindowLayer = AddLayer("cutWindow", Color.Cyan, doc);
            cutDoorLayer = AddLayer("cutDoor", Color.White, doc);
            aboveProjectionLayer = AddLayer("aboveProjection", Color.Purple, doc);
            dashedLayer = AddLayer("dashed", Color.White, doc);

        }


        protected int AddLayer(string name, Color color, RhinoDoc doc)
        {
            Rhino.DocObjects.Layer lyr = doc.Layers.FindName(name);
            return (lyr is null) ? doc.Layers.Add(name, color): lyr.Index;
        }

    }

    class MathHelper
    {
        public static Random random = new Random();

        public static bool IsLast(List<double> weights)
        {
            int num = 0;
            foreach (double w in weights)
            {
                if (w > 0.01)
                {
                    num += 1;
                }
            }
            if (num <= 1)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public static int RandomPick(List<double> weights, Random random)
        {
            weights = new List<double>(weights);
            double sum = 0.0;

            for (int i = 0; i < weights.Count; i++)
            {
                sum += weights[i];
                weights[i] = sum;
            }

            double rand = random.NextDouble() * sum;

            for (int i = 0; i < weights.Count; i++)
            {
                if (rand <= weights[i])
                {
                    return i;
                }
            }
            return 0;

        }

        public static double Sum(List<double> weights)
        {
            double sum = 0.0;

            for (int i = 0; i < weights.Count; i++)
            {
                sum += weights[i];
            }
            return sum;
        }

        public static List<int> GetOrder(List<double> priorities)
        {
            List<int> indices = new List<int>(priorities.Count);
            List<double> sorted = new List<double>(priorities.Count);

            sorted.Add(priorities[0]);
            indices.Add(0);
            for (int i = 1; i < priorities.Count; i++)
            {
                int j = 0;
                while (j <= sorted.Count)
                {
                    if (j == sorted.Count)
                    {
                        sorted.Add(priorities[i]);
                        indices.Add(i);
                        break;
                    }
                    if (sorted[j] <= priorities[i])
                    {
                        sorted.Insert(j, priorities[i]);
                        indices.Insert(j, i);
                        break;
                    }
                    else
                    {
                        j++;
                    }
                }
            }
            return indices;

        }

        public static List<int> GetOrder(List<int> priorities)
        {
            List<double> doubleList = priorities.ConvertAll(x => (double)x);
            return GetOrder(doubleList);

        }

        public static double PickWithCap(List<double> vals, double cap, Random random)
        {
            List<double> newVals = new List<double>(vals);
            for (int i = 0; i < newVals.Count; i++)
            {
                if (newVals[i] > cap)
                {
                    newVals.RemoveAt(i);
                }
            }
            return newVals[(int)Math.Floor(random.NextDouble() * newVals.Count)];
        }



    }

    abstract class SGSemanticsElement
    {
        public SGSemanticsSetting semanticsSetting = new SGSemanticsSetting();
        public List<SGSemanticsSetting> semanticsSettings = new List<SGSemanticsSetting>();

        public bool isLast = false;
        public double dimension;

        public bool hasSubElems;
        public int timesRepeated = 1;
        public abstract bool Converge(double target, Random random, out double _dimension);

        public abstract void GetGeometry();
        // TODO

        public abstract string Describe();

        public abstract string Detail();

        // each sub- class will override this method to allow for different means of summarisation: by addition, multiplication, union, intersection, etc.
        protected abstract SGSemanticsSetting MemberwiseSummarise(SGSemanticsSetting summary, SGSemanticsSetting nextSetting);

        public abstract void Summary(ref List<string> types, ref List<double> spans, ref List<SGSemanticsSubElement> subElems);

        public void SummariseSettings()
        {
            semanticsSetting = new SGSemanticsSetting();
            if (semanticsSettings.Count == 1)
            {
                semanticsSetting = semanticsSettings[0];
            }
            else
            {
                semanticsSetting = new SGSemanticsSetting();
                foreach (SGSemanticsSetting setting in semanticsSettings)
                {
                    semanticsSetting = MemberwiseSummarise(semanticsSetting, setting);
                }
            }
        }


        public SGSemanticsElement ShallowCopy()
        {
            return (SGSemanticsElement)this.MemberwiseClone();
        }




    }

    class SGSemanticsSubElement : SGSemanticsElement
    {
        public SGDrawingSetting drawingSetting;
        public string name;
        // only subElement class has drawing settings, because this is the only class where something is actually DRAWN

        public SGSemanticsSubElement(SGSemanticsSetting _semanticsSetting, SGDrawingSetting _drawingSetting, string _name)
        {
            semanticsSettings = new List<SGSemanticsSetting> { _semanticsSetting };
            drawingSetting = _drawingSetting;
            hasSubElems = false;
            name = _name;
            SummariseSettings();
        }


        protected override SGSemanticsSetting MemberwiseSummarise(SGSemanticsSetting summary, SGSemanticsSetting nextSetting)
        {
            // this does not really matter and should not be called
            return summary.Addition(nextSetting);
        }

        public override string Describe()
        {

            return name + " ";
        }

        public override bool Converge(double target, Random random, out double _dimension)
        {
            if (target < 0)
            {
                dimension = semanticsSetting.GenVal(random);
                _dimension = dimension;
                return true;
            }
            else
            {
                int iteration = 0;
                while (iteration < SGSemanticsSetting.maxIterations)
                {
                    double testResult;
                    if (isLast)
                    {
                        testResult = semanticsSetting.GenVal(target, random);
                    }
                    else
                    {
                        testResult = semanticsSetting.GenCappedVal(target, random);
                    }
                    double surplus = target - testResult;
                    if (surplus >= 0 && (!isLast))
                    {
                        dimension = testResult;
                        _dimension = dimension;

                        return true;
                    }
                    if (surplus >= 0 && surplus <= SGSemanticsSetting.tolerance)
                    {
                        dimension = testResult;
                        _dimension = dimension;

                        return true;
                    }

                    iteration += 1;
                }
                dimension = -1.0;
                _dimension = dimension;
                return false;
            }
        }

        public override void GetGeometry()
        {
            // TODO
        }

        public SGSemanticsSubElement ShallowCopy()
        {
            return (SGSemanticsSubElement)this.MemberwiseClone();
        }

        public override string Detail()
        {
            return "[" + name + dimension.ToString() + "]";
        }

        public override void Summary(ref List<string> types, ref List<double> spans, ref List<SGSemanticsSubElement> subElems)
        {
            types.Add(name);
            spans.Add(dimension);
            subElems.Add(ShallowCopy());
        }
    }

    class SGSemanticsVarElement : SGSemanticsElement
    {
        public List<SGSemanticsElement> pickableElems = new List<SGSemanticsElement>();
        public List<double> weights = new List<double>();
        public SGSemanticsElement picked;

        public SGSemanticsVarElement(List<SGSemanticsElement> _pickableElems, List<double> _weights)
        {

            pickableElems = new List<SGSemanticsElement>(_pickableElems);
            weights = new List<double>(_weights);
            hasSubElems = true;

            foreach (SGSemanticsElement elem in pickableElems)
            {
                semanticsSettings.Add(elem.semanticsSetting);
            }
            SummariseSettings();
        }

        public SGSemanticsVarElement(List<SGSemanticsElement> _pickableElems)
        {
            pickableElems = new List<SGSemanticsElement>(_pickableElems.ConvertAll(x => x.ShallowCopy()));
            weights = new List<double>();
            foreach (SGSemanticsElement elem in pickableElems)
            {
                weights.Add(1.0);
            }
        }

        public SGSemanticsVarElement(SGSemanticsElement _pickableElem)
        {
            pickableElems = new List<SGSemanticsElement> { _pickableElem.ShallowCopy() };
            weights = new List<double>();
            foreach (SGSemanticsElement elem in pickableElems)
            {
                weights.Add(1.0);
            }
        }


        public void ForcePick(SGSemanticsElement elem)
        {
            picked = elem;
        }

        public void ForcePick(int index)
        {
            picked = pickableElems[index];
        }

        public int RandomPick(Random random)
        {
            int index = MathHelper.RandomPick(weights, random);
            ForcePick(index);
            return index;
        }

        protected override SGSemanticsSetting MemberwiseSummarise(SGSemanticsSetting summary, SGSemanticsSetting nextSetting)
        {
            return summary.Union(nextSetting);
        }

        public override string Describe()
        {

            string result = "Var{";
            for (int i = 0; i < pickableElems.Count; i++)
            {
                if (weights[i] > 0.0)
                {
                    result += pickableElems[i].Describe();
                }
            }
            return result + "}";

        }



        public override bool Converge(double target, Random random, out double _dimension)
        {
            int index = RandomPick(random);
            List<double> tempWeights = new List<double>(weights);
            if (target < 0)
            {
                dimension = picked.semanticsSetting.GenVal(random);
                _dimension = dimension;
                return true;
            }
            else
            {
                bool converging = true;
                while (converging)
                {
                    if (MathHelper.Sum(tempWeights) <= 0.01)
                    {
                        converging = false;
                        dimension = -1.0;
                        _dimension = dimension;
                        return false;
                    }
                    else
                    {
                        int iteration = 0;
                        while (iteration < SGSemanticsSetting.maxIterations)
                        {
                            double testResult;
                            double surplus;
                            if (MathHelper.IsLast(tempWeights))
                            {
                                isLast = true;
                            }
                            if (isLast)
                            {
                                picked.isLast = isLast;
                                bool feasible = picked.Converge(target, random, out testResult);
                                if (feasible)
                                {
                                    surplus = target - testResult;
                                    if (surplus >= 0 && surplus <= SGSemanticsSetting.tolerance)
                                    {
                                        dimension = testResult;
                                        _dimension = dimension;
                                        picked.dimension = dimension;

                                        return true;
                                    }
                                }
                            }
                            else
                            {
                                bool feasible = picked.Converge(target, random, out testResult);
                                if (feasible)
                                {
                                    surplus = target - testResult;
                                    if (surplus >= 0)
                                    {
                                        dimension = testResult;
                                        _dimension = dimension;
                                        picked.dimension = dimension;
                                        return true;
                                    }
                                }

                            }
                            iteration += 1;
                        }

                        tempWeights[index] = 0.0;
                        index = RandomPick(random);

                    }

                }

            }
            dimension = -1.0;
            _dimension = dimension;
            return false;

        }

        public override string Detail()
        {
            return "{" + picked.Detail() + "}";
        }

        public override void GetGeometry()
        {
            // TODO
        }

        public SGSemanticsVarElement ShallowCopy()
        {
            SGSemanticsVarElement other = (SGSemanticsVarElement)this.MemberwiseClone();
            other.pickableElems = new List<SGSemanticsElement>(other.pickableElems.ConvertAll(x => x.ShallowCopy()));
            return other;
        }

        public override void Summary(ref List<string> types, ref List<double> spans, ref List<SGSemanticsSubElement> subElems)
        {
            picked.Summary(ref types, ref spans, ref subElems);
        }
    }


    class SGSemanticsMultiElement : SGSemanticsElement
    {
        public List<SGSemanticsElement> elems = new List<SGSemanticsElement>();


        public SGSemanticsMultiElement(List<SGSemanticsElement> _elems)
        {

            elems = new List<SGSemanticsElement>(_elems.ConvertAll(x => x.ShallowCopy()));
            hasSubElems = true;

            foreach (SGSemanticsElement elem in elems)
            {
                semanticsSettings.Add(elem.semanticsSetting);
            }
            SummariseSettings();
        }

        public SGSemanticsMultiElement(SGSemanticsMultiElement multiElem)
        {

            elems = new List<SGSemanticsElement>(multiElem.elems.ConvertAll(x => x.ShallowCopy()));
            hasSubElems = true;

            foreach (SGSemanticsElement elem in elems)
            {
                semanticsSettings.Add(elem.semanticsSetting);
            }
            SummariseSettings();
        }

        protected override SGSemanticsSetting MemberwiseSummarise(SGSemanticsSetting summary, SGSemanticsSetting nextSetting)
        {
            return summary.Addition(nextSetting);
        }

        public override string Describe()
        {
            string result = "{";
            foreach (SGSemanticsElement elem in elems)
            {
                result += elem.Describe();
            }
            return result + "}";
        }

        public SGSemanticsArrayElement ToArrayElement()
        {
            SGSemanticsArrayElement arrElem = new SGSemanticsArrayElement(this);
            return arrElem;
        }



        public override bool Converge(double target, Random random, out double _dimension)
        {
            bool converging = true;
            double currentDim;
            bool feasible = true;
            double remaining;
            int iteration = 0;
            List<int> priorities = new List<int>();
            List<double> targets = new List<double>();
            remaining = target;

            foreach (SGSemanticsElement elem in elems)
            {
                priorities.Add(elem.semanticsSetting.priority);
            }
            List<int> order = MathHelper.GetOrder(priorities);

            if (isLast)
            {
                elems[order[order.Count - 1]].isLast = true;
            }

            while (converging)
            {
                feasible = true;
                remaining = target;
                if (iteration > SGSemanticsSetting.maxIterations)
                {
                    dimension = -1.0;
                    _dimension = dimension;
                    return false;
                }
                else
                {

                    for (int i = 0; i < order.Count; i++)
                    {
                        // iterations here per i?



                        if (elems[order[i]].Converge(remaining, random, out currentDim))
                        {
                            remaining = remaining - currentDim;
                            if (remaining < 0)
                            {
                                feasible = false;
                                break;
                            }
                        }
                        else
                        {
                            feasible = false;
                            break;
                        }
                    }
                    if (feasible)
                    {
                        if (isLast && (remaining >= 0 && remaining <= SGSemanticsSetting.tolerance))
                        {
                            dimension = target - remaining;
                            _dimension = dimension;
                            return true;
                        }
                        else if (!isLast && remaining >= 0)
                        {
                            dimension = target - remaining;
                            _dimension = dimension;
                            return true;
                        }
                    }
                    iteration += 1;
                }

            }

            dimension = -1.0;
            _dimension = dimension;
            return false;



        }


        public SGSemanticsMultiElement ShallowCopy()
        {
            SGSemanticsMultiElement other = (SGSemanticsMultiElement)this.MemberwiseClone();
            other.elems = new List<SGSemanticsElement>(other.elems.ConvertAll(x => x.ShallowCopy()));
            return other;
        }

        public override void GetGeometry()
        {
            // TODO
        }

        public override string Detail()
        {
            string result = "{";
            foreach (SGSemanticsElement elem in elems)
            {
                result += elem.Detail();
            }
            return result + "}";
        }

        public override void Summary(ref List<string> types, ref List<double> spans, ref List<SGSemanticsSubElement> subElems)
        {
            foreach (SGSemanticsElement elem in elems)
            {
                elem.Summary(ref types, ref spans, ref subElems);
            }
        }

    }

    class SGSemanticsRepeatedElement : SGSemanticsMultiElement
    {
        public SGSemanticsMultiElement repeatedElem;

        public SGSemanticsRepeatedElement(SGSemanticsMultiElement multiElem, int _timesRepeated) : base(multiElem)
        {
            timesRepeated = _timesRepeated;
            elems = new List<SGSemanticsElement>();
            for (int i = 0; i < timesRepeated; i++)
            {
                elems.Add(multiElem.ShallowCopy());
            }
            semanticsSetting = multiElem.semanticsSetting.Multiplication(timesRepeated);
            repeatedElem = multiElem;
        }

        public override string Describe()
        {
            string result = "";
            result += (timesRepeated + 0).ToString();
            result += repeatedElem.Describe();
            return result + "";
        }

        public override void GetGeometry()
        {
            base.GetGeometry();
            // TODO
        }

        public override string Detail()
        {
            string result = " " + timesRepeated.ToString() + "x{";
            foreach (SGSemanticsElement elem in elems)
            {
                result += elem.Detail();
            }
            return result + "}";
        }

        public override void Summary(ref List<string> types, ref List<double> spans, ref List<SGSemanticsSubElement> subElems)
        {
            foreach (SGSemanticsElement elem in elems)
            {
                elem.Summary(ref types, ref spans, ref subElems);
            }
        }


    }

    class SGSemanticsArrayElement : SGSemanticsVarElement
    {
        //public new List<SGSemanticsRepeatedElement> pickableElems = new List<SGSemanticsRepeatedElement>();
        //dont understand why im doing this
        // if you dont understand why pls dont do it :)
        SGSemanticsMultiElement unit;

        public SGSemanticsArrayElement(SGSemanticsMultiElement _pickableElem) : base(_pickableElem)
        {
            isLast = _pickableElem.isLast;
            unit = _pickableElem;
            pickableElems = new List<SGSemanticsElement>();
            weights = new List<double>();

            hasSubElems = true;


            //List<SGSemanticsElement> arrayUnit = new List<SGSemanticsElement>();

            for (int i = 1; i < SGSemanticsSetting.arrayBufferLimit; i++)
            {
                //arrayUnit.Add(new SGSemanticsMultiElement(new List<SGSemanticsElement>(_elems)));
                SGSemanticsRepeatedElement arrayUnit = new SGSemanticsRepeatedElement(unit.ShallowCopy(), i);
                arrayUnit.isLast = isLast;
                pickableElems.Add(arrayUnit);
                weights.Add(1.0);
            }


            foreach (SGSemanticsElement elem in pickableElems)
            {
                semanticsSettings.Add(elem.semanticsSetting);
            }
            SummariseSettings();
            // TODO some sort of multiplication handler or controller should be implemented
        }


        public override string Describe()
        {
            string result = "ArrVar{";
            foreach (SGSemanticsElement elem in pickableElems)
            {
                result += elem.Describe();
            }
            return result + "}";
        }




        public override void GetGeometry()
        {
            base.GetGeometry();
        }

        public SGSemanticsArrayElement ShallowCopy()
        {
            return (SGSemanticsArrayElement)this.MemberwiseClone();
        }
    }

    class SGDrawingSetting
    {
        public double width; // setting
        public double length; // variable taken as input
        public int LayerID;

        public static SGDrawingSetting test = new SGDrawingSetting(0.3);

       
        

        /*
        ----- vis
        ----- cut

        ----- central/dash/anno ]
                                ]
        ----- cut               ] caps?
                                ]
        ----- detail            ]
        ----- vis
        */

        public SGDrawingSetting(double _width)
        {
            width = _width;








        }


        public void SGGetGeometry(double length)
        {
            // TODO
        }
    }


}
