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
		private float[] currentData = new float[12];	// current value
		private float[] previousData = new float[12]; 	// previous value
		private float[] diffArray = new float[12];		// store difference between current and previous data
		private float[] offsets = new float[12]; 		// offsets for all data
		private bool waitForStablization;		// wait until data is stablized

		private float waitTime;

        // Use this for initialization
		private GameObject g;
		private Reader dataReader;

//		T[] InitializeArray<T>(int length) where T : new()
//		{
//			T[] array = new T[length];
//			for (int i = 0; i < length; ++i)
//			{
//				array[i] = new T();
//			}
//			return array;
//		}

		private Queue<float>[] dataStream = new Queue<float>[12]; 					//holds all the data as a queue

		 
        private void Start()
        {

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

			g = GameObject.Find("Reader");
			dataReader = (Reader) g.GetComponent(typeof (Reader));
			waitTime = 3.0f;			// the reason why we wait is that sensor wait for couple seconds before sending data
			numberOfSensors = 2;
			sensorDOF = 6;
			convergingThreshhold = 0.0001f;
			waitForStablization = true;

			for (int i = 0; i < 3; i++) {
				convergingTest [i] = false;
			}
			// initialize all elements of that array to construct queues
			for (int i = 0; i < 12; i++) {
				dataStream [i] = new Queue<float> ();
				currentData [i] = 0f;
				previousData [i] = 0f;
			}
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
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }

		private void readAndHandleInput(){
			if (waitForStablization) {
				for (int i = 0; i < 12; i++) {
					previousData [i] = currentData [i];
					currentData [i] = dataReader.dataPoints [i]; 
					diffArray [i] = Math.Abs ((currentData [i] - previousData [i]) / currentData [i]);
				}
				convergingTest [0] = convergingTest [1];
				convergingTest [1] = convergingTest [2]; 
				if ((diffArray [0] < convergingThreshhold) && (diffArray [1] < convergingThreshhold) && (diffArray [2] < convergingThreshhold)) {
					convergingTest [2] = true;	
				} else {
					convergingTest [2] = false;
				}
				//print (diffArray [0].ToString () + "  " + diffArray [1].ToString () + "  " + diffArray [2].ToString ());
				//print (convergingTest [0].ToString () + "  " + convergingTest [1].ToString () + "  " + convergingTest [2].ToString ());
				if (convergingTest [0] && convergingTest [1] && convergingTest [2]) {
					print ("Data has been stablized");
					waitForStablization = false;
					for (int i = 0; i < 12; i++) {
						offsets [i] = currentData [i];
					}
				}
			} else {
				// stablized already
				for (int i = 0; i < 12; i++) {
					currentData [i] = dataReader.dataPoints [i] - offsets[i];
				}
			}
		}

        private void FixedUpdate()
        {	
			waitTime -= Time.deltaTime;
			if (waitTime < 0) {
				
				readAndHandleInput ();
				if (!waitForStablization) {

					float speed;
					GetInput (out speed);
					// always move along the camera forward as it is the direction that it being aimed at
					Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

					float scale = 0.001f;
					if (currentData [0] != 0) {
						print ("connection established");
					} else {
						print ("opps, something went wrong...");
					}
					//print (averagedData [3].ToString ("0.00") +"  " + averagedData [5].ToString ("0.00") + "  " + averagedData [4].ToString ("0.00"));
					print (currentData [3].ToString ("0.00") + "  " + currentData [5].ToString ("0.00") + "  " + currentData [4].ToString ("0.00"));

					m_MoveDir.x = currentData [3] * scale;				// left and right
					m_MoveDir.z = currentData [4] * scale;				// front and back, which is y in arduino
					m_MoveDir.y = currentData [5] * scale;				// up and down, which is y in unity


					// if (m_CharacterController.isGrounded)
					// {
					//     m_MoveDir.y = -m_StickToGroundForce;

					//     if (m_Jump)
					//     {
					//         m_MoveDir.y = m_JumpSpeed;
					//         PlayJumpSound();
					//         m_Jump = false;
					//         m_Jumping = true;
					//     }
					// }
					// else
					// {
					//     m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
					// }
					m_CollisionFlags = m_CharacterController.Move (m_MoveDir * Time.fixedDeltaTime);

					ProgressStepCycle (speed);
					UpdateCameraPosition (speed);

					m_MouseLook.UpdateCursorLock ();
				} else {
					print ("waiting for stable input...");
				}
			} else { 

				// waiting for sensor to send data
				print (waitTime);
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
