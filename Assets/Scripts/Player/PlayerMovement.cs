using UnityEngine;

namespace LostKingdom
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        public Transform cameraTransform;
        [Header("Movement")]
        [Min(0f)] public float walkSpeed = 7f, runSpeed = 10f;
        [Min(0f)] public float acceleration = 28f;
        [Min(0f)] public float turnSmoothTime = .12f;
        [Header("Jump")]
        [Min(.1f)] public float jumpHeight = 1.6f;
        public float gravity = -24f;
        CharacterController controller;
        float verticalSpeed, turnVelocity, jumpBuffer, groundedGrace;
        Vector3 planarVelocity;
        int jumpsRemaining = 2;
        Transform leftArm, rightArm, leftLeg, rightLeg;
        bool wasGrounded;
        float landingPulse, doubleJumpPose;
        public bool IsGrounded => controller && controller.isGrounded;
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            leftArm = transform.Find("Left sleeve"); rightArm = transform.Find("Right sleeve");
            leftLeg = transform.Find("Left trouser leg"); rightLeg = transform.Find("Right trouser leg");
        }
        void Update()
        {
            if (!GameManager.Instance.IsPlaying) return;
            bool groundedBeforeMove = controller.isGrounded && HasFootSupport();
            if (groundedBeforeMove) { groundedGrace = .12f; jumpsRemaining = 2; } else groundedGrace -= Time.deltaTime;
            if (groundedBeforeMove && verticalSpeed < 0) verticalSpeed = -2f;
            Vector2 input = Vector2.ClampMagnitude(DemoInput.Move, 1);
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = forward * input.y + right * input.x;
            bool jumpPressed = DemoInput.Jump;
            if (jumpPressed) jumpBuffer = .15f;
            else jumpBuffer -= Time.deltaTime;
            if (direction.sqrMagnitude > .01f)
            {
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(transform.eulerAngles.y, angle, ref turnVelocity, turnSmoothTime), 0);
            }
            if (jumpBuffer > 0 && jumpsRemaining > 0 && (groundedGrace > 0 || jumpsRemaining < 2))
            {
                bool secondJump = jumpsRemaining == 1;
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity) * (secondJump ? 1.08f : 1f);
                jumpsRemaining--; jumpBuffer = 0; groundedGrace = 0;
                if (secondJump) { doubleJumpPose = .55f; GameManager.Instance.ui.Notify("DOUBLE JUMP", 1.2f); }
            }
            verticalSpeed += gravity * Time.deltaTime;
            Vector3 targetVelocity = direction * (DemoInput.Sprint ? runSpeed : walkSpeed);
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, acceleration * Time.deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
            if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            bool groundedNow = controller.isGrounded && HasFootSupport();
            if (!wasGrounded && groundedNow)
            {
                landingPulse = 1f;
            }
            landingPulse = Mathf.MoveTowards(landingPulse, 0, Time.deltaTime * 3.5f);
            doubleJumpPose = Mathf.MoveTowards(doubleJumpPose, 0, Time.deltaTime * 2.6f);
            AnimateLimbs(direction.magnitude, groundedNow);
            wasGrounded = groundedNow;
            if (transform.position.y < -10) GameManager.Instance.RespawnAtCheckpoint(this);
        }
        void AnimateLimbs(float inputMagnitude, bool grounded)
        {
            if (!leftArm) leftArm = transform.Find("Left sleeve");
            if (!rightArm) rightArm = transform.Find("Right sleeve");
            if (!leftLeg) leftLeg = transform.Find("Left trouser leg");
            if (!rightLeg) rightLeg = transform.Find("Right trouser leg");
            float phase = Time.time * (DemoInput.Sprint ? 11f : 7f);
            float stride = grounded ? Mathf.Sin(phase) * Mathf.Clamp01(inputMagnitude) * (DemoInput.Sprint ? 32f : 22f) : 0f;
            float jumpPose = !grounded ? (verticalSpeed > 0 ? -28f : 22f) : -landingPulse * 15f;
            PoseLimb(leftArm, -stride * .7f + jumpPose, doubleJumpPose * 28f, 12);
            PoseLimb(rightArm, stride * .7f + jumpPose, -doubleJumpPose * 28f, 12);
            PoseLimb(leftLeg, stride - doubleJumpPose * 18f, 0, 14);
            PoseLimb(rightLeg, -stride - doubleJumpPose * 18f, 0, 14);
        }
        static void PoseLimb(Transform limb, float xAngle, float zAngle, float smoothing)
        {
            if (!limb) return;
            Quaternion target = Quaternion.Euler(xAngle, 0, zAngle);
            limb.localRotation = Quaternion.Slerp(limb.localRotation, target, 1 - Mathf.Exp(-smoothing * Time.deltaTime));
        }
        bool HasFootSupport()
        {
            // CharacterController can report grounded while its capsule is only brushing a platform's side.
            // Require the feet center to have a solid surface directly underneath before resetting jumps or cancelling gravity.
            float bottom = controller.center.y - controller.height * .5f;
            Vector3 origin = transform.TransformPoint(new Vector3(controller.center.x, bottom + .22f, controller.center.z));
            return Physics.Raycast(origin, Vector3.down, out RaycastHit hit, .34f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.normal.y >= .65f;
        }
        public void Respawn(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position; controller.enabled = true; verticalSpeed = 0; planarVelocity = Vector3.zero; jumpsRemaining = 2;
        }
    }
}
