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
    /// TurnViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class TurnViewer : Page
    {
        static int DIMENTIONS_POSTURE = 5;
        static int BODYPARTS_POSTURE = 51;
        static int DIMENTIONS_FOOTPRESSURE = 51;

        List<string> postureDataList = new List<string>();
        List<string> footPressureDataList = new List<string>();

        public TurnViewer()
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
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();
            openFileDialog.Title = "Select a folder";
            openFileDialog.IsFolderPicker = true;

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string folderPath = openFileDialog.FileName.Replace(".","/");
                try
                {
                    bool isCorrectFormat = false;
                    Dictionary<string, double[]> dictionary;
                    List<string> list;
                    string[] files = Directory.GetFiles(folderPath);
                    string[] values_str;
                    double[] values;

                    Array.Sort(files);
                    foreach (string file in files.Where(path => path.EndsWith("Posture.csv") && !path.Contains("Trimmed") && !path.Contains("AllTurns")))
                    {
                        list = FileOperation.ReadCSVFile(file, ".csv");
                        isCorrectFormat = (list[0].Split(",").Length == DIMENTIONS_POSTURE) && ((list.Count - 1) % BODYPARTS_POSTURE == 0);
                        if (isCorrectFormat) {
                            if(postureDataList.Count == 0)
                            {
                                postureDataList.Add(list[0]);
                            }
                            list.RemoveAt(0);

                            dictionary = new Dictionary<string, double[]>();
                            for (int i = 0; i < list.Count(); i++)
                            {
                                values_str = list[i].Split(",");
                                values = [double.Parse(values_str[0]), double.Parse(values_str[2]), double.Parse(values_str[3]), double.Parse(values_str[4])];
                                if (dictionary.ContainsKey(values_str[1])) {
                                    dictionary[values_str[1]] = MatrixOperation.Sum(dictionary[values_str[1]], values);
                                }
                                else
                                {
                                    dictionary.Add(values_str[1], values);
                                }
                            }

                            foreach (KeyValuePair<string,double[]> valuePair in dictionary)
                            {
                                values = MatrixOperation.Division(valuePair.Value,list.Count / BODYPARTS_POSTURE);
                                postureDataList.Add(values[0] + "," + valuePair.Key + "," + values[1] + "," + values[2] + "," + values[3]);
                            }
                        }
                    }

                    foreach (string file in files.Where(path => path.EndsWith("FootPressure.csv") && !path.Contains("Trimmed") && !path.Contains("AllTurns")))
                    {
                        list = FileOperation.ReadCSVFile(file, ".csv");
                        isCorrectFormat = list[0].Split(",").Length == DIMENTIONS_FOOTPRESSURE;
                        if (isCorrectFormat)
                        {
                            if (footPressureDataList.Count == 0)
                            {
                                footPressureDataList.Add(list[0]);
                            }
                            list.RemoveAt(0);

                            values = new double[DIMENTIONS_FOOTPRESSURE];
                            Array.Fill(values, 0);
                            for (int i = 0; i < list.Count(); i++)
                            {
                                values_str = list[i].Split(",");
                                values = MatrixOperation.Sum(values, Array.ConvertAll(values_str, s => double.TryParse(s, out double x) ? x : 0));
                            }
                            values = MatrixOperation.Division(values, list.Count);
                            footPressureDataList.Add(string.Join(",", values));
                        }
                    }
                }
                catch
                {
                    FolderPath.Text = "(Loading File Failure)";
                }
            }
            Slider.Maximum = Math.Max((postureDataList.Count - 1) / BODYPARTS_POSTURE, footPressureDataList.Count - 1);
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
                    using (StreamWriter writer = new StreamWriter(path + "_AllTurns_Posture.csv"))
                    {
                        for (int i = 0; i < postureDataList.Count; i++)
                        {
                            writer.WriteLine(postureDataList[i]);
                        }
                    }
                }

                if (footPressureDataList.Count > 0)
                {
                    using (StreamWriter writer = new StreamWriter(path + "_AllTurns_FootPressure.csv"))
                    {
                        for (int i = 0; i < footPressureDataList.Count; i++)
                        {
                            writer.WriteLine(footPressureDataList[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// View data of a specific frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int sliderValue = (int)Slider.Value;

            if (sliderValue < postureDataList.Count)
            {
                double feetXPosition = 0;
                double[] position;
                string[] position_str;
                MeshBuilder meshBuilder = new MeshBuilder();
                List<Tuple<string, Point3D>> pointList = new List<Tuple<string, Point3D>>();
                Point3D point;
                helixView.Children.Clear();
                for (int i = 0; i < BODYPARTS_POSTURE; i++)
                {
                    position_str = postureDataList[1 + (sliderValue - 1) * BODYPARTS_POSTURE + i].Split(",");
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

            if (sliderValue < footPressureDataList.Count)
            {
                byte colorValue;
                string[] feets = ["Left", "Right"];
                string[] pressureValues_str = footPressureDataList[1 + (sliderValue - 1)].Split(",");
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
            Page page;

            if (clickedButton.Name == "NomalView")
            {
                page = new Viewer();
                NavigationService.Navigate(page);
            }
        }
    }
}
