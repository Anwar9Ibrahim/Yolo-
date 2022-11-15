using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alturos.Yolo;
using Alturos.Yolo.Model;
using Alturos.VideoInfo;
using OpenCvSharp;
using System.Drawing;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Configuration;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using OpenCvSharp.Extensions;
using System.IO;
using System.Diagnostics;

namespace FinalFifthYear
{
    class Yolo
    {
        string conda;
        //this line is to make it possible to use python with anaconda in our code //server
        static string strCmdText = @"C:\ProgramData\Anaconda3\Scripts\activate.bat";
        //My computer
        // string strCmdText = "C:\\Users\\user\\Anaconda3\\Scripts\\activate.bat";
        static string solutionDir = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;
        //how many vehicles have i found over all time
        public static int VehicleCount = 0;
        //Accepted Types For The model
        public List<String> AcceptedTypes;
        List<Mat> frames;
        //colors
        Dictionary<string, Scalar> colors;
        public List<Mat> detecteFrames;
        //A list of all the Found Vehicles
        public List<Vehicle> vehicles;
        //the detectedVideo Path
        string inputFilePath;
        //VideoCapture Video;
        string videoTitle;
        ///Trying to release memory
        double qurt;
        double threqurt;
        double fps;
        //YoloConfiguration
        YoloConfiguration configuration;
        public Yolo()
        {
            frames = new List<Mat>();
            detecteFrames = new List<Mat>();
            colors = new Dictionary<string, Scalar>();
            AcceptedTypes = new List<string>();
            vehicles = new List<Vehicle>();
        }
        public void SetAcceptedTypes (List<string> accepted)
        {
            AcceptedTypes = accepted;
            getColor();
        }
        public void setInputFilePath(string detectVid)
        {
            inputFilePath = detectVid;
            videoTitle = inputFilePath.Substring(inputFilePath.LastIndexOf("\\") + 1);
        }
        public void setDetectionAlgo(YoloConfiguration config, List<string> accepted)
        {
            AcceptedTypes.Clear();

            foreach( string s in accepted)
            {
                AcceptedTypes.Add(s);
            }
            configuration = config;
            
            getColor();
        }
        public void detectFile()
        {
            vehicles.Clear();
            using (var yoloWrapper = new YoloWrapper(configuration)) 
            {
                var i = 0;
                using (var video = new VideoCapture(inputFilePath))
                {
                    qurt = video.FrameHeight * 0.30;
                    threqurt = video.FrameHeight * 0.40;
                    var yoloTracking = new YoloTracking(video.FrameWidth, video.FrameHeight);
                    fps = video.Fps;
                    while (video.Grab())
                    {
                        using (var img = video.RetrieveMat())
                        {
                            if (img != null)
                            {
                                try
                                {
                                    //convert from Mat to byte Array because yolo needs this type to work
                                    byte[] imageData = img.ToBytes();
                                    //detecting will give us yoloitems
                                    var items = yoloWrapper.Detect(imageData)
                                        .Where(v => AcceptedTypes.Any(s => (v.Type).Equals(s))).ToArray();
                                    //tracking will give us yoloTrackingitem this means that we
                                    //also have an objectId  assosiated with each object 
                                    var trackingItems = yoloTracking.Analyse(items);
                                    CountVehicles(img, trackingItems, i);
                                    //var editedImage = drawRectangles(img, items);
                                    var editedImage = drawRectangles_(img, trackingItems);
                                    //this way it will start showing the results faster
                                    Cv2.ImShow("image2", editedImage);
                                    Cv2.WaitKey(1);
                                    editedImage.Dispose();
                                }
                                catch
                                {
                                    var st = "frame error";
                                }
                            }
                            i++;
                        }
                    }
                    Cv2.DestroyAllWindows();
                }
            }
        }
        private Mat drawRectangles_(Mat frame, IEnumerable<YoloTrackingItem> items)
        {
            //draw the lines
            double qurt = frame.Height * 0.50;
            double threqurt = frame.Height * 0.60;
            string counterString = "Objects being tracked: " + vehicles.Count.ToString() + " ";
            Cv2.Line(frame, new OpenCvSharp.Point(0.0, qurt), new OpenCvSharp.Point(frame.Width, qurt), Scalar.Blue, 2);
            Cv2.Line(frame, new OpenCvSharp.Point(0.0, threqurt), new OpenCvSharp.Point(frame.Width, threqurt), Scalar.Red, 2);
            frame.Rectangle(new Rect(5, 5, counterString.Length * 18, 40), Scalar.Green, -1);
            Cv2.PutText(frame, "Objects being tracked: " + vehicles.Count.ToString() + " ", new OpenCvSharp.Point(5, 35), HersheyFonts.HersheySimplex, 1, Scalar.White, 2);
            //draw the rectangles to surround each object
            foreach (YoloTrackingItem item in items)
            {
                //find a way to color each object type with some color
                OpenCvSharp.Rect rect = new OpenCvSharp.Rect(item.X, item.Y, item.Width, item.Height);
                string addedString = item.Type + "  " + item.Confidence.ToString("0.00");
                frame.Rectangle(new Rect(item.X, item.Y - 10, addedString.Length * 9, 15), colors[item.Type], -1);
                frame.PutText(addedString, new OpenCvSharp.Point(item.X, item.Y), HersheyFonts.HersheySimplex, 0.5, Scalar.White);
                frame.Rectangle(rect, colors[item.Type], 2);
            }
            return frame;
        }
        private void CountVehicles(Mat frame, IEnumerable<YoloTrackingItem> items, int i)
        {
            var qurt = frame.Height * 0.50;
            var threqurt = frame.Height * 0.60;
            int tempCount = 0;

            foreach (YoloTrackingItem item in items)
            {
                Vehicle vehicle = new Vehicle(item, i);
                var exist = vehicles.Any(v => v.TrackingItem.ObjectId == item.ObjectId);
                if (!exist)
                {
                    if (item.Y < qurt && (item.Y + item.Height) > qurt)
                    {
                        Bitmap croppedImage = CropImage(frame.ToBitmap(), new Rectangle(item.X, item.Y, item.Width, item.Height));
                        vehicle.ImagePath = string.Format(solutionDir+"\\images\\{0}_item_{1}.bmp", videoTitle, (i + tempCount)* DateTime.Now.Second);
                        try
                        {
                            croppedImage.Save(vehicle.ImagePath);
                        }
                        catch
                        {
                            var excepion = "saveFile Error";
                        }
                        tempCount++;
                        vehicle.visiting = true;
                        vehicle.InFrame = i;
                        vehicle.videoTitle = videoTitle;
                        vehicles.Add(vehicle);
                    }

                }
                else
                {
                    var foundV = vehicles.Find(v => v.TrackingItem.ObjectId == item.ObjectId);
                    foundV.TrackingItem = item;
                    if (foundV.TrackingItem.Y < threqurt && (foundV.TrackingItem.Y + foundV.TrackingItem.Height) > threqurt && foundV.visiting == true && foundV.Seen == false)
                    {
                        foundV.visiting = false;
                        foundV.Seen = true;
                        foundV.OutFrame = i;
                        foundV.Speed = calculateSpeed(foundV);
                    }
                }

            }
        }
        private double calculateSpeed(Vehicle vehicle)
        {

            var speed = 0.0;
            if (vehicle.OutFrame != 0)
            {
                var time = (vehicle.OutFrame - vehicle.InFrame) / fps;//calculate time num_of_frames/frame rate
                var distance = 5;//the distance between the two drawn lines is almost  5 meters
                speed =(distance / time) * (10.0/ 36);//calculated by m/s   
            }
            return speed;
        }
        public void AddVehiclesToDB()
        {

            foreach (Vehicle vehicle in vehicles)
            {
                try
                {
                    string connetionString = null;
                    SqlConnection cnn;
                    connetionString = "Server=.;Database=VehicleCounter;User id=sa; Password=123;";
                    cnn = new SqlConnection(connetionString);
                    cnn.Open();

                    SqlCommand command;
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    String sql = "";

                    sql = "Insert into Vehicle (Id,CreationDate,VehicleType,Speed,Status,ImageURL,VideoTitle,Color) values(@Id,@CreationDate, @VehicleType, @Speed, @Status,@ImagePath,@VideoTitle,@Color )";

                    command = new SqlCommand(sql, cnn);
                    //add with Value
                    command.Parameters.AddWithValue("@Id", vehicle.Id);
                    command.Parameters.AddWithValue("@CreationDate", vehicle.CreationDate);
                    command.Parameters.AddWithValue("@VehicleType", vehicle.TrackingItem.Type);
                    command.Parameters.AddWithValue("@Speed", vehicle.Speed);
                    command.Parameters.AddWithValue("@Status", vehicle.Status);
                    command.Parameters.AddWithValue("@ImagePath", vehicle.ImagePath);
                    command.Parameters.AddWithValue("@VideoTitle", vehicle.videoTitle);
                    command.Parameters.AddWithValue("@Color", "InitialValue");

                    command.ExecuteNonQuery();
                    //adapter.InsertCommand = new SqlCommand(sql, cnn);
                    //adapter.InsertCommand.ExecuteNonQuery();

                    command.Dispose();
                    cnn.Close();
                }
                catch
                {
                    String d = "db Error";
                }

            }
        }
        public Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }
         public void CallProcess()
         { 
             var workingDirectory = Path.GetFullPath(@"C:\Users\anwar\yolov4-deepsort");
             var process = new Process
             {
                 StartInfo = new ProcessStartInfo
                 {
                     FileName = "cmd.exe",
                     RedirectStandardInput = true,
                     UseShellExecute = false,
                     RedirectStandardOutput = true,
                     WorkingDirectory = workingDirectory,
                     CreateNoWindow = true
                 }
             };
             process.Start();
             // Pass multiple commands to cmd.exe
             using (var sw = process.StandardInput)
             {
                 if (sw.BaseStream.CanWrite)
                 {
                     // Vital to activate Anaconda
                     sw.WriteLine(strCmdText);
                     sw.WriteLine("activate yolov4-gpu");
                     // run your script. You can also pass in arguments
                     string outputvideo = @"./ outputs / demo.avi";
                    // inputFilePath = @"./data/video/cars.mp4";
                     string commad = string.Format("python object_tracker.py --video {0} --output {1} --model {2} --info --count", inputFilePath, outputvideo, "yolov4");
                     sw.WriteLine(commad);
                     sw.WriteLine("exit");
                     //process.WaitForExit(600000);
                     while (!process.StandardOutput.EndOfStream)
                     {
                         var line = process.StandardOutput.ReadLine();
                         conda += line + "\r\n";
                     }
                     conda += "end";
                     // 
                     // process.WaitForExit();
                 }
                 else
                 {
                     Debug.WriteLine("Error occured");
                 }
             }
         }
        private void getColor()
        {
            colors.Clear();
            for (int i = 0; i < AcceptedTypes.Count; i++)
            {
                colors.Add(AcceptedTypes[i], Scalar.RandomColor());
            }
        }
    }
}
