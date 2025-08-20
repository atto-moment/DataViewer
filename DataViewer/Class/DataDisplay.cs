using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DataViewer
{
    public struct DataDisplay
    {
        /// <summary>
        /// Initialize a plot view
        /// </summary>
        /// <param name="helixView"></param>
        public static void InitializePlotView(ref HelixViewport3D helixView)
        {
            helixView.Children.Clear();
            helixView.Children.Add(new ModelVisual3D { Content = new AmbientLight { Color = Colors.White } });
            helixView.Children.Add(new GridLinesVisual3D()
            {
                MajorDistance = 5.0,
                MinorDistance = 0.5,
                Thickness = 0.01,
            });
        }

        /// <summary>
        /// Create a stick figure
        /// </summary>
        /// <param name="helixView"></param>
        /// <param name="dataList"></param>
        /// <param name="offset"></param>
        /// <param name="sliderValue"></param>
        /// <param name="color"></param>
        /// <param name="pointList"></param>
        /// <param name="isSimplified"></param>
        public static void CreateStickFigure(ref HelixViewport3D helixView, List<Tuple<double, string, double[]>> dataList, int offset, int sliderValue, Color color, bool isSimplified = false)
        {
            Tuple<double, string, double[]> data;
            Point3D previousPoint;
            MeshBuilder meshBuilder = new MeshBuilder();
            List<Tuple<string, Point3D>> pointList = new List<Tuple<string, Point3D>>();
            for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
            {
                data = dataList[(offset + sliderValue - 1) * Constant.BODYPARTS_POSTURE + i];
                pointList.Add(new Tuple<string, Point3D>(data.Item2, new Point3D(data.Item3[0], data.Item3[1], data.Item3[2])));
                if (!isSimplified || Constant.JOINTNAMES_SIMPLE.Contains(Constant.JOINTNAMES[i]))
                {
                    if (pointList[i].Item1 != "pelvis")
                    {
                        if (pointList[i - 1].Item1.Contains("end:"))
                        {
                            previousPoint = pointList[Array.IndexOf(Constant.JOINTNAMES, pointList[i - 1].Item1.Split(":")[1])].Item2;
                        }
                        else
                        {
                            previousPoint = pointList[i - 1].Item2;
                        }
                        meshBuilder.AddCylinder(previousPoint, pointList[i].Item2, 0.02);
                    }
                }
            }
            helixView.Children.Add(new ModelVisual3D
            {
                Content = new GeometryModel3D(
                    meshBuilder.ToMesh(),
                    new DiffuseMaterial(new SolidColorBrush(color)))
            });

            DataDisplay.CreateAngleLabels(ref helixView, pointList);
            DataDisplay.CreateJointPoints(ref helixView, pointList);
        }

        /// <summary>
        /// Create a stick figure
        /// </summary>
        /// <param name="helixView"></param>
        /// <param name="dataList"></param>
        /// <param name="offset"></param>
        /// <param name="sliderValue"></param>
        /// <param name="color"></param>
        /// <param name="pointList"></param>
        /// <param name="isSimplified"></param>
        public static void CreateStickFigure(ref HelixViewport3D helixView, List<Tuple<double, string, double[]>> dataList, int offset, int sliderValue, Color color, out List<Tuple<string, Point3D>> pointList, bool isSimplified = false)
        {
            Tuple<double, string, double[]> data;
            Point3D previousPoint;
            MeshBuilder meshBuilder = new MeshBuilder();
            pointList = new List<Tuple<string, Point3D>>();
            for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
            {
                data = dataList[(offset + sliderValue - 1) * Constant.BODYPARTS_POSTURE + i];
                pointList.Add(new Tuple<string, Point3D>(data.Item2, new Point3D(data.Item3[0], data.Item3[1], data.Item3[2])));
                if (!isSimplified || Constant.JOINTNAMES_SIMPLE.Contains(Constant.JOINTNAMES[i]))
                {
                    if (pointList[i].Item1 != "pelvis")
                    {
                        if (pointList[i - 1].Item1.Contains("end:"))
                        {
                            previousPoint = pointList[Array.IndexOf(Constant.JOINTNAMES, pointList[i - 1].Item1.Split(":")[1])].Item2;
                        }
                        else
                        {
                            previousPoint = pointList[i - 1].Item2;
                        }
                        meshBuilder.AddCylinder(previousPoint, pointList[i].Item2, 0.02);
                    }
                }
            }
            helixView.Children.Add(new ModelVisual3D
            {
                Content = new GeometryModel3D(
                    meshBuilder.ToMesh(),
                    new DiffuseMaterial(new SolidColorBrush(color)))
            });
        }

        /// <summary>
        /// Create angle labels
        /// </summary>
        /// <param name="helixView"></param>
        /// <param name="pointList"></param>
        public static void CreateAngleLabels(ref HelixViewport3D helixView, List<Tuple<string, Point3D>> pointList) {
            helixView.Children.Add(CreateAngleLabel(pointList[Array.IndexOf(Constant.JOINTNAMES, "thorax")], pointList[Array.IndexOf(Constant.JOINTNAMES, "pelvis")], [0, 0, -5], Brushes.White));
            helixView.Children.Add(CreateAngleLabel(pointList[Array.IndexOf(Constant.JOINTNAMES, "l_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "l_foot")], [-2.5, 0, 0], Brushes.Red));
            helixView.Children.Add(CreateAngleLabel(pointList[Array.IndexOf(Constant.JOINTNAMES, "r_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "r_foot")], [2.5, 0, 0], Brushes.Blue));
            helixView.Children.Add(CreateAngleDiffLabel(
                pointList[Array.IndexOf(Constant.JOINTNAMES, "l_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "l_foot")],
                pointList[Array.IndexOf(Constant.JOINTNAMES, "r_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "r_foot")],
                [0, 0, -2.5]));
            helixView.Children.Add(CreateAngleDiffLabel(
                pointList[Array.IndexOf(Constant.JOINTNAMES, "l_uarm")], pointList[Array.IndexOf(Constant.JOINTNAMES, "thorax")],
                pointList[Array.IndexOf(Constant.JOINTNAMES, "r_uarm")], pointList[Array.IndexOf(Constant.JOINTNAMES, "thorax")],
                [0, 0, 2.5]));
        }

        /// <summary>
        /// Create an angle label for the specific joints
        /// </summary>
        /// <param name="P"></param>
        /// <param name="Q"></param>
        /// <param name="margin"></param>
        /// <param name="brush"></param>
        /// <returns></returns>
        public static BillboardTextVisual3D CreateAngleLabel(Tuple<string, Point3D> P, Tuple<string, Point3D> Q, double[] margin, Brush brush)
        {
            return new BillboardTextVisual3D
            {
                Position = new Point3D(P.Item2.X + margin[0], 2.5 + margin[1], P.Item2.Z + margin[2]),
                Text = P.Item1 + "\n" + CalculateAngle(P.Item2, Q.Item2).ToString("F1") + " °",
                FontSize = 20,
                Foreground = brush == Brushes.White ? Brushes.Black : Brushes.White,
                Background = brush,
                BorderBrush = Brushes.Black
            };
        }

        /// <summary>
        /// Create an angle difference label for the specific joints
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="Q1"></param>
        /// <param name="P2"></param>
        /// <param name="Q2"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public static BillboardTextVisual3D CreateAngleDiffLabel(Tuple<string, Point3D> P1, Tuple<string, Point3D> Q1, Tuple<string, Point3D> P2, Tuple<string, Point3D> Q2, double[] margin)
        {
            double[] displayPosition = MatrixOperation.Sum(MatrixOperation.Division([(P1.Item2.X + P2.Item2.X), (P1.Item2.Y + P2.Item2.Y), (P1.Item2.Z + P2.Item2.Z)], 2.0), margin);
            double angleDiff = Math.Abs(CalculateAngle(P1.Item2, Q1.Item2) - CalculateAngle(P2.Item2, Q2.Item2));
            int colorValue = (int)Math.Round(255 * Math.Min(1.0, angleDiff / 45.0));

            Brush brush = new SolidColorBrush(Color.FromRgb(255, (byte)(255 - colorValue), (byte)(255 - colorValue)));
            return new BillboardTextVisual3D
                {
                    Position = new Point3D(displayPosition[0], 2.5 + displayPosition[1], displayPosition[2]),
                    Text = P1.Item1.Split("_").Last() + "_diff\n" + angleDiff.ToString("F1") + " °",
                    FontSize = 20,
                    Foreground = Brushes.Black,
                    Background = brush,
                    BorderBrush = Brushes.Black
                };
        }

        /// <summary>
        /// Calculate an angle for the specific joints
        /// </summary>
        /// <param name="P"></param>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static double CalculateAngle(Point3D P, Point3D Q)
        {
            double[] A = [P.X, P.Z];
            double[] B = [Q.X, P.Z];
            double[] C = [Q.X, Q.Z];
            double x = (B[0] - A[0]) * (C[0] - A[0]) + (B[1] - A[1]) * (C[1] - A[1]);
            double y = Math.Sqrt(Math.Pow(B[0] - A[0], 2) + Math.Pow(B[1] - A[1], 2)) * Math.Sqrt(Math.Pow(C[0] - A[0], 2) + Math.Pow(C[1] - A[1], 2));
            return x * 180 / y / Math.PI;
        }

        /// <summary>
        /// Create joint points
        /// </summary>
        /// <param name="helixView"></param>
        /// <param name="pointList"></param>
        /// <param name="euclideanDistanceValues"></param>
        /// <param name="indexes"></param>
        /// <param name="isSimplified"></param>
        public static void CreateJointPoints(ref HelixViewport3D helixView, List<Tuple<string, Point3D>> pointList, double[] euclideanDistanceValues = null, int[] indexes = null, bool isSimplified = false)
        {
            MeshBuilder meshBuilder_Red = new MeshBuilder();
            MeshBuilder meshBuilder_Blue = new MeshBuilder();

            if (indexes != null)
            {
                for (int i = 0; i < indexes.Length; i++)
                {
                    meshBuilder_Red.AddSphere(pointList[indexes[i]].Item2, euclideanDistanceValues[indexes[i]]);
                }
                helixView.Children.Add(new ModelVisual3D
                {
                    Content = new GeometryModel3D(
                        meshBuilder_Red.ToMesh(),
                        new DiffuseMaterial(new SolidColorBrush(Colors.Red)))
                });
            }
            for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
            {
                if ((!isSimplified || Constant.JOINTNAMES_SIMPLE.Contains(Constant.JOINTNAMES[i])) && indexes == null ? true : !indexes.Contains(i))
                {
                    meshBuilder_Blue.AddSphere(pointList[i].Item2, euclideanDistanceValues == null ? 0.05 : euclideanDistanceValues[i]);
                }
            }
            helixView.Children.Add(new ModelVisual3D
            {
                Content = new GeometryModel3D(
                    meshBuilder_Blue.ToMesh(),
                    new DiffuseMaterial(new SolidColorBrush(Colors.Blue)))
            });
        }

        /// <summary>
        /// Change the background color of each progress bar
        /// </summary>
        /// <param name="metricsName"></param>
        /// <param name="value_left"></param>
        /// <param name="value_right"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static SolidColorBrush ChangeBackGroundColor(string metricsName, double value_left, double value_right, string threshold)
        {
            bool isOutOfThreshold = false;
            if (metricsName == "Position")
            {
                isOutOfThreshold = value_left > double.Parse("0." + threshold) || value_right > double.Parse("0." + threshold);
            }
            else if (metricsName == "Angular" || metricsName == "Pressure")
            {
                isOutOfThreshold = value_left > double.Parse(threshold) || value_right > double.Parse(threshold);
            }
            return new SolidColorBrush(isOutOfThreshold ? Colors.Yellow : Colors.LightGray);
        }
    }
}
