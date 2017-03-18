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

	private float q;

	public float[] dataPoints = new float[12];
    // Use this for initialization
    void Start () {
		q = 0f; 

		Open();
    }
    
    // Update is called once per frame
    void Update () {
        if (ReadFromArduino() == null) {
            WriteToArduino("0");
			print ("writing to arduino");
        }
        else{
			
            string [] data = ReadFromArduino().Split();
			for (int i = 1; i < 13; i++) {
				
				// data are stored in slots 123 567 91011 131415
				int j = 0;
				j = (i - 1) / 3 + i;

				// convert string to float
				if (float.TryParse (data [j], out q)) {
					dataPoints [i - 1] = float.Parse (data [j]);
				} else {
					dataPoints [i - 1] = 0f;
				}
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


