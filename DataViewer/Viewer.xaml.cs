using DataViewer.Class;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DataViewer
{
    /// <summary>
    /// Viewer.xaml の相互作用ロジック
    /// </summary>
    public partial class Viewer : Page
    {
        List<string> postureDataList = new List<string>();
        List<string> footPressureDataList = new List<string>();

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
                        postureDataList = FileOperation.ReadCSVFile(filePath, extension);
                        isCorrectFormat = (postureDataList[0].Split(",").Length == Constant.DIMENTIONS_POSTURE) && ((postureDataList.Count - 1) % Constant.BODYPARTS_POSTURE == 0);
                        frameCount = postureDataList.Count / Constant.BODYPARTS_POSTURE;
                        frameRate = (int)Math.Round(1 / (double.Parse(postureDataList[Constant.BODYPARTS_POSTURE + 1].Split(",")[0]) - double.Parse(postureDataList[1].Split(",")[0])));
                    }
                    else if (clickedButton == Button_FootPressure)
                    {
                        footPressureDataList = FileOperation.ReadCSVFile(filePath, extension);
                        isCorrectFormat = footPressureDataList[0].Split(",").Length == Constant.DIMENTIONS_FOOTPRESSURE;
                        frameCount = footPressureDataList.Count;
                        frameRate = (int)Math.Round(1 / (double.Parse(footPressureDataList[2].Split(",")[0]) - double.Parse(footPressureDataList[1].Split(",")[0])));
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

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save files";
            saveFileDialog.Filter = "Folder|.";
            if (saveFileDialog.ShowDialog() == true)
            {
                Directory.CreateDirectory(saveFileDialog.FileName);
                saveFileDialog.FileName = saveFileDialog.FileName.Replace(".", "");
                path = saveFileDialog.FileName + "/" + saveFileDialog.SafeFileName;

                if (postureDataList.Count > 0)
                {
                    offset = int.Parse(FrameOffset_Minimum.Text) + int.Parse(FrameOffset_Posture.Text) - 1;
                    maximumFrame = Math.Min(int.Parse(FrameOffset_Maximum.Text) + int.Parse(FrameOffset_Posture.Text), int.Parse(FrameCount_Posture.Text));

                    for (int i = offset; i < maximumFrame; i++)
                    {
                        previousXPosition = xPosition;
                        xPosition = double.Parse(postureDataList[1 + i * Constant.BODYPARTS_POSTURE + 33].Split(",")[2]) + double.Parse(postureDataList[1 + i * Constant.BODYPARTS_POSTURE + 43].Split(",")[2]) / 2.0;

                        if (i == offset + 2)
                        {
                            isLeftTurn = xPosition > previousXPosition;
                        }
                        else if (i > offset +  2)
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
                    FileOperation.WriteCSVFile(path + "_Trimmed_Posture.csv", postureDataList, offset, maximumFrame);
                    FileOperation.WriteCSVFile(TurnIndexList, path + "_Turn_", "Posture", postureDataList, int.Parse(FrameOffset_Posture.Text), maximumFrame);
                }

                if (footPressureDataList.Count > 0)
                {
                    offset = (int)Math.Round((int.Parse(FrameOffset_Minimum.Text) - 1) * double.Parse(FrameRateRatio.Text)) + int.Parse(FrameOffset_FootPressure.Text);
                    maximumFrame = Math.Min((int)Math.Round(double.Parse(FrameOffset_Maximum.Text) * double.Parse(FrameRateRatio.Text)) + int.Parse(FrameOffset_FootPressure.Text), int.Parse(FrameCount_FootPressure.Text));
                    FileOperation.WriteCSVFile(path + "_Trimmed_FootPressure.csv", footPressureDataList, offset, maximumFrame);
                    FileOperation.WriteCSVFile(TurnIndexList, path + "_Turn_", "FootPressure", footPressureDataList, int.Parse(FrameOffset_FootPressure.Text), maximumFrame, double.Parse(FrameRateRatio.Text));
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
                double feetXPosition = 0;
                double[] position;
                string[] position_str;
                MeshBuilder meshBuilder = new MeshBuilder();
                List<Tuple<string, Point3D>> pointList = new List<Tuple<string, Point3D>>();
                Point3D point;
                helixView.Children.Clear();
                for (int i = 0; i < Constant.BODYPARTS_POSTURE; i++)
                {
                    position_str = postureDataList[1 + (frameOffset_Posture + sliderValue - 1) * Constant.BODYPARTS_POSTURE + i].Split(",");
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

            if (correctedSliderValue + frameOffset_FootPressure < frameCount_FootPressure)
            {
                byte colorValue;
                string[] feets = ["Left", "Right"];
                string[] pressureValues_str = footPressureDataList[correctedSliderValue + frameOffset_FootPressure].Split(",");
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
                    await Task.Delay(10);
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
        }
    }
}
