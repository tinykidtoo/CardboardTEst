﻿using UnityEngine;
using System.Collections;

[AddComponentMenu("Car Control/Ai Car Movement")]
public class AiOpponet : MonoBehaviour
{
		public enum driveType
		{
				Angry,
				Provoked,
				Normal
		}
		public driveType myAngerLevel;
		public int AngerBar = 0;
		public float myEngineTorque = 10f;
		public float myMaxTurnAmount = 50f;
		public float myMinTurnAmount = 5f;
		public float highSpeed = 50f;
		public float myCurrentSpeed;
		public float topSpeed = 10f;
		public float downPressureFactor = 0.5f;
		[Tooltip("The WheelColliders in this order : 0 = LF; 1 = LB; 2 = RF; 3 = RB")]
		public WheelCollider[]
				myColliderWheels = new WheelCollider[4]; 				//0LF 1LB 2RF 3RB
		[Tooltip("The WheelMesh in this order : 0 = LF; 1 = LB; 2 = RF; 3 = RB")]
		public Transform[]
				myVisualWheels = new Transform[4]; 							//0LF 1LB 2RF 3RB
		[Tooltip("The WheelColliders you wish to turn.  The order does not matter.")]
		public WheelCollider[]
				myTurnColl = new WheelCollider[2];						//0 == left && 1 == right
		[Tooltip("The WheelMeshes that you wish to turn. The order must match Turning Colliders.")]
		public Transform[]
				myTurnWheels = new Transform[2];								//0 == left && 1 == right
		[Tooltip("The WheelColliders which will have power applied to them.  The order does not matter.")]
		public WheelCollider[]
				myEngineWheels = new WheelCollider[2];
		private float h;
		private float v;
		private float mySteer;
		private AudioSource myEngine;
		public Transform target;
		public AiNodeGraph myNodeGraph;
		public int currentNode = 0;
		private float close2Node = 0;				//this gets set by the node network
		public int desiredSpeed = 40;
		public int reverseBrakeAmount = 40;
		private Vector3 lastSolidGround;
		bool isGrounded = false;
		float count = 5f;
		bool reseting = false;

		// Use this for initialization
		void Start ()
		{
				myEngine = gameObject.GetComponent<AudioSource> ();
				myEngine.pitch = 1f;
				myEngine.Play ();
				myAngerLevel = driveType.Normal;
				currentNode = 0;
				target = myNodeGraph.myNodes [currentNode];
				close2Node = (myNodeGraph.detectionRange * myNodeGraph.detectionRange);
				lastSolidGround = transform.position;
		}

		// Update is called once per frame
		void Update ()
		{
				CalculateAiWheelSteering ();
				CalculateSpeed ();
				UpdateVisualWheels ();	
				AdjustVolumePitch ();
				if (count < 0)
						ResetCar ();
				if (!isGrounded)
						count -= Time.deltaTime;
				if (count != 10f && isGrounded)
						count = 10f;
		}
		
		void FixedUpdate ()
		{
				mySteer = Mathf.Lerp (myMaxTurnAmount, myMinTurnAmount, myCurrentSpeed / highSpeed);
				EngineTorque ();
				DownwardForce ();
		}

		void OnTriggerEnter (Collider col)
		{
				if (col.gameObject.tag == "SpeedZone") {
						SpeedZone tempSpeedZone = col.gameObject.GetComponent<SpeedZone> ();
						desiredSpeed = Random.Range (tempSpeedZone.SpeedForZone - 10, tempSpeedZone.SpeedForZone + 25);
				}
		}
		
		void CalculateAiWheelSteering ()
		{
				h = Turn2Facing (target.position);
				h = Mathf.Clamp (h, -1, 1);
				if (h > 0.2f)
						h = 1f;
				if (h < -0.2f)
						h = -1f;
				if (HowCloseAmI (transform.position, target.position) < close2Node) {
						currentNode ++;
						if (currentNode > (myNodeGraph.myNodes.Count - 1))
								currentNode = 0;
						target = myNodeGraph.myNodes [currentNode];
				}
				if (myCurrentSpeed > desiredSpeed) {
						if (myCurrentSpeed > (desiredSpeed + 10f))
								v = -1f;
						else
								v = 0f;
				} else {
						v = 1F;
				}
		}
	
		float Turn2Facing (Vector3 target)//suggested 0.5f to see if you are within 180 degresses
		{
				Vector3 dir = (target - transform.position).normalized;
				float direction = Vector3.Dot (dir, transform.forward);
				return direction;
		}

		private float HowCloseAmI (Vector3 pointA, Vector3 pointB)	//this is what you have to do to make this work
		{
				Vector3 offset = pointA - pointB;					//This is our offset from the target
				float sqrLen = offset.sqrMagnitude;					//this does a love function i cant describe to you
				return sqrLen;										//this gives you the product of that sweet love function
		}

