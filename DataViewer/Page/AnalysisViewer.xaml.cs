using HelixToolkit.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

namespace DataViewer
{
    /// <summary>
    /// Analysis.xaml の相互作用ロジック
    /// </summary>
    public partial class AnalysisViewer : Page
    {
        List<string> postureDataList = new List<string>();
        List<string> footPressureDataList = new List<string>();
        bool isReverse = false;

        public AnalysisViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Open files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFiles(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string folderPath;
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();
            openFileDialog.Title = "Select a folder";
            openFileDialog.IsFolderPicker = true;

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                folderPath = openFileDialog.FileName.Replace(".", "/");
                try
                {
                    bool isCorrectFormat = false;
                    Dictionary<string, double[]> dictionaryEven, dictionaryOdd;
                    int turnCount;
                    List<string> list;
                    string[] files = Directory.GetFiles(folderPath);
                    string[] values_str;
                    double[] values, valuesEven, valuesOdd;

                    Array.Sort(files);
                    foreach (string file in files.Where(path => path.EndsWith("_AllTurns_Posture.csv")))
                    {
                        list = FileOperation.ReadCSVFile(file, ".csv");
                        isCorrectFormat = (list[0].Split(",").Length == Constant.DIMENTIONS_POSTURE) && ((list.Count - 1) % Constant.BODYPARTS_POSTURE == 0);
                        if (isCorrectFormat)
                        {
                            if (postureDataList.Count == 0)
                            {
                                postureDataList.Add(list[0]);
                            }
                            list.RemoveAt(0);

                            dictionaryEven = new Dictionary<string, double[]>();
                            dictionaryOdd = new Dictionary<string, double[]>();
                            for (int i = 0; i < list.Count() / Constant.BODYPARTS_POSTURE; i++)
                            {
                                for (int j = 0; j < Constant.BODYPARTS_POSTURE; j++)
                                {
                                    values_str = list[i * Constant.BODYPARTS_POSTURE + j].Split(",");
                                    values = [double.Parse(values_str[0]), double.Parse(values_str[2]), double.Parse(values_str[3]), double.Parse(values_str[4])];
                                    if (i % 2 == 0)
                                    {
                                        if (dictionaryOdd.ContainsKey(values_str[1]))
                                        {
                                            dictionaryOdd[values_str[1]] = MatrixOperation.Sum(dictionaryOdd[values_str[1]], values);
                                        }
                                        else
                                        {
                                            dictionaryOdd.Add(values_str[1], values);
                                        }

                                    }
                                    else
                                    {
                                        if (dictionaryEven.ContainsKey(values_str[1]))
                                        {
                                            dictionaryEven[values_str[1]] = MatrixOperation.Sum(dictionaryEven[values_str[1]], values);
                                        }
                                        else
                                        {
                                            dictionaryEven.Add(values_str[1], values);
                                        }
                                    }
                                }
                            }

                            turnCount = list.Count / Constant.BODYPARTS_POSTURE / 2;
                            foreach (KeyValuePair<string, double[]> valuePair in dictionaryOdd)
                            {
                                values = MatrixOperation.Division(valuePair.Value, turnCount % 2 == 0 ? turnCount : turnCount + 1);
                                postureDataList.Add(values[0] + "," + valuePair.Key + "," + values[1] + "," + values[2] + "," + values[3]);
                            }
                            foreach (KeyValuePair<string, double[]> valuePair in dictionaryEven)
                            {
                                values = MatrixOperation.Division(valuePair.Value, turnCount);
                                postureDataList.Add(values[0] + "," + valuePair.Key + "," + values[1] + "," + values[2] + "," + values[3]);
                            }

                            if (double.Parse(postureDataList[1].Split(",")[2]) < double.Parse(postureDataList[1 + Constant.BODYPARTS_POSTURE].Split(",")[2]))
                            {
                                isReverse = true;
                            }
                        }
                    }

                    foreach (string file in files.Where(path => path.EndsWith("_AllTurns_FootPressure.csv")))
                    {
                        list = FileOperation.ReadCSVFile(file, ".csv");
                        isCorrectFormat = list[0].Split(",").Length == Constant.DIMENTIONS_FOOTPRESSURE;
                        if (isCorrectFormat)
                        {
                            if (footPressureDataList.Count == 0)
                            {
                                footPressureDataList.Add(list[0]);
                            }
                            list.RemoveAt(0);

                            valuesEven = new double[Constant.DIMENTIONS_FOOTPRESSURE];
                            valuesOdd = new double[Constant.DIMENTIONS_FOOTPRESSURE];
                            Array.Fill(valuesEven, 0);
                            Array.Fill(valuesOdd, 0);
                            for (int i = 0; i < list.Count(); i++)
                            {
                                values_str = list[i].Split(",");
                                if (i % 2 == 0)
                                {
                                    valuesOdd = MatrixOperation.Sum(valuesOdd, Array.ConvertAll(values_str, s => double.TryParse(s, out double x) ? x : 0));
                                }
                                else
                                {
                                    valuesEven = MatrixOperation.Sum(valuesEven, Array.ConvertAll(values_str, s => double.TryParse(s, out double x) ? x : 0));
                                }
                            }

                            turnCount = list.Count / 2;

                            valuesOdd = MatrixOperation.Division(valuesOdd, turnCount % 2 == 0 ? turnCount : turnCount + 1);
                            footPressureDataList.Add(string.Join(",", valuesOdd));
                            valuesEven = MatrixOperation.Division(valuesEven, turnCount);
                            footPressureDataList.Add(string.Join(",", valuesEven));
                        }
                    }
                    FolderPath.Text = folderPath;
                }
                catch
                {
                    FolderPath.Text = "(Loading File Failure)";
                }
                RadioButtonChecked(LeftTurn, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Check numeric text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumericTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;
            if (!Regex.IsMatch(textbox.Text, @"^\d*$"))
            {
                textbox.Text = "0";
            }
        }

        /// <summary>
        /// Export modified data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Export(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            List<int> leftTurnIndexList = new List<int>();
            List<int> rightTurnIndexList = new List<int>();
            string path;

            CommonOpenFileDialog saveFileDialog = new CommonOpenFileDialog();
            saveFileDialog.Title = "Save files";
            saveFileDialog.IsFolderPicker = true;
            if (saveFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                path = saveFileDialog.FileName.Replace(".", "") + "/" + saveFileDialog.FileName.Split("\\").Last();

                if (postureDataList.Count > 0)
                {
                    FileOperation.WriteCSVFile(path + "_Summary_Posture.csv", postureDataList, 0, postureDataList.Count);
                }

                if (footPressureDataList.Count > 0)
                {
                    FileOperation.WriteCSVFile(path + "_Summary_FootPressure.csv", footPressureDataList, 0, footPressureDataList.Count);
                }
            }
        }

