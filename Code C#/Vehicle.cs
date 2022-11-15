using System;
using System.Collections.Generic;
using System.Text;
using Alturos.Yolo.Model;
using OpenCvSharp;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace FinalFifthYear
{
    public class Vehicle
    {
        //public string Id;
        public YoloTrackingItem TrackingItem;
        public bool Seen;
        public bool visiting;
        public int InFrame;
        public int OutFrame;
        public double Speed;
        public string ImagePath;
        public string videoTitle;
        public Guid Id { get; set; }
        public DateTime CreationDate { get; set; }
        public MyEnums.status Status { get; set; }
        public Vehicle(YoloTrackingItem item, int inFrame,int outFrame=0 )
        {
            Id = Guid.NewGuid();
            TrackingItem = item;
            visiting = false;
            InFrame = inFrame;
            Seen = false;
            Status = MyEnums.status.Active;
            CreationDate = DateTime.Now;
        }
    }
}
