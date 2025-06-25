using HelixToolkit.Wpf;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace DataViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        static int DIMENTIONS_POSTURE = 5;
        static int BODYPARTS_POSTURE = 51;
        static int DIMENTIONS_FOOTPRESSURE = 51;
        static int TIME_SPAN = 10;

        List<string> postureDataList = new List<string>();
        List<string> footPressureDataList = new List<string>();

        public MainWindow()
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
                        postureDataList = ReadCSVFile(filePath, extension);
                        isCorrectFormat = (postureDataList[0].Split(",").Length == DIMENTIONS_POSTURE) && ((postureDataList.Count - 1) % BODYPARTS_POSTURE == 0);
                        frameCount = postureDataList.Count / BODYPARTS_POSTURE;
                        frameRate = (int)Math.Round(1 / (double.Parse(postureDataList[BODYPARTS_POSTURE + 1].Split(",")[0]) - double.Parse(postureDataList[1].Split(",")[0])));
                    }
                    else if (clickedButton == Button_FootPressure)
                    {
                        footPressureDataList = ReadCSVFile(filePath, extension);
                        isCorrectFormat = footPressureDataList[0].Split(",").Length == DIMENTIONS_FOOTPRESSURE;
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
        /// Read a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<string> ReadCSVFile(string filePath, string extension)
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
            catch
            {
                return list;
            }
        }

        /// <summary>
        /// Convert a BVH file
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private List<string> ConvertBVHToCSV(StreamReader reader)
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
                    "l_clavicle", "l_uarm", "l_larm", "l_hand", "l_lathand", "end:l_hand", "l_medhand", "end:thorax:l",
                    "r_clavicle", "r_uarm", "r_larm", "r_hand", "r_lathand", "end:r_hand", "r_medhand", "end:thorax:r",
                    "neck", "head", "r_ear", "end:head", "l_ear", "end:head", "l_eye", "end:head", "r_eye", "end:head", "nose", "end:pelvis",
                    "l_thigh", "l_shank", "l_foot", "l_toes", "l_toe", "end:l_toes", "l_f_b_toe", "end:l_toes", "l_f_m_toe", "end:pelvis",
                    "r_thigh", "r_shank", "r_foot", "r_toes", "r_toe", "end:r_toes", "r_f_b_toe", "end:r_toes", "r_f_m_toe", "end:" ];
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (isOffsetData)
                {
                    if (line.Contains("OFFSET")){
                        offset = Array.ConvertAll(line.Split(" "), s => double.TryParse(s, out double x) ? x : 0);
                        offsetList.Add([offset[1], offset[2], offset[3]]);
                    }
                    else if (line.Contains("MOTION")){
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
                                motion[1] = [motionData[(i - endCount) * 6 + 3], motionData[(i - endCount) * 6 + 4], motionData[(i - endCount) * 5]];
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
                                if (jointNameList[i].Contains("end")) {
                                    position = MatrixOperation.Sum(MatrixOperation.Product([offsetList[i]], rotationList[i - 1]), [positionList[i - 1]])[0];
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
            return  list;
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
                else {
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
            else if (operate == "Reset"){
                if (dataName == "Minimum")
                {
                    property.SetValue(Slider, 1);
                }
                else if (dataName == "Maximum")
                {
                    property.SetValue(Slider, Math.Max(int.Parse(FrameCount_Posture.Text), (int)Math.Round(int.Parse(FrameCount_FootPressure.Text) * double.Parse(FrameRateRatio.Text))));
                }
                textBox.Text = Slider.Value.ToString();
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
            List<int> leftTurnIndexList = new List<int>();
            List<int> rightTurnIndexList = new List<int>();
            int offset;
            int maximumFrame;
            string dataName = clickedButton.Name.Split("_")[1];
            string path;

            Dictionary<string, string> extensions = new Dictionary<string, string>();
            extensions.Add("CSV", "Folder|.");
            extensions.Add("Video", "Video File (*.mp4)|*.mp4");
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save files";
            saveFileDialog.Filter = extensions[dataName];
            if (saveFileDialog.ShowDialog() == true)
            {
                if (dataName == "CSV")
                {
                    Directory.CreateDirectory(saveFileDialog.FileName);
                    saveFileDialog.FileName = saveFileDialog.FileName.Replace(".", "");
                    path = saveFileDialog.FileName + "/" + saveFileDialog.SafeFileName;

                    if (postureDataList.Count > 0)
                    {
                        using (StreamWriter writer = new StreamWriter(path + "_Trimmed_Posture.csv"))
                        {
                            offset = int.Parse(FrameOffset_Minimum.Text) + int.Parse(FrameOffset_Posture.Text) - 1;
                            maximumFrame = Math.Min(int.Parse(FrameOffset_Maximum.Text), int.Parse(FrameCount_Posture.Text));
                            writer.WriteLine(postureDataList[0]);
                            for (int i = offset; i < maximumFrame; i++)
                            {
                                previousXPosition = xPosition;
                                xPosition = 0;
                                for (int j = 0; j < BODYPARTS_POSTURE; j++)
                                {
                                    if (j == 33 || j == 43)
                                    {
                                        xPosition += double.Parse(postureDataList[1 + i * BODYPARTS_POSTURE + j].Split(",")[2]) / 2.0;
                                    }
                                    writer.WriteLine(postureDataList[1 + i * BODYPARTS_POSTURE + j]);
                                }

                                if (i == 2) {
                                    isLeftTurn = xPosition > previousXPosition;
                                }
                                else if (i > 2)
                                {
                                    if (isLeftTurn && xPosition < previousXPosition)
                                    {
                                        isLeftTurn = !isLeftTurn;
                                        leftTurnIndexList.Add(i);
                                    }
                                    else if (!isLeftTurn && xPosition > previousXPosition) {
                                        isLeftTurn = !isLeftTurn;
                                        rightTurnIndexList.Add(i);
                                    }
                                }
                            }
                        }
                        ExportAfterEveryTurn(leftTurnIndexList, path + "_LeftTurn_", "Posture", postureDataList, 5, maximumFrame);
                        ExportAfterEveryTurn(rightTurnIndexList, path + "_RightTurn_", "Posture", postureDataList, 5, maximumFrame);
                    }

                    if (footPressureDataList.Count > 0)
                    {
                        using (StreamWriter writer = new StreamWriter(path + "_Trimmed_FootPressure.csv"))
                        {
                            offset = (int)Math.Round((int.Parse(FrameOffset_Minimum.Text) - 1) * double.Parse(FrameRateRatio.Text)) + int.Parse(FrameOffset_FootPressure.Text);
                            maximumFrame = Math.Min((int)Math.Round(int.Parse(FrameOffset_Maximum.Text) * double.Parse(FrameRateRatio.Text)), int.Parse(FrameCount_FootPressure.Text));
                            writer.WriteLine(footPressureDataList[0]);
                            for (int i = offset; i < maximumFrame; i++)
                            {
                                writer.WriteLine(footPressureDataList[i]);
                            }
                        }

                        ExportAfterEveryTurn(leftTurnIndexList, path + "_LeftTurn_", "FootPressure", footPressureDataList, (int)Math.Round(5 * double.Parse(FrameRateRatio.Text)), maximumFrame);
                        ExportAfterEveryTurn(rightTurnIndexList, path + "_RightTurn_", "FootPressure", footPressureDataList, (int)Math.Round(5 * double.Parse(FrameRateRatio.Text)), maximumFrame);
                    }
                }
                else if (dataName == "Video")
                {
                    // normal   : 60sの動画で413s
                    if (postureDataList.Count > 0 || footPressureDataList.Count > 0)
                    {
                        int minimum = int.Parse(FrameOffset_Minimum.Text);
                        int maximum = int.Parse(FrameOffset_Maximum.Text);
                        Task.Run(() => ExportVideo(minimum, maximum, saveFileDialog.FileName));
                    }
                }
            }
        }

        private void ExportAfterEveryTurn(List<int> list, string path, string dataName, List<string> dataList, int margin, int maximum)
        {
            foreach (int i in list)
            {
                using (StreamWriter writer = new StreamWriter(path + (list.IndexOf(i) + 1) + "_" + dataName + ".csv"))
                {
                    writer.WriteLine(dataList[0]);
                    for (int j = Math.Max(0, i - margin); j < Math.Min(maximum, i + margin); j++)
                    {
                        if (dataName == "Posture")
                        {
                            for (int k = 0; k < BODYPARTS_POSTURE; k++)
                            {
                                writer.WriteLine(postureDataList[1 + j * BODYPARTS_POSTURE + k]);
                            }
                        }
                        else if (dataName == "FootPressure")
                        {
                            writer.WriteLine(dataList[1 + j]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Export a video file (not recommend)
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        private void ExportVideo(int minimum, int maximum, string fileName)
        {
            FrameworkElement element = DataViewer;
            double width = element.ActualWidth * 0.75;
            double height = element.ActualHeight * 0.75;
            FormatConvertedBitmap newFormatedBitmapSource;
            RenderTargetBitmap renderTargetBitmap = null;
            VideoWriter writer = new VideoWriter(fileName, FourCC.H264, 60, new OpenCvSharp.Size((int)width, (int)height));
            for (int i = minimum; i < maximum; i++)
            {
                element.Dispatcher.Invoke(() =>
                {
                    Slider.Value = i;
                    element.UpdateLayout();
                    DrawingVisual visual = new DrawingVisual();
                    using (DrawingContext context = visual.RenderOpen())
                    {
                        context.DrawRectangle(new BitmapCacheBrush(element), null, new System.Windows.Rect(0, 0, width, height));
                    }
                    renderTargetBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(visual);
                    renderTargetBitmap.Freeze();

                });

                newFormatedBitmapSource = new FormatConvertedBitmap();
                newFormatedBitmapSource.BeginInit();
                newFormatedBitmapSource.Source = renderTargetBitmap;
                newFormatedBitmapSource.DestinationFormat = PixelFormats.Bgr24;
                newFormatedBitmapSource.EndInit();

                using (Mat mat = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(newFormatedBitmapSource)) {
                    writer.Write(mat);
                }
            }
            writer.Dispose();
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
                for (int i = 0; i < BODYPARTS_POSTURE; i++)
                {
                    position_str = postureDataList[1 + (frameOffset_Posture + sliderValue - 1) * BODYPARTS_POSTURE + i].Split(",");
                    position = Array.ConvertAll(position_str, s => double.TryParse(s, out double x) ? x : 0);
                    pointList.Add(new Tuple<string, Point3D>(position_str[1] ,new Point3D(position[2], position[3], position[4])));
                    meshBuilder.AddSphere(pointList[i].Item2, 0.1);

                    if (i == 33 || i == 43)
                    {
                        feetXPosition += position[2] / 2.0;
                    }
                    if (i != 0)
                    {
                        if (pointList[i - 1].Item1.Contains("end:")) {
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
                Left_Position.Value = Math.Max(-feetXPosition,0);
                ChangeBackgroundColor("Position");

                helixView.Children.Add(new DefaultLights());
                helixView.Children.Add(new GridLinesVisual3D());
                helixView.Children.Add(new ModelVisual3D { 
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
            if (metricsName == "Position") {
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
                    await Task.Delay(TIME_SPAN);
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
            else if (clickedButton == Stop) {
                Play.IsEnabled = true;
                Pause.IsEnabled = false;
                Stop.IsEnabled = false;
                Slider.Value = 0;
            }
        }
    }
}
