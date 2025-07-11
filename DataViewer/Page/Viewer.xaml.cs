using HelixToolkit.Wpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace DataViewer
{
    /// <summary>
    /// Viewer.xaml の相互作用ロジック
    /// </summary>
    public partial class Viewer : Page
    {
        List<Tuple<double, string, double[]>> postureDataList = new List<Tuple<double, string, double[]>>();
        List<double[]> footPressureDataList = new List<double[]>();

        public Viewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Open a file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFile(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string dataName = clickedButton.Name.Split("_")[1];

            Dictionary<string, string> extensions = new Dictionary<string, string>();
            extensions.Add("Posture", "(*.csv;*.bvh)|*.csv;*.bvh");
            extensions.Add("FootPressure", "(*.csv;*.txt)|*.csv;*.txt");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a file";
            openFileDialog.Filter = "All Supported Format" + extensions[dataName];
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                TextBox textBox_FilePath = FindName($"FilePath_{dataName}") as TextBox;
                TextBox textBox_FrameCount = FindName($"FrameCount_{dataName}") as TextBox;
                TextBox textBox_FrameRate = FindName($"FrameRate_{dataName}") as TextBox;
                TextBox textBox_FrameOffset = FindName($"FrameOffset _{dataName}") as TextBox;
                try
                {
                    bool isCorrectFormat = false;
                    int frameCount = 0;
                    int frameRate = 0;
                    string extension = System.IO.Path.GetExtension(openFileDialog.FileName);
                    if (clickedButton == Button_Posture)
                    {
                        postureDataList = (List<Tuple<double, string, double[]>>)FileOperation.ReadAllFrames(filePath, extension);
                        isCorrectFormat = (postureDataList[0].Item3.Length == Constant.DIMENTIONS_POSTURE - 2) && (postureDataList.Count % Constant.BODYPARTS_POSTURE == 0);
                        frameCount = postureDataList.Count / Constant.BODYPARTS_POSTURE;
                        frameRate = (int)Math.Round(1 / (postureDataList[Constant.BODYPARTS_POSTURE].Item1 - postureDataList[0].Item1));
                    }
                    else if (clickedButton == Button_FootPressure)
                    {
                        footPressureDataList = (List<double[]>)FileOperation.ReadAllFrames(filePath, extension);
                        isCorrectFormat = footPressureDataList[0].Length == Constant.DIMENTIONS_FOOTPRESSURE;
                        frameCount = footPressureDataList.Count;
                        frameRate = (int)Math.Round(1 / (footPressureDataList[1][0] - footPressureDataList[0][0]));
                    }

                    if (isCorrectFormat)
                    {
                        textBox_FilePath.Text = filePath;
                        textBox_FrameCount.Text = frameCount.ToString();
                        textBox_FrameRate.Text = frameRate.ToString();
                    }
                    else
                    {
                        textBox_FilePath.Text = "(Invalid Format File)";
                        textBox_FrameCount.Text = "0";
                    }
                }
                catch
                {
                    textBox_FilePath.Text = "(Loading File Failure)";
                    textBox_FrameCount.Text = "0";
                }
            }
            Slider.Maximum = Math.Max(int.Parse(FrameCount_Posture.Text), (int)Math.Round(int.Parse(FrameCount_FootPressure.Text) * double.Parse(FrameRateRatio.Text)));
            FrameOffset_Maximum.Text = Slider.Maximum.ToString();
            SliderValueChanged(Slider, new RoutedPropertyChangedEventArgs<double>(1, 1));
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
        /// Calculate frame rate ratio
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrameRateChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text != "0")
            {
                if (FrameRate_FootPressure.Text != "0" && FrameRate_Posture.Text != "0")
                {
                    FrameRateRatio.Text = (double.Parse(FrameRate_FootPressure.Text) / double.Parse(FrameRate_Posture.Text)).ToString();
                }
                else
                {
                    FrameRateRatio.Text = "1";
                }
            }
            else
            {
            }
        }

        /// <summary>
        /// Set frame offsets
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetOffset(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string dataName = clickedButton.Name.Split("_")[1];
            TextBox textBox_FrameOffset = FindName($"FrameOffset_{dataName}") as TextBox;
            textBox_FrameOffset.Text = (int.Parse(textBox_FrameOffset.Text) + int.Parse(clickedButton.Tag.ToString())).ToString();
        }

        /// <summary>
        /// Trim data frames
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrimDataFrames(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string operate = clickedButton.Name.Split("_")[0];
            string dataName = clickedButton.Name.Split("_")[1];
            TextBox textBox = FindName($"FrameOffset_{dataName}") as TextBox;
            PropertyInfo property = typeof(Slider).GetProperty(dataName);
            if (operate == "Set")
            {
                property.SetValue(Slider, Slider.Value);
                textBox.Text = Slider.Value.ToString();
            }
            else if (operate == "Reset")
            {

                if (dataName == "Minimum")
                {
                    property.SetValue(Slider, 1);
                    textBox.Text = Slider.Minimum.ToString();
                }
                else if (dataName == "Maximum")
                {
                    property.SetValue(Slider, Math.Max(int.Parse(FrameCount_Posture.Text), (int)Math.Round(int.Parse(FrameCount_FootPressure.Text) * double.Parse(FrameRateRatio.Text))));
                    textBox.Text = Slider.Maximum.ToString();
                }
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
            bool isLeftTurn = false;
            double xPosition = 0;
            double previousXPosition;
            List<int> TurnIndexList = new List<int>();
            int offset;
            int maximumFrame;
            string path;

            CommonOpenFileDialog saveFileDialog = new CommonOpenFileDialog();
            saveFileDialog.Title = "Save files";
            saveFileDialog.IsFolderPicker = true;

            if (saveFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (!Directory.Exists(saveFileDialog.FileName))
                {
                    Directory.CreateDirectory(saveFileDialog.FileName);
                }
                path = saveFileDialog.FileName.Replace(".", "") + "/" + saveFileDialog.FileName.Split("\\").Last();

                if (postureDataList.Count > 0)
                {
                    offset = int.Parse(FrameOffset_Minimum.Text) + int.Parse(FrameOffset_Posture.Text) - 1;
                    maximumFrame = Math.Min(int.Parse(FrameOffset_Maximum.Text) + int.Parse(FrameOffset_Posture.Text), int.Parse(FrameCount_Posture.Text));

                    if (clickedButton.Name == "Export_CSV")
                    {
                        FileOperation.WriteCSVFile(path + "_Trimmed_Posture.csv", postureDataList, offset * Constant.BODYPARTS_POSTURE, (maximumFrame - 1) * Constant.BODYPARTS_POSTURE);
                    }
                    else if (clickedButton.Name == "Extract")
                    {
                        for (int i = offset; i < maximumFrame; i++)
                        {
                            previousXPosition = xPosition;
                            xPosition = (postureDataList[i * Constant.BODYPARTS_POSTURE + Array.IndexOf(Constant.JOINTNAMES, "l_foot")].Item3[0] + postureDataList[i * Constant.BODYPARTS_POSTURE + Array.IndexOf(Constant.JOINTNAMES, "r_foot")].Item3[0]) / 2.0;
                            if (i == offset + 2)
                            {
                                isLeftTurn = xPosition > previousXPosition;
                            }
                            else if (i > offset + 2)
                            {
                                if (isLeftTurn && xPosition < previousXPosition)
                                {
                                    isLeftTurn = !isLeftTurn;
                                    TurnIndexList.Add(i);
                                }
                                else if (!isLeftTurn && xPosition > previousXPosition)
                                {
                                    isLeftTurn = !isLeftTurn;
                                    TurnIndexList.Add(i);
                                }
                            }
                        }
                        FileOperation.WriteCSVFile(TurnIndexList, path + "_Turn_", postureDataList, int.Parse(FrameOffset_Posture.Text), maximumFrame);
                    }

                }

                if (footPressureDataList.Count > 0)
                {
                    offset = (int)Math.Round((int.Parse(FrameOffset_Minimum.Text) - 1) * double.Parse(FrameRateRatio.Text)) + int.Parse(FrameOffset_FootPressure.Text);
                    maximumFrame = Math.Min((int)Math.Round(double.Parse(FrameOffset_Maximum.Text) * double.Parse(FrameRateRatio.Text)) + int.Parse(FrameOffset_FootPressure.Text), int.Parse(FrameCount_FootPressure.Text));
                    if (clickedButton.Name == "Export_CSV")
                    {
                        FileOperation.WriteCSVFile(path + "_Trimmed_FootPressure.csv", footPressureDataList, offset, maximumFrame);
                    }
                    else if (clickedButton.Name == "Extract")
                    {
                        FileOperation.WriteCSVFile(TurnIndexList, path + "_Turn_", footPressureDataList, int.Parse(FrameOffset_FootPressure.Text), maximumFrame, double.Parse(FrameRateRatio.Text));
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
            int frameCount_FootPressure = int.Parse(FrameCount_FootPressure.Text);
            int frameCount_Posture = int.Parse(FrameCount_Posture.Text);
            int frameOffset_FootPressure = int.Parse(FrameOffset_FootPressure.Text);
            int frameOffset_Posture = int.Parse(FrameOffset_Posture.Text);
            int sliderValue = (int)Slider.Value;
            int correctedSliderValue = (int)Math.Round(sliderValue * double.Parse(FrameRateRatio.Text));

            if (sliderValue + frameOffset_Posture < frameCount_Posture)
            {
                List<Tuple<string, Point3D>> pointList = new List<Tuple<string, Point3D>>();
                MeshBuilder meshBuilder = new MeshBuilder();
                DataDisplay.CreateMeshBuilder(postureDataList, frameOffset_Posture, sliderValue, out pointList, out meshBuilder);

                double feetXPosition = (pointList[Array.IndexOf(Constant.JOINTNAMES, "l_foot")].Item2.X + pointList[Array.IndexOf(Constant.JOINTNAMES, "r_foot")].Item2.X) / 2.0;                
                Right_Position.Value = Math.Max(feetXPosition, 0);
                Left_Position.Value = Math.Max(-feetXPosition, 0);
                ChangeBackgroundColor("Position");

                helixView.Children.Clear();
                helixView.Children.Add(new ModelVisual3D { Content = new AmbientLight { Color = Colors.White} });
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

            if (correctedSliderValue + frameOffset_FootPressure < frameCount_FootPressure)
            {
                byte colorValue;
                string[] feets = ["Left", "Right"];
                double[] pressureValue = footPressureDataList[correctedSliderValue + frameOffset_FootPressure];
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
                    progressBar = FindName($"{feets[i]}_Acceleration") as ProgressBar;
                    progressBar.Value = Math.Max(pressureValue[18 + i * 25], 0);

                    progressBar = FindName($"{feets[i]}_Pressure") as ProgressBar;
                    progressBar.Value = pressureValue[23 + i * 25];

                    ellipse = FindName($"{feets[i]}_COP") as Ellipse;
                    ellipse.Margin = new Thickness((i == 0 ? -1 : 1) * (45 - pressureValue[24 + i * 25] * 250), 75 - pressureValue[25 + i * 25] * 1000,0,0);

                }
                ChangeBackgroundColor("Acceleration");
                ChangeBackgroundColor("Pressure");
            }

            if (Slider.Value == (int)Slider.Maximum)
            {
                Play.IsEnabled = false;
                Pause.IsEnabled = false;
                Stop.IsEnabled = true;
            }
            else if (Slider.Value == 1)
            {
                Play.IsEnabled = true;
                Pause.IsEnabled = false;
                Stop.IsEnabled = false;
            }
            else if (Slider.Value > 1 && !Stop.IsEnabled)
            {
                Stop.IsEnabled = true;
            }
            else if (Slider.Value > 1 && !Pause.IsEnabled)
            {
                Play.IsEnabled = true;
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
        /// Operate a player
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OperatePlayer(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == Play)
            {
                Play.IsEnabled = false;
                Pause.IsEnabled = true;
                Stop.IsEnabled = true;
                while ((int)Slider.Value < (int)Slider.Maximum)
                {
                    await Task.Delay(1);
                    Slider.Value += 1;
                    if (Play.IsEnabled)
                    {
                        break;
                    }
                }
            }
            else if (clickedButton == Pause)
            {
                Play.IsEnabled = true;
                Pause.IsEnabled = false;
                Stop.IsEnabled = true;
            }
            else if (clickedButton == Stop)
            {
                Play.IsEnabled = true;
                Pause.IsEnabled = false;
                Stop.IsEnabled = false;
                Slider.Value = 0;
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
            if (clickedButton.Name == "TurnView")
            {
                NavigationService.Navigate(Constant.TURN_VIEW);
            }
            if (clickedButton.Name == "AnalysisView")
            {
                NavigationService.Navigate(Constant.ANALYSIS_VIEW);
            }
        }
    }
}
