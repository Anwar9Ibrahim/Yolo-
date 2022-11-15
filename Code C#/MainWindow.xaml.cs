using System;
using Microsoft.Win32;
using System.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Media.Imaging;
//not important
using System.Linq.Expressions;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Alturos.Yolo;
using Alturos.Yolo.Model;
using Emgu.CV.UI;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace FinalFifthYear
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        string detectedVid;
        DataTable dtViewVehicles;
        private Yolo detector;
        //the server
        static string ConnetionString = "Server=.;Database=VehicleCounter;User id=sa; Password=123;";
        public List<string> AcceptedTypesF;
        //My comp
        //static string ConnetionString = "Server=.;Database=VehicleCounter;User id=sa; Password=123456789;";
        public MainWindow()
        {
            InitializeComponent();
            detector = new Yolo();
            AcceptedTypesF = new List<string>();
            string[] accepted_4 = { "bicycle", "car", "motorbike", "bus", "truck" };
            setAccepted(accepted_4);
            detector.setDetectionAlgo(new YoloConfiguration("yolov4-o.cfg", "yolov4-o_final.weights", "o.names"),AcceptedTypesF);
           // detector.SetAcceptedTypes(AcceptedTypes);
            dtViewVehicles = new DataTable();
            defineDataTable();
        }
        /// <summary>
        /// this fuction will choosing the input video, then it will use yolo wrapper to detect vehicles
        /// and add the detected Vehicles to the Database.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BDetect_Click(object sender, RoutedEventArgs e)
        {
            GDGVandFilters.Visibility = Visibility.Hidden;
            CLabel.Visibility = Visibility.Visible;
            //choosing the input video
            Lpath.Text = "Processing Video Please Wait...";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video files (*.mpg; *.mpeg; *.avi; *.mp4)| *.mpg; *.mpeg; *.avi; *.mp4";
            if (openFileDialog.ShowDialog() == true)
            {
                detectedVid = openFileDialog.FileName;
                //define the weights, all the data set cinfigurations
                detector.setInputFilePath(detectedVid);
                //this is for SORT Algorithm
                //detector.CallProcess();        
                //starting the detection 
                MessageBox.Show("Processing Video Please Wait...");
                //start detection and tracking this will give us frames with the rectangles drawn on them
                detector.detectFile();
                //add Vehicles to database
                detector.AddVehiclesToDB();
                Lpath.Text = "We were able to track " + detector.vehicles.Count + " vehicles devided as the following: \n";
                for (int i = AcceptedTypesF.Count - 1; i >= 0; i--)
                {
                    var counter = detector.vehicles.FindAll(v => v.TrackingItem.Type == AcceptedTypesF[i]).Count;
                    if (counter != 0)
                    {
                        Lpath.Text += "We found " + counter.ToString() + "   " + AcceptedTypesF[i] + " \n";
                    }
                }
                Lpath.Text += "please click on detected Vehicles for more details";
            }
            else
            {
                Lpath.Text = "No Video was Selected...";
            }
        }
        /// <summary>
        /// the following function will connect with the database and 
        /// get all the vehicles from it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BDatabaseVehicles_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection cnn;
            DataTable dTVehicles;
            dtViewVehicles.Clear();
            try
            {
                cnn = new SqlConnection(ConnetionString);
                cnn.Open();
                cnn.Close();
                try
                {
                    SqlCommand command;
                    String sql = " ";
                    sql = "Select * from Vehicle";
                    command = new SqlCommand(sql, cnn);
                    SqlDataAdapter sda = new SqlDataAdapter(command);
                    dTVehicles = new DataTable("emp");
                    sda.Fill(dTVehicles);
                    if (dTVehicles.Rows.Count != 0)
                    {
                        CLabel.Visibility = Visibility.Hidden;
                        GDGVandFilters.Visibility = Visibility.Visible;
                        //View Data get all vehices from dataBase

                        for (int i = 0; i < dTVehicles.Rows.Count; i++)
                        {
                            DataRow dataRow = dtViewVehicles.NewRow();
                            dataRow[0] = dTVehicles.Rows[i].Field<string>(dTVehicles.Columns[2]);
                            dataRow[1] = dTVehicles.Rows[i].Field<DateTime>(dTVehicles.Columns[1]);
                            dataRow[2] = dTVehicles.Rows[i].Field<Double>(dTVehicles.Columns[3]);
                            dataRow[3] = dTVehicles.Rows[i].Field<string>(dTVehicles.Columns[6]);
                            dataRow[4] = dTVehicles.Rows[i].Field<string>(dTVehicles.Columns[7]);
                            BitmapImage img = new BitmapImage();
                            string tem = dTVehicles.Rows[i].Field<string>(dTVehicles.Columns[5]);
                            Uri uri = new Uri(tem);
                            img = new System.Windows.Media.Imaging.BitmapImage(uri);
                            dataRow[5] = img;
                            dtViewVehicles.Rows.Add(dataRow);
                        }
                        DGVehiclesFromDB.ItemsSource = dtViewVehicles.DefaultView;
                    }
                    else
                    {
                        Lpath.Text = "Database Has No Vehicles Yet..";
                    }
                }

                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// the following function will View the detected vehicles from 
        /// the current Vedio.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrVehicles_Click(object sender, RoutedEventArgs e)
        {
            dtViewVehicles.Clear();
            if (detector.vehicles.Count == 0)
            {
                CLabel.Visibility = Visibility.Visible;
                GDGVandFilters.Visibility = Visibility.Hidden;
                Lpath.Text = "No video is selected yet, or selected Video has no vehicles";
                MessageBox.Show("No vehicles to Show, Please try another video");
            }
            else
            {
                CLabel.Visibility = Visibility.Hidden;
                GDGVandFilters.Visibility = Visibility.Visible;
                foreach (Vehicle vehicle in detector.vehicles)
                {
                    DataRow dataRow = dtViewVehicles.NewRow();
                    dataRow[0] = vehicle.TrackingItem.Type;
                    dataRow[1] = DateTime.Now;
                    dataRow[2] = vehicle.Speed;
                    dataRow[3] = vehicle.videoTitle;
                    dataRow[4] = "InitialValue";//dTVehicles.Rows[i].Field<string>(dTVehicles.Columns[7]);
                    BitmapImage img = new BitmapImage();
                    string tem = vehicle.ImagePath;
                    Uri uri = new Uri(tem);
                    img = new System.Windows.Media.Imaging.BitmapImage(uri);
                    dataRow[5] = img;
                    dtViewVehicles.Rows.Add(dataRow);
                }
                DGVehiclesFromDB.ItemsSource = dtViewVehicles.DefaultView;
            }
        }
        /// <summary>
        /// the following two functions will handle the power button and the 
        /// minimize button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BPower_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void BMinus_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CBDetectionAlgo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //String Algo = CBDetectionAlgo.SelectedItem.ToString();
            switch (CBDetectionAlgo.SelectedItem.ToString())
            {
                case "System.Windows.Controls.ComboBoxItem: YoloV4":
                    string[] accepted_O = { "bicycle", "car", "van", "truck", "tricycle", "awning-tricycle", "bus", "motor" };
                    setAccepted(accepted_O);
                    detector.setDetectionAlgo(new YoloConfiguration("yolov4-o.cfg", "yolov4-o_final.weights", "o.names"), AcceptedTypesF);
                    //var config = new YoloConfiguration("yolov4.cfg", "yolov4.weights", "coco.names");
                    break;
                case "System.Windows.Controls.ComboBoxItem: YoloV3":
                    string[] accepted_3 = { "Ambulance", "Bus", "Car", "Motorcycle", "Truck" };
                    setAccepted(accepted_3);
                    detector.setDetectionAlgo(new YoloConfiguration("yolov3-custom.cfg", "yolov3-custom_best.weights", "obj.names"), AcceptedTypesF);
                    break;
                case "System.Windows.Controls.ComboBoxItem: YoloV2":
                    string[] accepted_2 = { "bicycle", "bus", "car" };
                    setAccepted(accepted_2);
                    detector.setDetectionAlgo(new YoloConfiguration("yolov2-tiny-voc.cfg", "yolov2-tiny-voc.weights", "voc.names"), AcceptedTypesF);
                    break;
                default:
                    //YoloV4 Original
                    string[] accepted_4 = { "bicycle", "car", "motorbike", "bus", "truck" };
                    setAccepted(accepted_4);
                    detector.setDetectionAlgo(new YoloConfiguration("yolov4.cfg", "yolov4.weights", "coco.names"), AcceptedTypesF);
                    break;
            }
            CLabel.Visibility = Visibility.Visible;
            GDGVandFilters.Visibility = Visibility.Hidden;
            Lpath.Text = "Detection Model is" + CBDetectionAlgo.SelectedItem.ToString().Substring(CBDetectionAlgo.SelectedItem.ToString().IndexOf(":") + 1)+ "choose Video To Start Detections";


        }

        private void FilterVehicles_Click(object sender, RoutedEventArgs e)
        {
            DateTime start = DTstart.Value ?? DateTime.Now;
            DateTime finish = DTfinish.Value ?? DateTime.Now;
            bool t = false;
            if (finish > start)
            {
                DataTable filtered;
                try
                {
                    filtered = dtViewVehicles.AsEnumerable().Where(row => DateTime.ParseExact(row.Field<string>("Creation date"), "M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture) >= start
                     && DateTime.ParseExact(row.Field<string>("Creation date"), "M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture) <= finish).CopyToDataTable();
                    dtViewVehicles.Clear();
                    if (filtered.Rows.Count != 0)
                    {
                        for (int i = 0; i < filtered.Rows.Count; i++)
                        {
                            DataRow dataRow = dtViewVehicles.NewRow();
                            dataRow[0] = filtered.Rows[i].Field<string>(filtered.Columns[0]);
                            dataRow[1] = filtered.Rows[i].Field<string>(filtered.Columns[1]); //DateTime.ParseExact(filtered.Rows[i].Field<string>(filtered.Columns[1]), "M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
                            dataRow[2] = filtered.Rows[i].Field<string>(filtered.Columns[2]);
                            dataRow[3] = filtered.Rows[i].Field<string>(filtered.Columns[3]);
                            dataRow[4] = filtered.Rows[i].Field<string>(filtered.Columns[4]);
                            dataRow[5] = filtered.Rows[i].Field<BitmapImage>(filtered.Columns[5]);
                            dtViewVehicles.Rows.Add(dataRow);
                        }
                        DGVehiclesFromDB.ItemsSource = dtViewVehicles.DefaultView;
                    }
                    else
                    {
                        CLabel.Visibility = Visibility.Visible;
                        GDGVandFilters.Visibility = Visibility.Hidden;
                        Lpath.Text = "No Vehicles Matches your search.. ";//To filter from...";
                    }
                }
                catch(Exception exceptio)
                {
                    CLabel.Visibility = Visibility.Visible;
                    GDGVandFilters.Visibility = Visibility.Hidden;
                    Lpath.Text = "No Vehicles Matches your search..";
                    MessageBox.Show(exceptio.Message);
                }
                
            }
            else
            {
                CLabel.Visibility = Visibility.Visible;
                GDGVandFilters.Visibility = Visibility.Hidden;
                Lpath.Text = "the Date and Time picked are not Valid";
            }

        }
        private void defineDataTable ()
        {            
            dtViewVehicles.Columns.Add("Vehicle Type");
            dtViewVehicles.Columns.Add("Creation date");
            dtViewVehicles.Columns.Add("Speed");
            dtViewVehicles.Columns.Add("Video Title");
            dtViewVehicles.Columns.Add("Color");
            dtViewVehicles.Columns.Add("Image", typeof(BitmapImage));
        }
        private void setAccepted( string[] accepted)
        {
            AcceptedTypesF.Clear();
            for( int i =0; i< accepted.Length; i++)
            {
                AcceptedTypesF.Add(accepted[i]);
            }

        }

    }
}