        private void RadioButtonChecked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            bool isLeftTurn = radioButton.Name == "LeftTurn";

            if (postureDataList.Count / Constant.BODYPARTS_POSTURE > 1)
            {
                double feetXPosition = 0;
                double[] position;
                string[] position_str;
                MeshBuilder meshBuilder = new MeshBuilder();
                List<Tuple<string, Point3D>> pointList = new List<Tuple<string, Point3D>>();
                Point3D point;
                helixView.Children.Clear();
                for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
                {
                    position_str = postureDataList[1 + Convert.ToInt32(isLeftTurn == isReverse) * Constant.BODYPARTS_POSTURE + i].Split(",");
                    position = Array.ConvertAll(position_str, s => double.TryParse(s, out double x) ? x : 0);
                    pointList.Add(new Tuple<string, Point3D>(position_str[1], new Point3D(position[2], position[3], position[4])));
                    meshBuilder.AddSphere(pointList[i].Item2, 0.1);

                    if (i == 33 || i == 43)
                    {
                        feetXPosition += position[2] / 2.0;
                    }
                    if (i != 0)
                    {
                        if (pointList[i - 1].Item1.Contains("end:"))
                        {
                            point = pointList.Find((p) => p.Item1.Contains(pointList[i - 1].Item1.Split(":")[1])).Item2;
                        }
                        else
                        {
                            point = pointList[i - 1].Item2;
                        }
                        meshBuilder.AddCylinder(point, pointList[i].Item2, 0.05);
                    }
                }

                Right_Position.Value = Math.Max(feetXPosition, 0);
                Left_Position.Value = Math.Max(-feetXPosition, 0);
                ChangeBackgroundColor("Position");

                helixView.Children.Add(new DefaultLights());
                helixView.Children.Add(new GridLinesVisual3D());
                helixView.Children.Add(new ModelVisual3D
                {
                    Content = new GeometryModel3D(
                        meshBuilder.ToMesh(),
                        new DiffuseMaterial(new SolidColorBrush(Colors.Blue)))
                });
            }

