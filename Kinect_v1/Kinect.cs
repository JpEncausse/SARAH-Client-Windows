using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

using Microsoft.Kinect;

namespace net.encausse.sarah.kinect1 {
  public class Kinect : IDisposable {

    public String Name { get; set; }
    private KinectSensor Sensor { get; set; }
    public MotionTask Task { get; set; }

    public Kinect(String name, KinectSensor sensor) { 
      this.Sensor = sensor;
      this.Name = name;
    }

    public void Dispose() {

      if (source != null) {
        Log("Dispose audio source");
        source.Stop();
      }

      if (Task != null) {
        Task.Dispose();
        Task = null;
      }

      if (Sensor != null) {
        Sensor.Dispose();
        Sensor = null;
      }
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log(Name, msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug(Name, msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error(Name, msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error(Name, ex);
    }

    // -------------------------------------------
    //  START
    // ------------------------------------------

    public void Start() {
      if (Sensor == null) { return; }

      InitProperties();

      if (!ConfigManager.GetInstance().Find("kinect_v1.speech.speech_only", true)) {

        // Motion Task
        Task = new MotionTask(Name);

        // Config Gesture
        BeforeKinectStartGesture(Sensor);

        // Config Color
        BeforeKinectStartColor(Sensor);

        // Start all Frames
        Sensor.AllFramesReady += KinectSensorOnAllFramesReady;
      }
      
      // Start Sensor
      Log("Start sensor");
      try { Sensor.Start(); } catch (IOException) {
        Error("No Kinect Sensor: already used");
        Sensor = null;  // Some other application is streaming from the same Kinect sensor
        return;
      }

      // Start Audio Stream
      InitSpeechEngine(Sensor);

      // Elevation angle +/- 27
      Sensor.ElevationAngle = ConfigManager.GetInstance().Find("kinect_v1.elevation", 0);
    }

    public bool Ready() {
      return init;
    }

    // -------------------------------------------
    //  SPEECH
    // -------------------------------------------

    KinectAudioSource source = null;
    protected void InitSpeechEngine(KinectSensor Sensor) {

      source = Sensor.AudioSource;
      source.EchoCancellationMode = EchoCancellationMode.CancellationAndSuppression;
      source.NoiseSuppression = true;
      source.BeamAngleMode = BeamAngleMode.Adaptive; // set the beam to adapt to the surrounding
      source.AutomaticGainControlEnabled = false;

      var echo = ConfigManager.GetInstance().Find("kinect_v1.speech.echo", -1);
      if (echo >= 0) { source.EchoCancellationSpeakerIndex = echo; }

      Log("AutomaticGainControlEnabled : " + source.AutomaticGainControlEnabled);
      Log("BeamAngle : " + source.BeamAngle);
      Log("EchoCancellationMode : " + source.EchoCancellationMode);
      Log("EchoCancellationSpeakerIndex : " + source.EchoCancellationSpeakerIndex);
      Log("NoiseSuppression : " + source.NoiseSuppression);
      Log("SoundSourceAngle : " + source.SoundSourceAngle);
      Log("SoundSourceAngleConfidence : " + source.SoundSourceAngleConfidence);

      var stream = source.Start();
      double confidence = ConfigManager.GetInstance().Find("kinect_v1.speech.confidence", 0.6);
      AddOnManager.GetInstance().AddAudioSource(Name, stream, "Kinect", null, confidence);
    }

    // -------------------------------------------
    //  GESTURE
    // ------------------------------------------

    protected void BeforeKinectStartGesture(KinectSensor Sensor) {

      if (!ConfigManager.GetInstance().Find("kinect_v1.skeleton.enable", false)) { return; }
      if (ConfigManager.GetInstance().Find("kinect_v1.skeleton.seated", false)) {
        Sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
      }

      TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
      smoothingParam.Smoothing = 0.5f;
      smoothingParam.Correction = 0.5f;
      smoothingParam.Prediction = 0.5f;
      smoothingParam.JitterRadius = 0.05f;
      smoothingParam.MaxDeviationRadius = 0.04f;

      Sensor.SkeletonStream.Enable(smoothingParam);
    }

    // -------------------------------------------
    //  COLOR / DEPTH
    // ------------------------------------------

    protected void BeforeKinectStartColor(KinectSensor Sensor) {

      if (!ConfigManager.GetInstance().Find("kinect_v1.color.enable", false)) { return; }

      Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
      Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
    }

    // -------------------------------------------
    //  FRAMEREADY (Gesture + Color + IR)
    // -------------------------------------------

    private int FPS = 12;
    private int FPSGlobal   = 0;
    private int FPSColor    = 0;
    private int FPSDepth    = 0;
    private int FPSSkeleton = 0;
    private int FPSJoints   = 0;

    protected void InitProperties() {
      FPSGlobal = FPS / ConfigManager.GetInstance().Find("kinect_v1.fps", FPS);
      FPSColor = FPS / ConfigManager.GetInstance().Find("kinect_v1.color.fps", FPS);
      FPSDepth = FPS / ConfigManager.GetInstance().Find("kinect_v1.depth.fps", FPS);
      FPSSkeleton = FPS / ConfigManager.GetInstance().Find("kinect_v1.skeleton.fps", FPS);
      FPSJoints = FPS / ConfigManager.GetInstance().Find("kinect_v1.skeleton.joints", FPS);

      Log("FPSGlobal: " + FPSGlobal + " (" + (FPS / FPSGlobal) + ")");
      Log("FPSColor: " + FPSColor + " (" + (FPS / FPSColor) + ")");
      Log("FPSDepth: " + FPSDepth + " (" + (FPS / FPSDepth) + ")");
      Log("FPSSkeleton: " + FPSSkeleton + " (" + (FPS / FPSSkeleton) + ")");
      Log("FPSJoints: " + FPSJoints + " (" + (FPS / FPSJoints) + ")");
    }

    // Skeleton
    public Skeleton[] Skeletons { get; set; }
    public Timestamp SkeletonStamp { get; set; }
    public List<NBody> BodyData { get; set; }

    // Color
    public byte[] ColorDataRaw { get; set; }
    public int ColorWidth, ColorHeight;
    public ColorImageFormat ColorFormat { get; set; }
    public Timestamp ColorStamp { get; set; }
    // public ColorImagePoint[] ColorCoordinates { get; set; }
    // public DepthImagePoint[] DepthCoordinates { get; set; }

    // Depth
    public short[] DepthData { get; set; }
    public int DepthWidth, DepthHeight, DepthMin, DepthMax;
    public DepthImagePixel[] DepthPixels { get; set; }
    public DepthImageFormat DepthFormat { get; set; }
    public Timestamp DepthStamp { get; set; }

    // Fps
    private int fpsGlobal   = 0;
    private int fpsColor    = 0;
    private int fpsDepth    = 0;
    private int fpsSkeleton = 0;
    private int fpsJoints   = 0;

    public StopwatchAvg AllFrameWatch = new StopwatchAvg();

    private void KinectSensorOnAllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs) {

      if (++fpsGlobal < FPSGlobal) { return; } fpsGlobal = 0;

      using (var depthFrame    = allFramesReadyEventArgs.OpenDepthImageFrame())
      using (var colorFrame    = allFramesReadyEventArgs.OpenColorImageFrame())
      using (var skeletonFrame = allFramesReadyEventArgs.OpenSkeletonFrame()) {

        if (null == depthFrame || null == colorFrame || null == skeletonFrame) {
          return;
        }

        AllFrameWatch.Again();

        // Init ONCE with provided data
        InitFrames(depthFrame, colorFrame, skeletonFrame);

        // Backup frames (motion)
        depthFrame.CopyPixelDataTo(this.DepthData);

        // Motion check
        if (Task == null || Task.StandBy) {
          AllFrameWatch.Stop();
          return;
        }

        // Copy computed depth
        if (++fpsDepth >= FPSDepth) {
          fpsDepth = 0;
          depthFrame.CopyDepthImagePixelDataTo(this.DepthPixels);
          DepthStamp.Time = System.DateTime.Now;
        }

        // Copy color data
        if (++fpsColor >= FPSColor) { fpsColor = 0;
          colorFrame.CopyPixelDataTo(ColorDataRaw);
          
          
          // Remove transparency
          // for (int i = 0; i < this.ColorData.Length; i += 4) { this.ColorData[i] = 255; }

          // Downgrade to bgr
          // for (int i3, i4, i = 0; i < this.ColorDataRaw.Length / 4; i++) {
          //  i3 = i * 3; i4 = i * 4;
          //  ColorData[i3 + 0] = ColorDataRaw[i4 + 0];
          //  ColorData[i3 + 1] = ColorDataRaw[i4 + 1];
          //  ColorData[i3 + 2] = ColorDataRaw[i4 + 2];
          // }

          // Map color coordinate
          // Sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthFormat, DepthPixels, ColorFormat, ColorCoordinates);
          // Sensor.CoordinateMapper.MapColorFrameToDepthFrame(ColorFormat, DepthFormat, DepthPixels, DepthCoordinates);

          // Clean with DepthFrame (experimental)
          // if (Task.NoMotion != null) {
            // Task.MaskDepth(DepthData, ColorData);
          // }

          ColorStamp.Time = System.DateTime.Now;
        }

        // Copy skeleton data
        if (++fpsSkeleton >= FPSSkeleton && this.Skeletons != null) { fpsSkeleton = 0;
          skeletonFrame.CopySkeletonDataTo(this.Skeletons);
          SkeletonStamp.Time = System.DateTime.Now;
        }

        // Convert Joint 3D to 2D on 1st skeleton
        if (++fpsJoints >= FPSJoints && BodyData != null) { 
          fpsJoints = 0;
          RefreshBodyData(Skeletons);
        }

        AllFrameWatch.Stop();
      }
    }

    // -------------------------------------------
    //  INIT FRAME
    // -------------------------------------------

    private bool init = false;
    private void InitFrames(DepthImageFrame depthFrame, ColorImageFrame colorFrame, SkeletonFrame skeletonFrame) {
      if (init) { return; } init = true;

      // Depth Frame
      if (null == DepthPixels) {
        DepthFormat = depthFrame.Format;
        DepthStamp = new Timestamp();
        DepthWidth = depthFrame.Width;
        DepthHeight = depthFrame.Height;
        DepthMin = depthFrame.MinDepth;
        DepthMax = depthFrame.MaxDepth;
        DepthData = new short[depthFrame.PixelDataLength];
        DepthPixels = new DepthImagePixel[depthFrame.PixelDataLength];

        var dueTime  = TimeSpan.FromMilliseconds(200);
        var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("kinect_v1.motion.ms", 100));
        Task.SetDepth(DepthData, null, DepthWidth, DepthHeight);
        Task.Start(dueTime, interval);

        KinectManager.GetInstance().InitDepthFrame(Name, DepthData, DepthStamp, DepthWidth, DepthHeight, DepthMin, DepthMax);
        KinectManager.GetInstance().InitDepthFrame(Name, DepthPixels, DepthData, DepthStamp, DepthWidth, DepthHeight, DepthMin, DepthMax);
      }

      // Color Frame
      if (null == ColorDataRaw) {
        ColorFormat = colorFrame.Format;
        ColorDataRaw = new byte[colorFrame.PixelDataLength];
        ColorWidth = colorFrame.Width;
        ColorHeight = colorFrame.Height;
        ColorStamp = new Timestamp();
        // ColorCoordinates  = new ColorImagePoint[DepthPixels.Length];
        // DepthCoordinates = new DepthImagePoint[ColorWidth*ColorHeight];

        AddOnManager.GetInstance().InitColorFrame(Name, ColorDataRaw, ColorStamp, ColorWidth, ColorHeight, 12);
      }

      // Skeleton Frame
      if (null == Skeletons) {
        SkeletonStamp = new Timestamp();
        Skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
        BodyData = new List<NBody>(skeletonFrame.SkeletonArrayLength);
        KinectManager.GetInstance().InitSkeletonFrame(Name, Skeletons, SkeletonStamp, ColorWidth, ColorHeight);
        AddOnManager.GetInstance().InitBodyFrame(Name, BodyData, SkeletonStamp, ColorWidth, ColorHeight);
      }
    }

    // -------------------------------------------
    //  INTERNAL BODY
    // -------------------------------------------

    private ICollection<NBody> cache = new List<NBody>();
    private void RefreshBodyData(IList<Skeleton> skeletons) {
      cache.Clear();
      foreach (var skeleton in skeletons) {
        if (skeleton.TrackingState != SkeletonTrackingState.Tracked) { continue; }
        RefreshBodyData(skeleton);
      }
      lock (BodyData) {
        BodyData.Clear();
        BodyData.AddRange(cache);
      }
    }

    private void RefreshBodyData(Skeleton skeleton) {
      foreach (var nbody in BodyData) {
        if ((int) nbody.TrackingId != skeleton.TrackingId) { continue; }
        RefreshBodyData(skeleton, nbody); return;
      }
      RefreshBodyData(skeleton, new NBody(skeleton.TrackingId));
    }

    private void RefreshBodyData(Skeleton skeleton, NBody nbody) {
      cache.Add(nbody);

      // Joints
      foreach (Joint joint in skeleton.Joints) {
        var ntype = ResolveJointType(joint.JointType);
        var njoint = nbody.GetJoint(ntype);
        var point = Sensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorFormat);

        njoint.Tracking = ResolveTrackingState(joint.TrackingState);
        njoint.SetPosition3D(joint.Position.X, joint.Position.Y, joint.Position.Z);
        njoint.SetPosition2D(point.X, point.Y);

        if (  njoint.Type == NJointType.Head
           || njoint.Type == NJointType.HandRight
           || njoint.Type == NJointType.HandLeft) {
          njoint.SetJointRadius(ComputeJointRadius(joint));
        }
      }

      // Misc
      nbody.Tracking = NTrackingState.Tracked;
    }

