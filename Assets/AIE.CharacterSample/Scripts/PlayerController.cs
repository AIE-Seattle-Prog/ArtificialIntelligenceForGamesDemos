using System;
using UnityEngine;
using UnityEngine.Serialization;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class PlayerController : MonoBehaviour
	{
		private Vector2 moveWish;
		private Vector2 lookWish;
		private const float _threshold = 0.01f;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		public CharacterMotor motor;
#if ENABLE_INPUT_SYSTEM
		public PlayerInput input;
#endif
		// camera
		[Header("Camera")]
		[SerializeField]
		private GameObject _mainCamera;
		private float _TargetYaw;
		private float _TargetPitch;

		public bool IsCurrentDeviceMouse
		{
			get
			{
				// TODO: Check how to detect if mouse is active
				return true;
			}
		}

		[Tooltip("The follow target set that the camera will follow")]
		public GameObject CameraTarget;

		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 70.0f;

		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -30.0f;

		[Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
		public float CameraAngleOverride = 0.0f;

		[Tooltip("For locking the camera position on all axis")]
		public bool LockCameraPosition = false;

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

#if ENABLE_INPUT_SYSTEM
		// to be called by 'Send Messages' on a co-located PlayerInput component
		
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
#endif

		public void MoveInput(Vector2 newMoveDirection)
		{
			moveWish = newMoveDirection;
		}

		public void LookInput(Vector2 newLookDirection)
		{
			lookWish = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			motor.JumpWish = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			motor.SprintWish = newSprintState;
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}

		private void ControlRotation()
		{
			// if there is an input and camera position is not fixed
			if (lookWish.sqrMagnitude >= _threshold && !LockCameraPosition)
			{
				//Don't multiply mouse input by Time.deltaTime;
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_TargetYaw += lookWish.x * deltaTimeMultiplier;
				_TargetPitch += lookWish.y * deltaTimeMultiplier;
			}

			// clamp our rotations so our values are limited 360 degrees
			_TargetYaw = ClampAngle(_TargetYaw, float.MinValue, float.MaxValue);
			_TargetPitch = ClampAngle(_TargetPitch, BottomClamp, TopClamp);

			// camera controller will follow this target
			CameraTarget.transform.rotation = Quaternion.Euler(_TargetPitch + CameraAngleOverride,
				_TargetYaw, 0.0f);
		}

		private void Awake()
		{
			_TargetYaw = CameraTarget.transform.rotation.eulerAngles.y;

			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Update()
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			float horz = Input.GetAxisRaw("Horizontal");
			float vert = Input.GetAxisRaw("Vertical");
			MoveInput(new Vector2(horz, vert));

			float lookX = Input.GetAxisRaw("Mouse X");
			float lookY = -Input.GetAxisRaw("Mouse Y");
			LookInput(new Vector2(lookX, lookY));

			if (Input.GetButtonDown("Jump"))
			{
				JumpInput(true);
			}

			SprintInput(Input.GetButton("Sprint"));
#endif

			Vector3 rotatedMoveWish = Quaternion.Euler(0.0f, _TargetYaw, 0.0f) * new Vector3(moveWish.x, 0, moveWish.y);
			motor.MoveWish = rotatedMoveWish;
		}

		private void LateUpdate()
		{
			ControlRotation();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}
	}

}