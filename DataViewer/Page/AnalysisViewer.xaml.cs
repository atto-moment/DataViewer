using HelixToolkit.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataViewer
{
    /// <summary>
    /// Analysis.xaml の相互作用ロジック
    /// </summary>
    public partial class AnalysisViewer : Page
    {
        List<Tuple<double, string, double[]>> postureDataList = new List<Tuple<double, string, double[]>>();
        List<double[]> footPressureDataList = new List<double[]>();
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
                    string[] files = Directory.GetFiles(folderPath);
                    List<Tuple<double, string, double[]>> _postureDataList = new List<Tuple<double, string, double[]>>();
                    List<double[]> _footPressureDataList = new List<double[]>();

                    postureDataList = new List<Tuple<double, string, double[]>>();
                    footPressureDataList = new List<double[]>();

                    FileOperation.GetMeanValues(files.Where(path => path.Contains("_AllTurns_")).ToArray(), out _postureDataList, out _footPressureDataList, 0, 2);
                    postureDataList.AddRange(_postureDataList);
                    footPressureDataList.AddRange(_footPressureDataList);
                    
                    FileOperation.GetMeanValues(files.Where(path => path.Contains("_AllTurns_")).ToArray(), out _postureDataList, out _footPressureDataList, 1, 2);
                    postureDataList.AddRange(_postureDataList);
                    footPressureDataList.AddRange(_footPressureDataList);

                    if (postureDataList[0].Item3[0] < postureDataList[Constant.BODYPARTS_POSTURE].Item3[0])
                    {
                        isReverse = true;
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
            TextBox textBox = sender as TextBox;
            textBox.Text = Regex.IsMatch(textBox.Text, @"^\d*$") ? textBox.Text : "0";
        }

        /// <summary>
        /// Export modified data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Export(object sender, RoutedEventArgs e)
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
                if (clickedButton.Name.Contains("CSV"))
                {
                    if (postureDataList.Count > 0)
                    {
                       // FileOperation.WriteCSVFile(path + "_AllTurns_Posture.csv", postureDataList, 0, postureDataList.Count);
                    }
                    if (footPressureDataList.Count > 0)
                    {
                        //FileOperation.WriteCSVFile(path + "_AllTurns_FootPressure.csv", footPressureDataList, 0, footPressureDataList.Count);
                    }
                }
                else if (clickedButton.Name.Contains("PNG"))
                {
                    string[] feets = ["Left", "Right"];
                    RadioButton radioButton;
                    for (int i = 0; i < 2; i++)
                    {
                        radioButton = FindName($"{feets[i]}Turn") as RadioButton;
                        radioButton.IsChecked = true;
                        await Task.Delay(50);
                        FileOperation.CaptureScreen(path + $"_{feets[i]}Turn.png", DataViewer);
                        await Task.Delay(50);
                    }
                }
            }
        }

        /// <summary>
        /// View left/right turn summary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButtonChecked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            bool isLeftTurn = radioButton.Name == "LeftTurn";

            if (postureDataList.Count / Constant.BODYPARTS_POSTURE > 1)
            {
                MeshBuilder meshBuilder = new MeshBuilder();
                List<Tuple<string, Point3D>> pointList = new List<Tuple<string, Point3D>>();
                DataDisplay.CreateMeshBuilder(postureDataList, 0, 1 + Convert.ToInt32(isLeftTurn == isReverse), out pointList, out meshBuilder);
                double feetXPosition = (pointList[Array.IndexOf(Constant.JOINTNAMES, "l_foot")].Item2.X + pointList[Array.IndexOf(Constant.JOINTNAMES, "r_foot")].Item2.X) / 2.0;

                Right_Position.Value = Math.Max(feetXPosition, 0);
                Left_Position.Value = Math.Max(-feetXPosition, 0);
                ChangeBackgroundColor("Position");

                helixView.Children.Clear();
                helixView.Children.Add(new ModelVisual3D { Content = new AmbientLight { Color = Colors.White } });
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

            if (footPressureDataList.Count > 1)
            {
                byte colorValue;
                string[] feets = ["Left", "Right"];
                double[] pressureValue = footPressureDataList[Convert.ToInt32(isLeftTurn == isReverse)];
                System.Windows.Shapes.Path path;
                Label label;
                Ellipse ellipse;
                ProgressBar progressBar;
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        colorValue = (byte)(255 - Math.Min(255, 25.5 * (int)pressureValue[1 + i * 25 + j]));
                        path = FindName($"{feets[i]}_{j + 1}") as System.Windows.Shapes.Path;
                        path.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(colorValue, colorValue, colorValue));

                        label = FindName($"{feets[i]}_{j + 1}_Label") as Label;
                        label.Content = pressureValue[1 + i * 25 + j];
                        label.Foreground = new SolidColorBrush(colorValue > 128 ? Colors.Black : Colors.White);
                    }
                    progressBar = FindName($"{feets[i]}_Angular") as ProgressBar;
                    progressBar.Value = pressureValue[20 + i * 25];

                    progressBar = FindName($"{feets[i]}_Pressure") as ProgressBar;
                    progressBar.Value = pressureValue[23 + i * 25];

                    ellipse = FindName($"{feets[i]}_COP") as Ellipse;
                    ellipse.Margin = new Thickness((i == 0 ? -1 : 1) * (45 - pressureValue[25 + i * 25] * 250), -pressureValue[24 + i * 25] * 1000, 0, 0);
                }
                ChangeBackgroundColor("Angular");
                ChangeBackgroundColor("Pressure");
            }
        }

        /// <summary>
        /// Change the background color of each gauge
        /// </summary>
        /// <param name="metricsName"></param>
        private void ChangeBackgroundColor(string metricsName)
        {
            ProgressBar progressBar_Left = FindName($"Left_{metricsName}") as ProgressBar;
            ProgressBar progressBar_Right = FindName($"Right_{metricsName}") as ProgressBar;
            TextBox textBox = FindName($"Threshold_{metricsName}") as TextBox;
            SolidColorBrush solidColorBrush = DataDisplay.ChangeBackGroundColor(metricsName, progressBar_Left.Value, progressBar_Right.Value, textBox.Text);
            progressBar_Left.Background = solidColorBrush;
            progressBar_Right.Background = solidColorBrush;
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