    private int ComputeJointRadius(Joint joint) {
      if (joint.TrackingState == JointTrackingState.NotTracked) { return 0; }
      var pos = Sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, DepthFormat);
      var x0 = (int)pos.X; var y0 = (int)pos.Y;
      if (x0 + y0 * DepthWidth > DepthData.Length) return 0;
      var depth0 = DepthData[x0 + y0 * DepthWidth];

      var x = ComputeJointRadius(x0, y0, depth0, DepthWidth, true);
      var y = ComputeJointRadius(x0, y0, depth0, DepthWidth, false);

      return (x0 - x) > (y0 - y) ? (x0 - x) * ColorWidth / DepthWidth
                                 : (y0 - y) * ColorHeight / DepthHeight;
    }

    private int ComputeJointRadius(int x0, int y0, int depth0, int width, bool onX) {
      var k0 = onX ? x0 : y0;
      var i = k0;
      for (var cpt = 0; i > 0; i--) {
        var depthI = onX ? DepthData[i + y0 * DepthWidth]
                         : DepthData[x0 + i * DepthWidth];
        cpt = depth0 - depthI > 200 ? cpt + 1 : 0;
        if (cpt >= 3) { return i; }
        if (k0 - i > 100) { return k0; }
      }
      return k0;
    }


    private NTrackingState ResolveTrackingState(JointTrackingState state) {
      switch (state) {
        case JointTrackingState.Tracked: return NTrackingState.Tracked;
        case JointTrackingState.Inferred: return NTrackingState.Inferred;
        case JointTrackingState.NotTracked: return NTrackingState.NotTracked;
      }
      return NTrackingState.NotTracked;
    }

