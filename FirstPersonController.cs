using System;
using System.Collections;
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
		private float roll1, pitch1, yaw1, ax1, ay1, az1, roll2, pitch2, yaw2, ax2, ay2, az2;	// to be imported from reader
		private float roll1Offset, pitch1Offset, yaw1Offset, ax1Offset, ay1Offset, az1Offset, roll2Offset, pitch2Offset, yaw2Offset, ax2Offset, ay2Offset, az2Offset;
		private Queue roll1Q = new Queue();
		private Queue pitch1Q = new Queue();
		private Queue yaw1Q = new Queue();
		private Queue ax1Q = new Queue();
		private Queue ay1Q = new Queue();
		private Queue az1Q = new Queue();
		private Queue roll2Q = new Queue();
		private Queue pitch2Q = new Queue();
		private Queue yaw2Q = new Queue();
		private Queue ax2Q = new Queue();
		private Queue ay2Q = new Queue();
		private Queue az2Q = new Queue();
		private int counter;
		private int averagingSize;

		private float waitTime;

        // Use this for initialization
		private GameObject g;
		private Reader dataReader;


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
			counter = 0;
			averagingSize = 10;

			roll1= 0;
			pitch1= 0;
			yaw1= 0;
			ax1= 0;
			ay1= 0;
			az1= 0;
			roll2= 0;
			pitch2= 0;
			yaw2= 0;
			ax2= 0;
			ay2= 0;
			az2 = 0;

			waitTime = 20.0f;

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
			if (counter < averagingSize - 1) {
				roll1Q.Enqueue (dataReader.roll1);
				pitch1Q.Enqueue (dataReader.pitch1);
				yaw1Q.Enqueue (dataReader.yaw1);
				ax1Q.Enqueue (dataReader.ax1);
				ay1Q.Enqueue (dataReader.ay1);
				az1Q.Enqueue (dataReader.az1);
				roll2Q.Enqueue (dataReader.roll2);
				pitch2Q.Enqueue (dataReader.pitch2);
				yaw2Q.Enqueue (dataReader.yaw2);
				ax2Q.Enqueue (dataReader.ax2);
				ay2Q.Enqueue (dataReader.ay2);
				az2Q.Enqueue (dataReader.az2);
				counter++;
			}
			else if( counter < averagingSize){
				roll1Q.Enqueue (dataReader.roll1);
				pitch1Q.Enqueue (dataReader.pitch1);
				yaw1Q.Enqueue (dataReader.yaw1);
				ax1Q.Enqueue (dataReader.ax1);
				ay1Q.Enqueue (dataReader.ay1);
				az1Q.Enqueue (dataReader.az1);
				roll2Q.Enqueue (dataReader.roll2);
				pitch2Q.Enqueue (dataReader.pitch2);
				yaw2Q.Enqueue (dataReader.yaw2);
				ax2Q.Enqueue (dataReader.ax2);
				ay2Q.Enqueue (dataReader.ay2);
				az2Q.Enqueue (dataReader.az2);
				counter++;

				foreach (float num in roll1Q.ToArray()) {
					roll1 = roll1 + num;
				}
				roll1 = roll1 / averagingSize;

				foreach (float num in pitch1Q.ToArray()) {
					pitch1 = pitch1 + num;
				}
				pitch1 = pitch1 / averagingSize;

				foreach (float num in yaw1Q.ToArray()) {
					yaw1 = yaw1 + num;
				}
				yaw1 = yaw1 / averagingSize;

				foreach (float num in ax1Q.ToArray()) {
					ax1 = ax1 + num;
				}
				ax1 = ax1 / averagingSize;

				foreach (float num in ay1Q.ToArray()) {
					ay1 = ay1 + num;
				}
				ay1 = ay1 / averagingSize;

				foreach (float num in az1Q.ToArray()) {
					az1 = az1 + num;
				}
				az1 = az1 / averagingSize;

				foreach (float num in roll2Q.ToArray()) {
					roll2 = roll2 + num;
				}
				roll2 = roll2 / averagingSize;

				foreach (float num in pitch2Q.ToArray()) {
					pitch2 = pitch2 + num;
				}
				pitch2 = pitch2 / averagingSize;

				foreach (float num in yaw2Q.ToArray()) {
					yaw2 = yaw2 + num;
				}
				yaw2 = yaw2 / averagingSize;

				foreach (float num in ax2Q.ToArray()) {
					ax2 = ax2 + num;
				}
				ax2 = ax2 / averagingSize;

				foreach (float num in ay2Q.ToArray()) {
					ay2 = ay2 + num;
				}
				ay2 = ay2 / averagingSize;

				foreach (float num in az2Q.ToArray()) {
					az2 = az2 + num;
				}
				az2 = az2 / averagingSize;

				roll1= 0;
				pitch1= 0;
				yaw1= 0;
				ax1= 0;
				ay1= 0;
				az1= 0;
				roll2= 0;
				pitch2= 0;
				yaw2= 0;
				ax2= 0;
				ay2= 0;
				az2 = 0;

			}
			else {										//applying moving averaging filter
				roll1Q.Enqueue ((float)dataReader.roll1);
				pitch1Q.Enqueue ((float)dataReader.pitch1);
				yaw1Q.Enqueue ((float)dataReader.yaw1);
				ax1Q.Enqueue ((float)dataReader.ax1);
				ay1Q.Enqueue ((float)dataReader.ay1);
				az1Q.Enqueue ((float)dataReader.az1);
				roll2Q.Enqueue ((float)dataReader.roll2);
				pitch2Q.Enqueue ((float)dataReader.pitch2);
				yaw2Q.Enqueue ((float)dataReader.yaw2);
				ax2Q.Enqueue ((float)dataReader.ax2);
				ay2Q.Enqueue ((float)dataReader.ay2);
				az2Q.Enqueue ((float)dataReader.az2);


				roll1 += (dataReader.roll1);
				pitch1 += (dataReader.pitch1);
				yaw1 += (dataReader.yaw1);
				ax1 += (dataReader.ax1);
				ay1 += (dataReader.ay1);
				az1 += (dataReader.az1);
				roll2 += (dataReader.roll2);
				pitch2 += (dataReader.pitch2);
				yaw2 += (dataReader.yaw2);
				ax2 += (dataReader.ax2);
				ay2 += (dataReader.ay2);
				az2 += (dataReader.az2);

				roll1 -= (float) roll1Q.Dequeue();
				pitch1 -= (float)pitch1Q.Dequeue();
				yaw1 -= (float)yaw1Q.Dequeue();
				ax1 -= (float)ax1Q.Dequeue();
				ay1 -= (float)ay1Q.Dequeue();
				az1 -= (float)az1Q.Dequeue();
				roll2 -= (float)roll2Q.Dequeue();
				pitch2 -= (float)pitch2Q.Dequeue();
				yaw2 -= (float)yaw2Q.Dequeue();
				ax2 -= (float)ax2Q.Dequeue();
				ay2 -= (float)ay2Q.Dequeue();
				az2 -= (float)az2Q.Dequeue();
			}
		}

        private void FixedUpdate()
        {	
			waitTime -= Time.deltaTime;
			print (waitTime);
			if (waitTime < 0) {

				readAndHandleInput ();

				//			print (yaw1.ToString ("R") + " " + pitch1.ToString ("R") + " " +roll1.ToString ("R") + " " +ax1.ToString ("R") +" " +ay1.ToString ("R") + " " +az1.ToString ("R"));
				//			print (yaw2.ToString ("R") + " " +pitch2.ToString ("R") + " " +roll2.ToString ("R") + " " +ax2.ToString ("R") + " " +ay2.ToString ("R") + " " +az2.ToString ("R"));
				//			print ("=========");
				float speed;
				GetInput(out speed);
				// always move along the camera forward as it is the direction that it being aimed at
				Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

				// get a normal for the surface that is being touched to move along it
				// RaycastHit hitInfo;
				// Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
				//                    m_CharacterController.height/2f, ~0, QueryTriggerInteraction.Ignore);
				// desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

				float scale = 0.0005f;
				print (ax1.ToString ("R") + " " + ay1.ToString ("R") + " " + az1.ToString ("R"));
				m_MoveDir.x = ax1 * scale;
				m_MoveDir.y = ay1 * scale;
				m_MoveDir.z = az1 * scale;


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
				m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

				ProgressStepCycle(speed);
				UpdateCameraPosition(speed);

				m_MouseLook.UpdateCursorLock();
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