		void CalculateSpeed ()
		{
				myCurrentSpeed = 2 * 22 / 7 * myTurnColl [0].radius * myTurnColl [0].rpm * 60 / 1000;
				myCurrentSpeed = Mathf.Round (-myCurrentSpeed);
		}
	
		void UpdateVisualWheels ()
		{
				for (int i = 0; i < myTurnColl.Length; i++) {
						myTurnColl [i].steerAngle = (mySteer / 2f) * h;
				}
				for (int i = 0; i < myVisualWheels.Length; i++) {
						myVisualWheels [i].position = GetWheelPos (myColliderWheels [i], myVisualWheels [i]);
						myVisualWheels [i].rotation = myColliderWheels [i].transform.rotation;
						myVisualWheels [i].RotateAround (myVisualWheels [i].transform.position, myVisualWheels [i].transform.right, 20 * Time.deltaTime);
				}
				for (int i = 0; i < myTurnWheels.Length; i++) {
						myTurnWheels [i].localEulerAngles = new Vector3 (myTurnWheels [i].localEulerAngles.x, 90f + (mySteer * h), myTurnWheels [i].localEulerAngles.z);
				}
		}

		void AdjustVolumePitch ()
		{
		
				float tmpFloat = Mathf.Abs (myCurrentSpeed);
				myEngine.pitch = 1f + tmpFloat * 0.025f;
				if (myCurrentSpeed > 40f)
						myEngine.pitch = 1.15f + tmpFloat * 0.015f;
				if (myCurrentSpeed > 60f)
						myEngine.pitch = 1.25f + tmpFloat * 0.013f;
				if (myCurrentSpeed > 80f)
						myEngine.pitch = 1.5f + tmpFloat * 0.011f;
				if (myEngine.pitch < 1f)
						myEngine.pitch = 1f;
				if (myEngine.pitch > 2.3f)
						myEngine.pitch = 2.3f;
		}

		Vector3 GetWheelPos (WheelCollider myWheelColl, Transform myWheel)
		{
				RaycastHit hit;
				if (Physics.Raycast (myWheelColl.transform.position, -myWheelColl.transform.up, out hit, myWheelColl.suspensionDistance + myWheelColl.radius)) {
						myWheel.position = hit.point + myWheelColl.transform.up * myWheelColl.radius; 
				} else {
						myWheel.position = myWheelColl.transform.position - (myWheelColl.transform.up * myWheelColl.suspensionDistance);
				}
				return myWheel.position;
		}

		void EngineTorque ()
		{
				int revBrake = 0;
				if (v < -0.1 && myCurrentSpeed > 0.5f)
						revBrake = reverseBrakeAmount;
				else
						revBrake = 0;
				if (myCurrentSpeed < topSpeed && myCurrentSpeed > -(topSpeed / 4f)) {
						for (int i = 0; i < myEngineWheels.Length; i++) {
								myEngineWheels [i].motorTorque = ((myEngineTorque + revBrake) * -v) / myEngineWheels.Length;
						}
				} else {
						for (int i = 0; i < myEngineWheels.Length; i++) {
								myEngineWheels [i].motorTorque = 0f;
						}
				}
		}

		void DownwardForce ()
		{
				isGrounded = false;
				for (int i = 0; i < myColliderWheels.Length; i++) {
						if (myColliderWheels [i].isGrounded) {
								isGrounded = true;
								break;
						}
				}
				if (isGrounded) {		
						Vector3 downPressure = new Vector3 (0f, 0f, 0f);
						downPressure.y = -Mathf.Pow (rigidbody.velocity.magnitude, 1.2f) * downPressureFactor;
						downPressure.y = Mathf.Max (downPressure.y, -70f);
						rigidbody.AddForce (downPressure, ForceMode.Acceleration);
				}
		}

		public void ResetCar ()
		{
				reseting = true;
				count = 5f;
				rigidbody.velocity = Vector3.zero;
				foreach (WheelCollider wheelCol in myColliderWheels) {
						wheelCol.rigidbody.velocity = Vector3.zero;
				}
				Transform tempTrans = myNodeGraph.GetClosestWaypoint (transform.position);
				int newCurrNodeInt = 0;
				int.TryParse (tempTrans.gameObject.name, out newCurrNodeInt);
				currentNode = newCurrNodeInt;
				do {
						LayerMask mask = -9;
						RaycastHit hit;
						if (!Physics.SphereCast (tempTrans.position, myNodeGraph.detectionRange, transform.forward, out hit, 10, mask)) {
								transform.rotation = tempTrans.rotation;
								transform.position = tempTrans.position;
								count = 5f;
								reseting = false;
								break;
						} else {
								transform.rotation = tempTrans.rotation;
								transform.position = tempTrans.position + Vector3.up * 2.5f;
								count = 5f;
								reseting = false;
								break;
						}
				} while(reseting);
		
		}
}
