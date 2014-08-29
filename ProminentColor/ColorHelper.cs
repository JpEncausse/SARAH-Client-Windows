using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net.encausse.sarah.color {

    // ==========================================
    //  RGB
    // ==========================================

    public class RGB {
      public int r = 0;
      public int g = 0;
      public int b = 0;
      public int d = 0;
      public int count = 0;

      public override String ToString() {
        return "r:" + r + ", g:" + g + ", b:" + b + ", c:" + count + ", d:" + d;
      }
    };

    // ==========================================
    //  PIXEL
    // ==========================================

    public class Pixel {
      public int r = 0;
      public int g = 0;
      public int b = 0;
      public int count = 1;
      public int weight = 1;

      public bool DoesRgbMatch(RGB rgb) {
        if (null == rgb)
          return true;
        int r = this.r >> rgb.d;
        int g = this.g >> rgb.d;
        int b = this.b >> rgb.d;
        return rgb.r == r && rgb.g == g && rgb.b == b;
      }

      public void favorHue() {
        if (r > 220 && g > 220 && b > 220) { weight = 0; }
        else if (r < 100 && g < 100 && b < 100) { weight = 0; }
        else { weight = 1;
          //weight = (int) ((Math.Abs(r - g) * Math.Abs(r - g) + Math.Abs(r - b) * Math.Abs(r - b) + Math.Abs(g - b) * Math.Abs(g - b)) / 65535d * 50 + 1);
        }
      }

      public override String ToString() {
        return "r:" + r + ", g:" + g + ", b:" + b + ", c:" + count + ", w:" + weight;
      }
    };

    // ==========================================
    //  COLOR
    // ==========================================

    public class ColorHelper {

      private Dictionary<string, Pixel> pixels = new Dictionary<string, Pixel>();
      private Dictionary<string, int> weights = new Dictionary<string, int>();

      private byte[] buffer = null;
      public RGB GetMostProminentColor(byte[] data) {
        if (null == data) { return null; }
        if (null == buffer) { buffer = new byte[data.Length]; }
        Array.Copy(data, buffer, data.Length);

        FillImageData(data, 0);

        RGB rgb = null;
        rgb = GetMostProminentRGB(6, rgb);
        rgb = GetMostProminentRGB(4, rgb);
        rgb = GetMostProminentRGB(2, rgb);
        rgb = GetMostProminentRGB(0, rgb);

        return rgb;
      }

      protected RGB GetMostProminentRGB(int degrade, RGB rgbMatch) {
        weights.Clear();
        int count = 0;

        foreach (Pixel pixel in pixels.Values) {
          int weight = pixel.weight * pixel.count;
          count++;

          if (pixel.DoesRgbMatch(rgbMatch)) {
            String key = (pixel.r >> degrade) + "," + (pixel.g >> degrade) + "," + (pixel.b >> degrade);
            if (weights.ContainsKey(key)) {
              weights[key] += weight;
            }
            else {
              weights.Add(key, weight);
            }
          }
        }

        RGB rgb = new RGB();
        rgb.d = degrade;

        foreach (String key in weights.Keys) {
          if (count <= weights[key])
            continue;

          String[] data = key.Split(',');
          rgb.count = count;
          rgb.r = int.Parse(data[0]);
          rgb.g = int.Parse(data[1]);
          rgb.b = int.Parse(data[2]);
        }

        return rgb;
      }

      protected void FillImageData(byte[] data, int degrade) {
        pixels.Clear();
        int length = data.Length;
        int factor = (int)Math.Max(1, Math.Round(length / 50f));

        for (int i = 0; i < data.Length; i += factor * 4) { // bgra

          String key = (data[i + 2] >> degrade) + "," + (data[i + 1] >> degrade) + "," + (data[i] >> degrade);
          Pixel pixel = null;
          if (pixels.TryGetValue(key, out pixel)) {
            pixel.count++;
          }
          else {
            pixel = new Pixel();
            pixel.b = data[i];
            pixel.g = data[i + 1];
            pixel.r = data[i + 2];
            pixel.favorHue();
            pixels.Add(key, pixel);
          }
        }
      }
    }

  }
