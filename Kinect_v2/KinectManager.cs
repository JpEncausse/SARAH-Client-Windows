
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Controls;

using Microsoft.Kinect;



namespace net.encausse.sarah.kinect2 {
  public class KinectManager : IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static KinectManager manager = null;
    private KinectManager() { }

    public static KinectManager GetInstance() {
      if (manager == null) {
        manager = new KinectManager();
      }
      return manager;
    }

    public void Dispose() {
      if (Sensors == null) { return; }
      foreach (var sensor in Sensors.Values) {
        sensor.Dispose();
      }
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log("Kinect2", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("Kinect2", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("Kinect2", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("Kinect2", ex);
    }

    // -------------------------------------------
    //  INIT 
    // ------------------------------------------

    public Dictionary<string, Kinect> Sensors { get; set; }
    public void InitSensors() {
      
      // Cached sensors
      Sensors = new Dictionary<string, Kinect>();

      // Looking for a valid sensor 
      //var i = 0;
      //foreach (var potential in KinectSensor.KinectSensors) {
        Kinect sensor = new Kinect("Kinect_v2_0", KinectSensor.GetDefault());
        Sensors.Add("Kinect_v2_0", sensor);
      //}

      // Little warning
      if (Sensors.Count <= 0) {
        Error("No Kinect Sensor");
      }
    }

    public void StartSensors() {
      foreach (var sensor in Sensors.Values) {
        sensor.Start();
      }
    }

    public bool Ready() {
      foreach (var sensor in Sensors.Values) {
        if (!sensor.Ready()) { return false; }
      }
      return true;
    }

    // -------------------------------------------
    //  PLUGIN MANAGER 
    // -------------------------------------------

    public void InitDepthFrame(string device, ushort[] data, Timestamp state, int width, int height) {
      foreach (IAddOn addon in AddOnManager.GetInstance().AddOns) {
        if (addon is IAddOnKinect) {
          ((IAddOnKinect)addon).InitDepthFrame(device, data, state, width, height);
        }
      }
    }


    public void InitBodyFrame(string device, IList<Body> data, Timestamp stamp) {
      foreach (IAddOn addon in AddOnManager.GetInstance().AddOns) {
        if (addon is IAddOnKinect) {
          ((IAddOnKinect)addon).InitBodyFrame(device, data, stamp);
        }
      }
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public void HandleSidebar(string device, StackPanel sidebar) {
      if (Sensors.ContainsKey(device)) {
        Sensors[device].HandleSidebar(sidebar);
      }
    }

    public void RepaintColorFrame(string device, byte[] bgr, int width, int height) {
      if (Sensors.ContainsKey(device)) {
        Sensors[device].RepaintColorFrame(bgr, width, height);
      }
    }

  }
}
