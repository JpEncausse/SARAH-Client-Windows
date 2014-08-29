using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace net.encausse.sarah.rtp {
  /// <summary>
  /// Connects to an RTP stream and listens for data
  /// http://stackoverflow.com/questions/15886888/c-sharp-capture-rtp-stream-and-send-to-speech-recognition/15934124#15934124
  /// </summary>
  public class RTPClient {
    private const int AUDIO_BUFFER_SIZE = 65536;

    private UdpClient client;
    private IPEndPoint endPoint;
    private Stream stream;
    private bool writeHeaderToConsole = true;
    private bool listening = false;
    private int port;
    private Thread listenerThread;


    /// <summary>
    /// Gets whether the client is listening for packets
    /// </summary>
    public bool Listening {
      get { return listening; }
    }
    /// <summary>
    /// Gets the port the RTP client is listening on
    /// </summary>
    public int Port {
      get { return port; }
    }

    /// <summary>
    /// RTP Client for receiving an RTP stream containing a WAVE audio stream
    /// </summary>
    /// <param name="port">The port to listen on</param>
    /// <param name="stream">The stream to write to</param>
    public RTPClient(int port, Stream stream) {
      this.port = port;
      this.stream = stream;
    }

    /// <summary>
    /// Creates a connection to the RTP stream
    /// </summary>
    public void StartClient() {
      // Create new UDP client. The IP end point tells us which IP is sending the data
      client = new UdpClient(port);
      endPoint = new IPEndPoint(IPAddress.Broadcast, port);

      Log("Listening for packets on port " + port + "...");

      listening = true;
      listenerThread = new Thread(ReceiveCallback);
      listenerThread.Name = "UDP Thread";
      listenerThread.Start();
    }

    /// <summary>
    /// Tells the UDP client to stop listening for packets.
    /// </summary>
    public void StopClient() {
      // Set the boolean to false to stop the asynchronous packet receiving
      listening = false;
      listenerThread.Interrupt();
      try { client.Close(); } catch (SocketException) { }
      Log("Stopped listening on port " + port);
    }

    /// <summary>
    /// Handles the receiving of UDP packets from the RTP stream
    /// </summary>
    /// <param name="ar">Contains packet data</param>
    private void ReceiveCallback() {
      Log("ReceiveCallback");

      // Begin looking for the next packet
      while (listening) {
        // Receive packet
        try {
          byte[] packet = client.Receive(ref endPoint);

          // Decode the header of the packet
          int version = GetRTPHeaderValue(packet, 0, 1);
          int padding = GetRTPHeaderValue(packet, 2, 2);
          int extension = GetRTPHeaderValue(packet, 3, 3);
          int csrcCount = GetRTPHeaderValue(packet, 4, 7);
          int marker = GetRTPHeaderValue(packet, 8, 8);
          int payloadType = GetRTPHeaderValue(packet, 9, 15);
          int sequenceNum = GetRTPHeaderValue(packet, 16, 31);
          int timestamp = GetRTPHeaderValue(packet, 32, 63);
          int ssrcId = GetRTPHeaderValue(packet, 64, 95);

          if (writeHeaderToConsole) {
            Debug(version + ", " + padding + ", " + extension + ", " + csrcCount + ", " + marker + ", " + payloadType + ", " + sequenceNum + ", " + timestamp + ", " + ssrcId);
          }

          // Write the packet to the audio stream
          stream.Write(packet, 12, packet.Length - 12);
        } catch (SocketException) { break; } // exit the while loop
      }
    }

    /// <summary>
    /// Grabs a value from the RTP header in Big-Endian format
    /// </summary>
    /// <param name="packet">The RTP packet</param>
    /// <param name="startBit">Start bit of the data value</param>
    /// <param name="endBit">End bit of the data value</param>
    /// <returns>The value</returns>
    private int GetRTPHeaderValue(byte[] packet, int startBit, int endBit) {
      int result = 0;

      // Number of bits in value
      int length = endBit - startBit + 1;

      // Values in RTP header are big endian, so need to do these conversions
      for (int i = startBit; i <= endBit; i++) {
        int byteIndex = i / 8;
        int bitShift = 7 - (i % 8);
        result += ((packet[byteIndex] >> bitShift) & 1) * (int)Math.Pow(2, length - i + startBit - 1);
      }
      return result;
    }

    // -------------------------------------------
    //  UTILITY
    // -------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log("RTP", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("RTP", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("RTP", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("RTP", ex);
    }
  }
}
