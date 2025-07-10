using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataViewer
{
    public struct FileOperation
    {

        /// <summary>
        /// Read a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<string> ReadCSVFile(string filePath, string extension)
        {
            List<string> list = new List<string>();
            string line = "";
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    if (extension == ".bvh")
                    {
                        return ConvertBVHToCSV(reader);
                    }
                    else
                    {
                        if (extension == ".txt")
                        {
                            reader.ReadLine();
                            reader.ReadLine();
                        }
                        while (!reader.EndOfStream)
                        {
                            if (extension == ".csv")
                            {
                                line = reader.ReadLine();
                            }
                            else if (extension == ".txt")
                            {
                                line = reader.ReadLine().Replace("\t", ",");
                            }
                            list.Add(line);
                        }
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return list;
            }
        }

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
                        // turn viewer
                        return extension == ".bvh" ? new List<Tuple<double, string, double[]>>() : new List<double[]>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return extension == ".bvh" ? new List<Tuple<double, string, double[]>>() : new List<double[]>();
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
        /// Convert a BVH file
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<string> ConvertBVHToCSV(StreamReader reader)
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
            List<string> list = new List<string>();
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
                        //FrameCount_Posture.Text = line.Split(":")[1];
                        list.Add("timestamp,jointName,position_x,position_y,position_z");
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
                            list.Add(timestamp.ToString() + "," + Constant.JOINTNAMES[i] + "," + position[0].ToString() + "," + position[1].ToString() + "," + position[2].ToString());
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
    }
}
