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
        List<Tuple<double, string, double[]>> postureDataList = new List<Tuple<double, string, double[]>>();
        List<double[]> footPressureDataList = new List<double[]>();

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
            string folderPath;
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();
            openFileDialog.Title = "Select a folder";
            openFileDialog.IsFolderPicker = true;

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                folderPath = openFileDialog.FileName.Replace(".","/");
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
                        isCorrectFormat = (list[0].Split(",").Length == Constant.DIMENTIONS_POSTURE) && ((list.Count - 1) % Constant.BODYPARTS_POSTURE == 0);
                        if (isCorrectFormat) {
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
                                values = MatrixOperation.Division(valuePair.Value,list.Count / Constant.BODYPARTS_POSTURE);
                                postureDataList.Add(new Tuple<double, string, double[]>(values[0], valuePair.Key, [values[1], values[2], values[3]]));
                            }
                        }
                    }

                    foreach (string file in files.Where(path => path.EndsWith("FootPressure.csv") && !path.Contains("Trimmed") && !path.Contains("AllTurns")))
                    {
                        list = FileOperation.ReadCSVFile(file, ".csv");
                        isCorrectFormat = list[0].Split(",").Length == Constant.DIMENTIONS_FOOTPRESSURE;
                        if (isCorrectFormat)
                        {
                            list.RemoveAt(0);

                            values = new double[Constant.DIMENTIONS_FOOTPRESSURE];
                            Array.Fill(values, 0);
                            for (int i = 0; i < list.Count(); i++)
                            {
                                values_str = list[i].Split(",");
                                values = MatrixOperation.Sum(values, Array.ConvertAll(values_str, s => double.TryParse(s, out double x) ? x : 0));
                            }
                            values = MatrixOperation.Division(values, list.Count);
                            footPressureDataList.Add(values);
                        }
                    }
                    FolderPath.Text = folderPath;
                }
                catch
                {
                    FolderPath.Text = "(Loading File Failure)";
                }
            }
            Slider.Maximum = Math.Max((postureDataList.Count - 1) / Constant.BODYPARTS_POSTURE, footPressureDataList.Count - 1);
            SliderValueChanged(Slider, new RoutedPropertyChangedEventArgs<double>(1, 1));
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
                    FileOperation.WriteCSVFile(path + "_AllTurns_Posture.csv", postureDataList, 0, postureDataList.Count);
                }

                if (footPressureDataList.Count > 0)
                {
                    FileOperation.WriteCSVFile(path + "_AllTurns_FootPressure.csv", footPressureDataList, 0, footPressureDataList.Count);
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
                MeshBuilder meshBuilder = new MeshBuilder();
                List<Tuple<string, Point3D>> pointList = new List<Tuple<string, Point3D>>();
                DataDisplay.CreateMeshBuilder(postureDataList, 0, sliderValue, out pointList, out meshBuilder);
                double feetXPosition = (pointList[Array.IndexOf(Constant.JOINTNAMES, "l_foot")].Item2.X + pointList[Array.IndexOf(Constant.JOINTNAMES, "r_foot")].Item2.X) / 2.0;

                Right_Position.Value = Math.Max(feetXPosition, 0);
                Left_Position.Value = Math.Max(-feetXPosition, 0);
                ChangeBackgroundColor("Position");

                helixView.Children.Clear();
                helixView.Children.Add(new DefaultLights());
                helixView.Children.Add(new GridLinesVisual3D()
                {
                    MajorDistance = 5.0,
                    MinorDistance = 0.5,
                    Thickness = 0.01,
                });
                helixView.Children.Add(DataDisplay.CreateAngleLabel(pointList[Array.IndexOf(Constant.JOINTNAMES, "thorax")], pointList[Array.IndexOf(Constant.JOINTNAMES, "pelvis")], [0, 0, 2.5], Brushes.White));
                helixView.Children.Add(DataDisplay.CreateAngleLabel(pointList[Array.IndexOf(Constant.JOINTNAMES, "l_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "l_foot")], [-2.5, 0, 0], Brushes.Red));
                helixView.Children.Add(DataDisplay.CreateAngleLabel(pointList[Array.IndexOf(Constant.JOINTNAMES, "r_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "r_foot")], [2.5, 0, 0], Brushes.Blue));
                helixView.Children.Add(DataDisplay.CreateAngleDiffLabel(
                    pointList[Array.IndexOf(Constant.JOINTNAMES, "l_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "l_foot")],
                    pointList[Array.IndexOf(Constant.JOINTNAMES, "r_shank")], pointList[Array.IndexOf(Constant.JOINTNAMES, "r_foot")],
                    [0, 0, -2.5]));
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
                double[] pressureValue = footPressureDataList[sliderValue - 1];
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
        /// Exclude a specific turn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExcludeTurn(object sender, RoutedEventArgs e)
        {
            if (Slider.Maximum > 1)
            {
                postureDataList.RemoveRange(1 + ((int)Slider.Value - 1) * Constant.BODYPARTS_POSTURE, Constant.BODYPARTS_POSTURE);
                footPressureDataList.RemoveAt((int)Slider.Value);

                Slider.Value = 1;
                Slider.Maximum = Math.Max((postureDataList.Count - 1) / Constant.BODYPARTS_POSTURE, footPressureDataList.Count - 1);
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
            if (clickedButton.Name == "AnalysisView")
            {
                NavigationService.Navigate(Constant.ANALYSIS_VIEW);
            }
        }
    }
}
