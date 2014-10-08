using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace net.encausse.sarah {
  public abstract class AbstractAddOnTask : IDisposable {

    protected CancellationTokenSource Source { get; set; }
    public String Device { get; set; }
    public String Name { get; set; }


    private TimeSpan dueTime;
    private TimeSpan interval;
    public AbstractAddOnTask(TimeSpan dueTime, TimeSpan interval) {
      this.dueTime = dueTime;
      this.interval = interval;
    }
    
    public void Start() {
      this.Source = new CancellationTokenSource();
      AsyncTask(dueTime, interval, Source.Token);
    }

    public virtual void Dispose() {
      if (Source != null) {
        Source.Cancel();
        Source = null;
      }
      EndTask();
    }

    protected ColorFrame Color = null;
    protected BodyFrame  Body  = null;
    protected DepthFrame Depth = null;

    protected List<DeviceFrame> Frames = new List<DeviceFrame>();
    public void AddFrame(DeviceFrame frame){
      Frames.Add(frame);

      if (frame is ColorFrame) { Color = (ColorFrame)frame; }
      if (frame is BodyFrame)  { Body  = (BodyFrame)frame; }
      if (frame is DepthFrame) { Depth = (DepthFrame)frame; }
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log(Name + "|" + Device, msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug(Name + "|" + Device, msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error(Name + "|" + Device, msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error(Name + "|" + Device, ex);
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    private bool pause = false;
    public virtual void Pause(bool state) {
      Log("Pause: " + state);
      this.pause = state;
    }

    protected virtual void InitTask() { }
    protected virtual void DoTask() { }
    protected virtual void EndTask() { }

    private DateTime Timestamp = DateTime.Now;
    public StopwatchAvg TaskWatch = new StopwatchAvg();
    private async Task AsyncTask(TimeSpan dueTime, TimeSpan interval, CancellationToken token) {

      // Initial wait time before we begin the periodic loop.
      if (dueTime > TimeSpan.Zero)
        await Task.Delay(dueTime, token);

      InitTask();

      // Repeat this loop until cancelled.
      while (!token.IsCancellationRequested) {

        if (pause) {
          TaskWatch.Reset();
          await Task.Delay(interval, token);
          continue;
        }

        // Check if Frame has been updated
        if (Color.Stamp != null) {
          if (Color.Stamp.Time == Timestamp) {
            TaskWatch.Reset();
            await Task.Delay(interval, token);
            continue;
          }
          Timestamp = Color.Stamp.Time;
        }

        // Timestamp data
        TaskWatch.Again();

        // Do Job
        try { DoTask(); }
        catch (Exception ex) { 
          Error(ex); 
        }

        TaskWatch.Stop();

        // Wait to repeat again.
        try {
          if (interval > TimeSpan.Zero)
            await Task.Delay(interval, token);
        } catch (ThreadInterruptedException){ break; }
      }

      EndTask();
    }

    // -------------------------------------------
    //  UI
    // ------------------------------------------

    public virtual void RepaintColorFrame(byte[] data, int width, int height) {
      if (watch != null) {
        var avg = TaskWatch.Average();
        if (!String.IsNullOrWhiteSpace(avg)){
          watch.Text = Name + ": " + avg;
        }
      }
    }

    private TextBlock watch;
    public virtual void HandleSidebar(StackPanel sidebar) {
      if (sidebar.Name != "Sidebar") { return; }
      watch = new TextBlock { FontWeight = FontWeights.Bold };
      var ticks = (StackPanel)System.Windows.LogicalTreeHelper.FindLogicalNode(sidebar, "Ticks");
      ticks.Children.Add(watch);
    }

    public virtual void HandleSelection(Rectangle rect) {  }
  }
}