    private NJointType ResolveJointType(JointType type) {
      switch (type) {

        case JointType.Head: return NJointType.Head;
        case JointType.ShoulderLeft: return NJointType.ShoulderLeft;
        case JointType.ElbowLeft: return NJointType.ElbowLeft;
        case JointType.WristLeft: return NJointType.WristLeft;
        case JointType.HandLeft: return NJointType.HandLeft;
        case JointType.ShoulderRight: return NJointType.ShoulderRight;
        case JointType.ElbowRight: return NJointType.ElbowRight;
        case JointType.WristRight: return NJointType.WristRight;
        case JointType.HandRight: return NJointType.HandRight;
        case JointType.HipLeft: return NJointType.HipLeft;
        case JointType.KneeLeft: return NJointType.KneeLeft;
        case JointType.AnkleLeft: return NJointType.AnkleLeft;
        case JointType.FootLeft: return NJointType.FootLeft;
        case JointType.HipRight: return NJointType.HipRight;
        case JointType.KneeRight: return NJointType.KneeRight;
        case JointType.AnkleRight: return NJointType.AnkleRight;
        case JointType.FootRight: return NJointType.FootRight;

        case JointType.HipCenter: return NJointType.SpineBase;
        case JointType.Spine: return NJointType.SpineMid;
        case JointType.ShoulderCenter: return NJointType.SpineShoulder;
      }
      return NJointType.Head;
    }

  }
}
