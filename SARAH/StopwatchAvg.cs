using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net.encausse.sarah {
  public class StopwatchAvg : Stopwatch {

    private int iteration = 1;
    private int current = 1;
    private TimeSpan sum;


    public StopwatchAvg()
      : base() {
      this.iteration = 1000;
      this.current = 1000;
    }
    public StopwatchAvg(int it)
      : base() {
      this.iteration = it;
      this.current = it;
    }

    new public void Reset(){
      base.Reset();
      this.current = this.iteration;
      this.sum = TimeSpan.Zero;
    }

    public void Again() {
      if (current-- == 0) {
        this.current = iteration;
        this.sum = Elapsed;
      }
      else {
        this.sum += Elapsed;
      }
      base.Restart();
    }

    public long AverageMilliseconds() {
      double count = (iteration - current);
      return (long)(sum.Milliseconds / count);
    }

    public long AverageTicks() {
      double count = (iteration - current);
      return (long)(sum.Ticks / count);
    }

    public String Average() {
      double count = (iteration - current);

      long ms = AverageMilliseconds();
      if (ms > 0) {
        return String.Format("   {0:000}ms", ms);
      }

      long tk = AverageTicks();
      if (tk > 0) {
        return String.Format("{0:000000}tk", tk);
      }

      return "        ";
    }
  }
}
