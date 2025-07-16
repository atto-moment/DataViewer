using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DataViewer
{
    public struct FileOperation
    {
        /// <summary>
        /// Read all frames from the BVH or TXT file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static object ReadAllFrames(string filePath, string extension)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    if (extension == ".bvh")
                    {
                        return ReadBVHFile(reader);
                    }
                    else if (extension == ".txt")
                    {
                        List<double[]> list = new List<double[]>();
                        reader.ReadLine();
                        reader.ReadLine();
                        reader.ReadLine();
                        while (!reader.EndOfStream)
                        {
                            list.Add(Array.ConvertAll(reader.ReadLine().Split("\t"), s => double.TryParse(s, out double x) ? x : 0));
                        }
                        return list;
                    }
                    else
                    {
                        string[] header = reader.ReadLine().Split(",");
                        if (header.Length == Constant.DIMENTIONS_POSTURE)
                        {
                            List <Tuple<double, string, double[]>> list = new List<Tuple<double, string, double[]>>();
                            int index = 0;
                            double [] values;
                            while (!reader.EndOfStream)
                            {
                                values = Array.ConvertAll(reader.ReadLine().Split(","), s => double.TryParse(s, out double x) ? x : 0);
                                list.Add(new Tuple<double, string, double[]>(values[0], Constant.JOINTNAMES[index], [values[2], values[3], values[4]]));
                                index = (index + 1) % Constant.BODYPARTS_POSTURE;
                            }
                            return list;
                        }
                        else if (header.Length == Constant.DIMENTIONS_FOOTPRESSURE)
                        {
                            List<double[]> list = new List<double[]>();
                            while (!reader.EndOfStream)
                            {
                                list.Add(Array.ConvertAll(reader.ReadLine().Split(","), s => double.TryParse(s, out double x) ? x : 0));
                            }
                            return list;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get the mean values of all frames from the CSV file 
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public static void GetMeanValues(string[] files, out List<Tuple<double, string, double[]>> postureDataList, out List<double[]> footPressureDataList, int offset = 0, int count = 1)
        {
            double length;
            postureDataList = new List<Tuple<double, string, double[]>>();
            footPressureDataList = new List<double[]>();
            Array.Sort(files);

            foreach (string file in files.Where(path => path.EndsWith("Posture.csv")))
            {
                List<Tuple<double, string, double[]>> list = (List<Tuple<double, string, double[]>>)FileOperation.ReadAllFrames(file, ".csv");
                double[][] values = new double[Constant.BODYPARTS_POSTURE][];
                for (int i = offset; i < list.Count() / Constant.BODYPARTS_POSTURE; i += count)
                {
                    int index;
                    double originX = 0;
                    for (int j = 0; j < Constant.BODYPARTS_POSTURE; j++)
                    {
                        index = i * Constant.BODYPARTS_POSTURE + j;
                        if (list[index].Item2 == "pelvis" && count != 1)
                        {
                            originX = list[index].Item3[0] + list[index].Item3[0] > 0 ? -0.01 : 0.01;
                        }
                        if (values[index % Constant.BODYPARTS_POSTURE] == null)
                        {
                            values[index % Constant.BODYPARTS_POSTURE] = [list[index].Item1, list[index].Item3[0] - originX, list[index].Item3[1], list[index].Item3[2]];
                        }
                        else
                        {
                            values[index % Constant.BODYPARTS_POSTURE] = MatrixOperation.Sum(values[index % Constant.BODYPARTS_POSTURE], [list[index].Item1, list[index].Item3[0] - originX, list[index].Item3[1], list[index].Item3[2]]);
                        }
                    }
                }
                length = Math.Round(Math.Round((list.Count - offset) / (double)count) / Constant.BODYPARTS_POSTURE);
                for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
                {
                    values[i] = MatrixOperation.Division(values[i], length);
                    postureDataList.Add(new Tuple<double, string, double[]>(values[i][0], Constant.JOINTNAMES[i], [values[i][1], values[i][2], values[i][3]]));
                }
            }
            foreach (string file in files.Where(path => path.EndsWith("FootPressure.csv")))
            {
                List<double[]> list = (List<double[]>)FileOperation.ReadAllFrames(file, ".csv");
                double[] values = new double[Constant.DIMENTIONS_FOOTPRESSURE];
                Array.Fill(values, 0);
                for (int i = offset; i < list.Count(); i += count)
                {
                    values = MatrixOperation.Sum(values, list[i]);
                }
                length = Math.Round((list.Count - offset) / (double)count);
                values = MatrixOperation.Division(values, length);
                footPressureDataList.Add(values);
            }
        }

        /// <summary>
        /// Read a BVH file
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<Tuple<double, string, double[]>> ReadBVHFile(StreamReader reader)
        {
            bool isOffsetData = true;
            double frameTime = 0;
            double timestamp = 0;
            double[] motionData;
            double[] offset = new double[3];
            double[] position = new double[3];
            double[][] displacement;
            double[][] motion = new double[2][];
            double[][] rotation = new double[3][];
            int endCount = 0;
            List<double[]> offsetList = new List<double[]>();
            List<double[]> positionList = new List<double[]>();
            List<double[][]> rotationList = new List<double[][]>();
            List<Tuple<double, string, double[]>> list = new List<Tuple<double, string, double[]>>();
            string line;
            string nextParentJointName = "";

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (isOffsetData)
                {
                    if (line.Contains("OFFSET"))
                    {
                        offset = Array.ConvertAll(line.Split(" "), s => double.TryParse(s, out double x) ? x : 0);
                        offsetList.Add([offset[1], offset[2], offset[3]]);
                    }
                    else if (line.Contains("MOTION"))
                    {
                        isOffsetData = false;
                    }
                }
                else
                {
                    if (line.Contains("Frames"))
                    {
                    }
                    else if (line.Contains("Frame Time"))
                    {
                        frameTime = double.Parse(line.Split(':')[1]);
                    }
                    else
                    {
                        motionData = Array.ConvertAll(line.Split(" "), s => double.TryParse(s, out double x) ? x : 0);
                        endCount = 0;
                        for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
                        {
                            if (i != Constant.BODYPARTS_POSTURE - 1)
                            {
                                motion[0] = [motionData[(i - endCount) * 6], motionData[(i - endCount) * 6 + 1], motionData[(i - endCount) * 6 + 2]];
                                motion[1] = [motionData[(i - endCount) * 6 + 3], motionData[(i - endCount) * 6 + 4], motionData[(i - endCount) * 6 + 5]];
                                position = MatrixOperation.Sum(offsetList[i], motion[0]);
                            }

                            if (nextParentJointName != "")
                            {
                                displacement = MatrixOperation.Transpose(MatrixOperation.Product(rotationList[Array.IndexOf(Constant.JOINTNAMES, nextParentJointName)], MatrixOperation.Transpose([position])));
                                position = MatrixOperation.Sum(displacement, [positionList[Array.IndexOf(Constant.JOINTNAMES, nextParentJointName)]])[0];
                                rotation = MatrixOperation.Product(rotationList[Array.IndexOf(Constant.JOINTNAMES, nextParentJointName)], MatrixOperation.Rotate(motion[1]));
                                nextParentJointName = "";
                            }
                            else
                            {
                                if (Constant.JOINTNAMES[i].Contains("end"))
                                {
                                    displacement = MatrixOperation.Transpose(MatrixOperation.Product(rotationList[i - 1], MatrixOperation.Transpose([offsetList[i]])));
                                    position = MatrixOperation.Sum(displacement, [positionList[i - 1]])[0];
                                    nextParentJointName = Constant.JOINTNAMES[i].Split(":")[1];
                                    endCount = endCount + 1;
                                }
                                else if (Constant.JOINTNAMES[i].Contains("pelvis"))
                                {
                                    rotation = MatrixOperation.Rotate(motion[1]);
                                }
                                else
                                {
                                    displacement = MatrixOperation.Transpose(MatrixOperation.Product(rotationList[i - 1], MatrixOperation.Transpose([position])));
                                    position = MatrixOperation.Sum(displacement, [positionList[i - 1]])[0];
                                    rotation = MatrixOperation.Product(rotationList[i - 1], MatrixOperation.Rotate(motion[1]));
                                }
                            }
                            positionList.Add(position);
                            rotationList.Add(rotation);
                            list.Add(new Tuple<double, string, double[]>(timestamp, Constant.JOINTNAMES[i], [position[0], position[1], position[2]]));
                        }
                        positionList = new List<double[]>();
                        rotationList = new List<double[][]>();
                        timestamp = timestamp + frameTime;
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Write a CSV file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dataList"></param>
        /// <param name="offset"></param>
        /// <param name="maximumFrame"></param>
        /// <param name="maximumFrame"></param>
        public static void WriteCSVFile(string path, object dataList, int offset, int maximumFrame)
        {
            string[] header = dataList.GetType() == typeof(List<Tuple<double, string, double[]>>) ? Constant.HEADER_POSTURE : Constant.HEADER_FOOTPRESSURE;
            string line;

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine(String.Join(",", header));
                for (int i = offset; i < maximumFrame; i++)
                {
                    if (header == Constant.HEADER_POSTURE)
                    {
                        line = String.Join(",", 
                            ((List<Tuple<double, string, double[]>>)dataList)[i].Item1,
                            ((List<Tuple<double, string, double[]>>)dataList)[i].Item2,
                            ((List<Tuple<double, string, double[]>>)dataList)[i].Item3.EnumerateToString(null, ","));
                    }
                    else
                    {
                        line = String.Join(",", ((List<double[]>)dataList)[i]);
                    }
                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Write CSV files after every turn
        /// </summary>
        /// <param name="indexList"></param>
        /// <param name="path"></param>
        /// <param name="dataName"></param>
        /// <param name="dataList"></param>
        /// <param name="offset"></param>
        /// <param name="maximumFrame"></param>
        /// <param name="frameRateRatio"></param>
        public static void WriteCSVFile(List<int> indexList, string path, object dataList, int offset, int maximumFrame, double frameRateRatio = 1)
        {
            string[] header = dataList.GetType() == typeof(List<Tuple<double, string, double[]>>) ? Constant.HEADER_POSTURE : Constant.HEADER_FOOTPRESSURE;
            string dataName = header == Constant.HEADER_POSTURE ? "Posture" : "FootPressure";

            foreach (int i in indexList)
            {
                int minimum = Math.Max(0, Math.Min(maximumFrame, (int)Math.Round((i - 5) * frameRateRatio)));
                int maximum = Math.Min(maximumFrame, (int)Math.Round((i + 5) * frameRateRatio));

                using (StreamWriter writer = new StreamWriter(path + (indexList.IndexOf(i) + 1).ToString("D" + 3) + "_" + dataName + ".csv"))
                {
                    writer.WriteLine(String.Join(",", header));
                    for (int j = minimum; j < maximum; j++)
                    {
                        if (header == Constant.HEADER_POSTURE)
                        {
                            for (int k = 0; k < Constant.BODYPARTS_POSTURE; k++)
                            {
                                writer.WriteLine(String.Join(",",
                                    ((List<Tuple<double, string, double[]>>)dataList)[(offset + j) * Constant.BODYPARTS_POSTURE + k].Item1,
                                    ((List<Tuple<double, string, double[]>>)dataList)[(offset + j) * Constant.BODYPARTS_POSTURE + k].Item2,
                                    ((List<Tuple<double, string, double[]>>)dataList)[(offset + j) * Constant.BODYPARTS_POSTURE + k].Item3.EnumerateToString(null, ",")));
                            }
                        }
                        else
                        {
                            writer.WriteLine(String.Join(",", ((List<double[]>)dataList)[offset + j]));
                        }
                    }
                }
            }
        }

        public static void CaptureScreen(string path, FrameworkElement element)
        {
            element.UpdateLayout();
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(new BitmapCacheBrush(element), null, new Rect(0, 0, width, height));
            }
            RenderTargetBitmap beatmap = new RenderTargetBitmap((int)width, (int)height, 96d, 96d, PixelFormats.Pbgra32);
            beatmap.Render(visual);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(beatmap));
                encoder.Save(stream);
            }
        }

        public static void WriteDATFile(string path, TextBox textBox1, TextBox textBox2)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine(String.Join(",", [textBox1.Name, textBox1.Text]));
                writer.WriteLine(String.Join(",", [textBox2.Name, textBox2.Text]));
            }
        }
    }
}
