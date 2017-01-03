using UnityEngine;
using System;
using System.Collections;
using System.IO.Ports;


public class Reader : MonoBehaviour {
        /* The serial port where the Arduino is connected. */
    [Tooltip("The serial port where the Arduino is connected")]
    public string port = "COM4";
    /* The baudrate of the serial port. */
    [Tooltip("The baudrate of the serial port")]
    public int baudrate = 115200;

    private SerialPort stream;

	public float roll1, pitch1, yaw1, ax1, ay1, az1, roll2, pitch2, yaw2, ax2, ay2, az2;

    // Use this for initialization
    void Start () {
        Open();
    }
    
    // Update is called once per frame
    void Update () {
        if (ReadFromArduino() == null) {
            WriteToArduino("0");
        }
        else{
			
			float q = 0f;
            string [] data = ReadFromArduino().Split();
			if (float.TryParse (data [1], out q)) {
				yaw1 = float.Parse (data [1]);
			} else {
				yaw1 = 0f;
			}
			//print (yaw1);
			if (float.TryParse (data [2], out q)) {
				pitch1 = float.Parse (data [2]);
			} else {
				pitch1 = 0f;
			}
			if (float.TryParse (data [3], out q)) {
				roll1 = float.Parse (data [3]);
			} else {
				roll1 = 0f;
			}
			if (float.TryParse (data [5], out q)) {
				ax1 = float.Parse (data [5]);
			} else {
				ax1 = 0f;
			}
			if (float.TryParse (data [6], out q)) {
				ay1 = float.Parse (data [6]);
			} else {
				ay1 = 0f;
			}
			if (float.TryParse (data [7], out q)) {
				az1 = float.Parse (data [7]);
			} else {
				az1 = 0f;
			}



			if (float.TryParse (data [9], out q)) {
				yaw2 = float.Parse (data [9]);
			} else {
				yaw2 = 0f;
			}
			if (float.TryParse (data [10], out q)) {
				pitch2 = float.Parse (data [10]);
			} else {
				pitch2 = 0f;
			}
			if (float.TryParse (data [11], out q)) {
				roll2 = float.Parse (data [11]);
			} else {
				roll2 = 0f;
			}
			if (float.TryParse (data [13], out q)) {
				ax2 = float.Parse (data [13]);
			} else {
				ax2 = 0f;
			}
			if (float.TryParse (data [14], out q)) {
				ay2 = float.Parse (data [14]);
			} else {
				ay2 = 0f;
			}
			if (float.TryParse (data [15], out q)) {
				az2 = float.Parse (data [15]);
			} else {
				az2 = 0f;
			}
        }
    }



    public void Open () {
        // Opens the serial port
        stream = new SerialPort(port, baudrate);
        stream.ReadTimeout = 50;
        stream.Open();
        //this.stream.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
    }

    public void WriteToArduino(string message)
    {
        // Send the request
        stream.WriteLine(message);
        stream.BaseStream.Flush();
    }

    public string ReadFromArduino(int timeout = 0)
    {
        //stream.ReadTimeout = timeout;
        try
        {
            return stream.ReadLine();
        }
        catch (TimeoutException)
        {
            return null;
        }
    }
    

    public IEnumerator AsynchronousReadFromArduino(Action<string> callback, Action fail = null, float timeout = float.PositiveInfinity)
    {
        DateTime initialTime = DateTime.Now;
        DateTime nowTime;
        TimeSpan diff = default(TimeSpan);

        string dataString = null;

        do
        {
            // A single read attempt
            try
            {
                dataString = stream.ReadLine();
            }
            catch (TimeoutException)
            {
                dataString = null;
            }

            if (dataString != null)
            {
                callback(dataString);
                yield return null;
            } else
                yield return new WaitForSeconds(0.05f);

            nowTime = DateTime.Now;
            diff = nowTime - initialTime;

        } while (diff.Milliseconds < timeout);

        if (fail != null)
            fail();
        yield return null;
    }

    public void Close()
    {
        stream.Close();
    }
}


