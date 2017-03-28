using UnityEngine;
using System;
using System.Collections;
using System.Threading;
using System.IO.Ports;


using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

public class Reader : MonoBehaviour {
        /* The serial port where the Arduino is connected. */
    [Tooltip("The serial port where the Arduino is connected")]
    public string port = "COM4";
    /* The baudrate of the serial port. */
    [Tooltip("The baudrate of the serial port")]
    public int baudrate = 115200;

    private SerialPort stream;

	private float q;

	private Thread myThread;

	private long now; 

	private bool[] convergingTest = new bool[3];	// test to see if the values have stopped changing
	private float convergingThreshhold;				// a value that determines what the allowable error range is
	private float[] previousData = new float[12]; 	// previous value
	private float[] diffArray = new float[12];		// store difference between current and previous data
	private float[] offsets = new float[12]; 		// offsets for all data
	private bool waitForStablization;		// wait until data is stablized
	private bool transition; 
	private bool threadAlive;
	private string newLine;
	private bool offsetFlag; 


	public float[] currentData = new float[12];	// current value
	public float[] dataPoints = new float[12];
    // Use this for initialization
    void Start () {
		q = 0f; 
		offsetFlag = false;
		Open ();
		transition = false;
		//setting converging variables
		convergingThreshhold = 0.0001f;
		waitForStablization = true;

		for (int i = 0; i < 3; i++) {
			convergingTest [i] = false;
		}
		// initialize all elements of that array to construct queues
		for (int i = 0; i < 12; i++) {
			currentData [i] = 0f;
			previousData [i] = 0f;
		}
		// wait for arduino to initialize 
		myThread = new Thread (new ThreadStart (readData));
		myThread.Start ();
    }

	void Awake() {
		DontDestroyOnLoad (transform.gameObject);
		threadAlive = true;
	}

	void Update() {
		if (!waitForStablization && !transition) {
			Application.LoadLevel("main");
			transition = true;
			print ("Scene Transitioning");
		}
	}

	void OnDestroy() {
		threadAlive = false;
	}

	private void readAndHandleInput(){
		if (waitForStablization) {
			for (int i = 0; i < 12; i++) {
				previousData [i] = currentData [i];
				currentData [i] = dataPoints [i]; 
				diffArray [i] = Math.Abs ((currentData [i] - previousData [i]) / currentData [i]);
			}
			convergingTest [0] = convergingTest [1];
			convergingTest [1] = convergingTest [2]; 
			if ((diffArray [0] < convergingThreshhold) && (diffArray [1] < convergingThreshhold) && (diffArray [2] < convergingThreshhold)) {		// check if data has converged
				convergingTest [2] = true;	
			} else {
				convergingTest [2] = false;
			}
			//print (diffArray [3].ToString () + "  " + diffArray [4].ToString () + "  " + diffArray [5].ToString ());
			//print (convergingTest [0].ToString () + "  " + convergingTest [1].ToString () + "  " + convergingTest [2].ToString ());
			if (convergingTest [0] && convergingTest [1] && convergingTest [2]) {
				print ("Data has been stablized");
				waitForStablization = false;
				for (int i = 0; i < 12; i++) {
					offsets [i] = currentData [i];
				}
			}
		} else {
			for (int i = 0; i < 12; i++) {
				currentData [i] = dataPoints [i] - offsets[i];
			}
			//print(dataPoints[5].ToString("0.00"));

		}
	}

    // Update is called once per frame
    void readData () {
		System.Threading.Thread.Sleep (3000);
		while (threadAlive) {
			//print ("running");
			newLine = ReadFromArduino ();
			if (newLine == null) {
				WriteToArduino("0");
				System.Threading.Thread.Sleep (2000);
				print ("writing to arduino");
			}
			else{
				string [] data = newLine.Split();
				for (int i = 1; i < 13; i++) {
					// data are stored in slots 123 567 91011 131415
					int j = 0;
					j = (i - 1) / 3 + i;

					// if not enough data points, line not read properly
					if (j >= data.Length) {
						break;
					}
					// convert string to float
					if (float.TryParse (data [j], out q)) {				// THIS LINE IS WRONG??? 
						dataPoints [i - 1] = float.Parse (data [j]);
					} else {
						//print("parsing failed, default to 0");
						dataPoints [i - 1] = 0f;
					}
				}
				readAndHandleInput ();
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


