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
        /// Create a mesh builder from posture data at a specific frame
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="offset"></param>
        /// <param name="sliderValue"></param>
        /// <param name="pointList"></param>
        /// <param name="meshBuilder"></param>
        public static void CreateMeshBuilder(List<Tuple<double, string, double[]>> dataList, int offset, int sliderValue, out List<Tuple<string, Point3D>> pointList, out MeshBuilder meshBuilder)
        {
            Tuple<double, string, double[]> data;
            Point3D previousPoint;
            pointList = new List<Tuple<string, Point3D>>();
            meshBuilder = new MeshBuilder();
            for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
            {
                data = dataList[(offset + sliderValue - 1) * Constant.BODYPARTS_POSTURE + i];
                pointList.Add(new Tuple<string, Point3D>(data.Item2, new Point3D(data.Item3[0], data.Item3[1], data.Item3[2])));
                meshBuilder.AddSphere(pointList[i].Item2, 0.1);
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
                    meshBuilder.AddCylinder(previousPoint, pointList[i].Item2, 0.05);
                }
            }
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
                Position = new Point3D(P.Item2.X + margin[0], P.Item2.Y + margin[1], P.Item2.Z + margin[2]),
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
                    Position = new Point3D(displayPosition[0], displayPosition[1], displayPosition[2]),
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
            else if (metricsName == "Acceleration")
            {
                isOutOfThreshold = (value_left + value_right) / 2.0 < double.Parse("0." + threshold);

            }
            else if (metricsName == "Pressure")
            {
                isOutOfThreshold = value_left > double.Parse(threshold) || value_right > double.Parse(threshold);
            }
            return new SolidColorBrush(isOutOfThreshold ? Colors.Yellow : Colors.LightGray);
        }
    }
}
