using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace net.encausse.sarah {
  public abstract class AbstractAddOnTask : IDisposable {

    protected CancellationTokenSource Source { get; set; }
    protected String Device { get; set; }
    protected String Name { get; set; }

    public AbstractAddOnTask(string device) {
      this.Device = device;
    }

    public void Start(TimeSpan dueTime, TimeSpan interval) {
      this.Source = new CancellationTokenSource();
      AsyncTask(dueTime, interval, Source.Token);
    }

    public virtual void Dispose() {
      if (Source != null) {
        Source.Cancel();
        Source = null;
      }
    }

    protected byte[] Color;
    protected Timestamp Stamp;
    protected int Width, Height, Fps;
    public void SetColor(byte[] data, Timestamp stamp, int width, int height, int fps) {
      Color = data;
      Stamp = stamp;
      Width  = width;
      Height = height;
      Fps = fps;
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log(Device, msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug(Device, msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error(Device, msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error(Device, ex);
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
        if (Stamp != null) {
          if (Stamp.Time == Timestamp) {
            TaskWatch.Reset();
            await Task.Delay(interval, token);
            continue;
          }
          Timestamp = Stamp.Time;
        }

        // Timestamp data
        TaskWatch.Again();

        // Do Job
        try { DoTask(); }
        catch (Exception ex) { Error(ex); }

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
      watch = new TextBlock { FontWeight = FontWeights.Bold };
      var ticks = (StackPanel)System.Windows.LogicalTreeHelper.FindLogicalNode(sidebar, "Ticks");
      ticks.Children.Add(watch);
    }
  }
}
