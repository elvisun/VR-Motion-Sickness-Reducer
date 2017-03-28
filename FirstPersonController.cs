using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]



    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        //public Reader data;

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
		private int numberOfSensors;
		private int sensorDOF; 

		private bool[] convergingTest = new bool[3];	// test to see if the values have stopped changing
		private float convergingThreshhold;				// a value that determines what the allowable error range is
		private float[] previousData = new float[12]; 	// previous value
		private float[] diffArray = new float[12];		// store difference between current and previous data
		private float[] offsets = new float[12]; 		// offsets for all data
		private bool waitForStablization;		// wait until data is stablized

		private float waitTime;
		private float ax, ay, az, vx, vy, vz, kvx, kvy, kvz, constantvx, constantvy;		// keeps track of velocity in xyz, and also its coefficients
		private float initialSpeed, initialAngle, rotationAngle, rotationSensitivity, previousRotation; 
		private int waitCycles;
        // Use this for initialization
		private GameObject g;
		private Reader dataReader;

		private Queue<float>[] dataStream = new Queue<float>[12]; 					//holds all the data as a queue

		 
        private void Start()
        {	


			// character controller variables
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);


			//align view
			//this.gameObject.transform.rotation = this.gameObject.transform.GetChild (0).gameObject.transform.rotation;

			// logic variables
			g = GameObject.Find("Reader");
			dataReader = (Reader) g.GetComponent(typeof (Reader));
			waitTime = 3.0f;			// the reason why we wait is that sensor wait for couple seconds before sending data
			numberOfSensors = 2;
			sensorDOF = 6;

			// computation variables
			// default velocities
			vx = 0f;
			vy = 0f;
			vz = 0f;

			// scaling coefficients
			kvx = 0.02f;		// left and right
			kvy = 0.02f;		// up and down, which is y in unity
			kvz = 5f;		// front and back, which is y in arduino

			ax = 0f;
			ay = 0f;
			az = 0f;

			initialSpeed = 30f;

			initialAngle = 0f;
			rotationSensitivity = 1.5f;


        }


        // Update is called once per frame
        private void Update()
        {

            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

			if(Input.GetKeyDown(KeyCode.R)){
				transform.position = Vector3.zero;
				vx = 0f;
				vy = 0f;
				vz = 0f;
			}
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {	
			waitTime -= Time.deltaTime;

			if (waitTime < 0) {
				
				float speed;
				float highPassThreashold;
				GetInput (out speed);
				// always move along the camera forward as it is the direction that it being aimed at
				Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

				if (dataReader.currentData [0] != 0) {
					//print ("connection established");
				} else {
					//print ("opps, something went wrong...");
				}

				//logging data
				//print acceleration
				//print (dataReader.currentData [3].ToString ("0.00") + "  " + dataReader.currentData [5].ToString ("0.00") + "  " + dataReader.currentData [4].ToString ("0.00"));


				// compute distance to move
				previousRotation = rotationAngle;
				rotationAngle = (dataReader.currentData[0] * rotationSensitivity) / 180f * ((float)Math.PI);			// all Mathf functions take Radian, not degree
				//print(rotationAngle);
				if (Math.Abs (rotationAngle - previousRotation) > 0.0174444f * 0.1f) {		// if turning too much
					ax = 0f;
					ay = 0f;
					az = 0f;
					vz = 0f;
					waitCycles = 20;
					//print ("turning");
				} else {
					// wait for x cycles until data is stable
					waitCycles -= 1;
					if (waitCycles <= 0) {
						//print ("not turning");
						ax = Mathf.Cos (rotationAngle) * dataReader.currentData [3] - Mathf.Sin (rotationAngle) * dataReader.currentData [4];
						ay = Mathf.Sin (rotationAngle) * dataReader.currentData [3] + Mathf.Cos (rotationAngle) * dataReader.currentData [4];
						az = dataReader.currentData [5];

						print (az.ToString("0.00"));
						// integrate to get velocity
						highPassThreashold = 5f;
						if (Math.Abs (ax) > highPassThreashold) {
							vx += ax * kvx;
						} else
							vx = 0;
						if (Math.Abs (az) > highPassThreashold) {
							vz += az * kvz;
						} else
							vz = 0;
						if (Math.Abs (ay) > highPassThreashold) {
							vy += ay * kvy;
						} else
							vy = 0;
					} else {
						//print ("waiting");
					}
				}
					
				// calculate the new forward direction
				constantvx =  Mathf.Sin (rotationAngle) * initialSpeed; 
				constantvy =  Mathf.Cos (rotationAngle) * initialSpeed;


				//print (dataReader.currentData [4].ToString ("0.00") + "  " + dataReader.currentData [3].ToString ("0.00") + "  " + dataReader.currentData [5].ToString ("0.00"));

				//print (rotationAngle.ToString() + " , " + ax.ToString ("0.00") + "  " + ay.ToString ("0.00") + "  " + az.ToString ("0.00"));

				//print (vx.ToString ("0.00") + "  " + vy.ToString ("0.00") + "  " + vz.ToString ("0.00"));

				//print (vz);

				m_MoveDir.x = constantvx;//+ vx * Time.deltaTime;				// left and right
				m_MoveDir.z = constantvy;// + vy * Time.deltaTime;				// front and back, which is y in arduino
				//m_MoveDir.y = vz * Time.deltaTime;				// up and down, which is y in unity

				// 3 is left and right
				// 4 is front and back
				// 5 is up and down


				m_CollisionFlags = m_CharacterController.Move (m_MoveDir * Time.fixedDeltaTime);

				ProgressStepCycle (speed);
				UpdateCameraPosition (speed);

				m_MouseLook.UpdateCursorLock ();

			} else { 
				// waiting for sensor to send data
				//print (waitTime);
			}
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
