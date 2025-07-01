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
            string[] jointNameList =
                    [ "pelvis", "abdomen", "thorax",
                    "l_clavicle", "l_uarm", "l_larm", "l_hand", "l_lathand", "end:l_hand", "l_medhand", "end:thorax:l_medhand",
                    "r_clavicle", "r_uarm", "r_larm", "r_hand", "r_lathand", "end:r_hand", "r_medhand", "end:thorax:r_medhand",
                    "neck", "head", "r_ear", "end:head:r_ear", "l_ear", "end:head:l_ear", "l_eye", "end:head:l_eye", "r_eye", "end:head:r_eye", "nose", "end:pelvis:nose",
                    "l_thigh", "l_shank", "l_foot", "l_toes", "l_toe", "end:l_toes:l_toe", "l_f_b_toe", "end:l_toes:l_f_b_toe", "l_f_m_toe", "end:pelvis",
                    "r_thigh", "r_shank", "r_foot", "r_toes", "r_toe", "end:r_toes:r_toe", "r_f_b_toe", "end:r_toes:r_f_b_toe", "r_f_m_toe", "end:" ];
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
                        for (int i = 0; i < jointNameList.Length; i++)
                        {
                            if (i != jointNameList.Length - 1)
                            {
                                motion[0] = [motionData[(i - endCount) * 6], motionData[(i - endCount) * 6 + 1], motionData[(i - endCount) * 6 + 2]];
                                motion[1] = [motionData[(i - endCount) * 6 + 3], motionData[(i - endCount) * 6 + 4], motionData[(i - endCount) * 6 + 5]];
                                position = MatrixOperation.Sum(offsetList[i], motion[0]);
                            }

                            if (nextParentJointName != "")
                            {
                                displacement = MatrixOperation.Transpose(MatrixOperation.Product(rotationList[Array.IndexOf(jointNameList, nextParentJointName)], MatrixOperation.Transpose([position])));
                                position = MatrixOperation.Sum(displacement, [positionList[Array.IndexOf(jointNameList, nextParentJointName)]])[0];
                                rotation = MatrixOperation.Product(rotationList[Array.IndexOf(jointNameList, nextParentJointName)], MatrixOperation.Rotate(motion[1]));
                                nextParentJointName = "";
                            }
                            else
                            {
                                if (jointNameList[i].Contains("end"))
                                {
                                    displacement = MatrixOperation.Transpose(MatrixOperation.Product(rotationList[i - 1], MatrixOperation.Transpose([offsetList[i]])));
                                    position = MatrixOperation.Sum(displacement, [positionList[i - 1]])[0];
                                    nextParentJointName = jointNameList[i].Split(":")[1];
                                    endCount = endCount + 1;
                                }
                                else if (jointNameList[i].Contains("pelvis"))
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
                            list.Add(timestamp.ToString() + "," + jointNameList[i] + "," + position[0].ToString() + "," + position[1].ToString() + "," + position[2].ToString());
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
        public static void WriteCSVFile(string path, List<string> dataList, int offset, int maximumFrame)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                if (offset != 0)
                {
                    writer.WriteLine(dataList[0]);
                }
                for (int i = offset; i < maximumFrame; i++)
                {
                    writer.WriteLine(dataList[i]);
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
        public static void WriteCSVFile(List<int> indexList, string path, string dataName, List<string> dataList, int offset, int maximumFrame, double frameRateRatio = 1)
        {
            foreach (int i in indexList)
            {
                int minimum = Math.Max(0, Math.Min(maximumFrame, (int)Math.Round((i - 5) * frameRateRatio)));
                int maximum = Math.Min(maximumFrame, (int)Math.Round((i + 5) * frameRateRatio));

                using (StreamWriter writer = new StreamWriter(path + (indexList.IndexOf(i) + 1).ToString("D" + 3) + "_" + dataName + ".csv"))
                {
                    writer.WriteLine(dataList[0]);
                    for (int j = minimum; j < maximum; j++)
                    {
                        if (dataName == "Posture")
                        {
                            for (int k = 0; k < Constant.BODYPARTS_POSTURE; k++)
                            {
                                writer.WriteLine(dataList[1 + (offset + j) * Constant.BODYPARTS_POSTURE + k]);
                            }
                        }
                        else if (dataName == "FootPressure")
                        {
                            writer.WriteLine(dataList[1 + offset + j]);
                        }
                    }
                }
            }
        }
    }
}