            if (footPressureDataList.Count > 1)
            {
                byte colorValue;
                string[] feets = ["Left", "Right"];
                string[] pressureValues_str = footPressureDataList[1 + Convert.ToInt32(isLeftTurn == isReverse)].Split(",");
                double[] pressureValue = Array.ConvertAll(pressureValues_str, s => double.TryParse(s, out double x) ? x : 0);
                System.Windows.Shapes.Path path;
                ProgressBar progressBar;
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        colorValue = (byte)(255 - Math.Min(255, 25.5 * (int)pressureValue[1 + i * 26 + j]));
                        path = FindName($"{feets[i]}_{j + 1}") as System.Windows.Shapes.Path;
                        path.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(colorValue, colorValue, colorValue));
                    }
                    progressBar = FindName($"{feets[i]}_Acceleration") as ProgressBar;
                    progressBar.Value = Math.Max(pressureValue[18 + i * 25], 0);

                    progressBar = FindName($"{feets[i]}_Pressure") as ProgressBar;
                    progressBar.Value = pressureValue[23 + i * 25];

                }
                ChangeBackgroundColor("Acceleration");
                ChangeBackgroundColor("Pressure");
            }
        }

        /// <summary>
        /// Change a background color of each gauge
        /// </summary>
        /// <param name="metricsName"></param>
        private void ChangeBackgroundColor(string metricsName)
        {
            bool isOutOfThreshold = false;
            double threshold = 0;
            ProgressBar progressBar_Left = FindName($"Left_{metricsName}") as ProgressBar;
            ProgressBar progressBar_Right = FindName($"Right_{metricsName}") as ProgressBar;
            if (metricsName == "Position")
            {
                double.TryParse("0." + Threshold_Position.Text, out threshold);
                isOutOfThreshold = progressBar_Left.Value > threshold || progressBar_Right.Value > threshold;
            }
            else if (metricsName == "Acceleration")
            {
                double.TryParse("0." + Threshold_Acceleration.Text, out threshold);
                isOutOfThreshold = (progressBar_Left.Value + progressBar_Right.Value) / 2.0 < threshold;

            }
            else if (metricsName == "Pressure")
            {
                double.TryParse(Threshold_Pressure.Text, out threshold);
                isOutOfThreshold = progressBar_Left.Value > threshold || progressBar_Right.Value > threshold;
            }

            if (isOutOfThreshold)
            {
                progressBar_Left.Background = new SolidColorBrush(Colors.Yellow);
                progressBar_Right.Background = new SolidColorBrush(Colors.Yellow);
            }
            else
            {
                progressBar_Left.Background = new SolidColorBrush(Colors.LightGray);
                progressBar_Right.Background = new SolidColorBrush(Colors.LightGray);
            }
        }

        /// <summary>
        /// Change a viewer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeViewer(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            if (clickedButton.Name == "NomalView")
            {
                NavigationService.Navigate(Constant.NOMAL_VIEW);
            }
            if (clickedButton.Name == "TurnView")
            {
                NavigationService.Navigate(Constant.TURN_VIEW);
            }
        }
    }
}
