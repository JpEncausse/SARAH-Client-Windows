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

    private DepthFrame Depth;
    private ColorFrame Color;
    private BodyFrame Skeletons;
    private ColorImageFormat ColorFormat;

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
        if (!init) { return; }

        // Backup frames (motion)
        depthFrame.CopyPixelDataTo(Depth.Pixelss);

        // Motion check
        if (Task == null || Task.StandBy) {
          AllFrameWatch.Stop();
          return;
        }

        // Copy computed depth
        if (++fpsDepth >= FPSDepth) {
          fpsDepth = 0;
          // depthFrame.CopyDepthImagePixelDataTo(this.DepthPixels);
          Depth.Stamp.Time = System.DateTime.Now;
        }

        // Copy color data
        if (++fpsColor >= FPSColor) { fpsColor = 0;
          colorFrame.CopyPixelDataTo(Color.Pixels);
          Color.Stamp.Time = System.DateTime.Now;
        }

        // Copy skeleton data
        if (++fpsSkeleton >= FPSSkeleton) { fpsSkeleton = 0;
          skeletonFrame.CopySkeletonDataTo((Skeleton[])Skeletons.RawData);
          Skeletons.Stamp.Time = System.DateTime.Now;
        }

        // Convert Joint 3D to 2D on 1st skeleton
        if (++fpsJoints >= FPSJoints) { 
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

      // Color Frame
      Color = new ColorFrame();
      Color.Width = colorFrame.Width;
      Color.Height = colorFrame.Height;
      Color.Pixels = new byte[colorFrame.PixelDataLength];
      Color.Stamp = new Timestamp();
      Color.Fps = FPS;
      AddOnManager.GetInstance().InitFrame(Name, Color);
      Log(Color.ToString());
      ColorFormat = colorFrame.Format;

      // Depth Frame
      Depth = new DepthFrame();
      Depth.Width = depthFrame.Width;
      Depth.Height = depthFrame.Height;
      Depth.Pixelss = new short[depthFrame.PixelDataLength];
      Depth.Stamp = new Timestamp();
      AddOnManager.GetInstance().InitFrame(Name, Depth);
      Log(Depth.ToString());

      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("kinect_v1.motion.ms", 100));
      Task = new MotionTask(dueTime, interval);
      Task.Device = "";
      Task.AddFrame(Depth);
      Task.Start();


      // Skeleton Frame
      Skeletons = new BodyFrame();
      Skeletons.Width  = colorFrame.Width;
      Skeletons.Height = colorFrame.Height;
      Skeletons.RawData = new Skeleton[6];
      Skeletons.Bodies  = new List<NBody>(6);
      Skeletons.Stamp = new Timestamp();
      AddOnManager.GetInstance().InitFrame(Name, Skeletons);
      Log(Skeletons.ToString());

    }

    // -------------------------------------------
    //  INTERNAL BODY
    // -------------------------------------------

    private ICollection<NBody> cache = new List<NBody>();
    private void RefreshBodyData(BodyFrame frame) {
      cache.Clear();
      foreach (var skeleton in (IList<Skeleton>)frame.RawData) {
        if (skeleton.TrackingState != SkeletonTrackingState.Tracked) { continue; }

        NBody nbody = frame.Find(Convert.ToUInt64(skeleton.TrackingId));
        if (nbody == null) {
          nbody = new NBody(Convert.ToUInt64(skeleton.TrackingId), frame.Width, frame.Height);
        }

        cache.Add(nbody);
        RefreshBodyData(skeleton, nbody);
      }

      lock (frame.Bodies) {
        frame.Bodies.Clear();
        frame.Bodies.AddRange(cache);
      }
    }

    private void RefreshBodyData(Skeleton skeleton, NBody nbody) {
      cache.Add(nbody);

      // Joints
      foreach (Joint joint in skeleton.Joints) {
        var ntype = ResolveJointType(joint.JointType);
        var njoint = nbody.GetJoint(ntype);
        var point = Sensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorFormat);

        njoint.Tracking = ResolveTrackingState(joint.TrackingState);
        njoint.SetPosition2D(point.X, point.Y);
        njoint.SetPosition3D(joint.Position.X, joint.Position.Y, joint.Position.Z);
      }

      // Misc
      nbody.Tracking = NTrackingState.Tracked;
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
