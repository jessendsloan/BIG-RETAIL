using System;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// Moves an NPC through a small world-space waypoint path and keeps its
    /// visual facing and walk animation synchronized with actual movement.
    /// Destination choice and pathfinding remain outside the character rig.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcPathFollower : MonoBehaviour
    {
        private static readonly int SpeedParameter =
            Animator.StringToHash("Speed");

        [Header("Prototype Path")]
        [SerializeField]
        private Vector3[] waypoints = Array.Empty<Vector3>();

        [SerializeField, Min(0f)]
        private float movementSpeed = 1.2f;

        [SerializeField, Min(0.0001f)]
        private float arrivalDistance = 0.02f;

        [SerializeField, Min(0.0001f)]
        private float walkAnimationMetersPerSecond = 1.2f;

        [SerializeField]
        private bool playOnEnable;

        [SerializeField]
        private bool loopPath;

        private Animator animator;
        private NpcCutoutRig cutoutRig;
        private bool hasSpeedParameter;
        private int waypointIndex;
        private bool isMoving;
        private Vector3 velocity;

        public float MovementSpeed => movementSpeed;
        public float WalkAnimationMetersPerSecond => walkAnimationMetersPerSecond;
        public float ArrivalDistance => arrivalDistance;
        public bool IsMoving => isMoving;
        public int CurrentWaypointIndex => waypointIndex;
        public Vector3 Velocity => velocity;

        private void Awake()
        {
            CachePresentationReferences();
        }

        private void OnEnable()
        {
            CachePresentationReferences();
            isMoving = playOnEnable && waypoints != null && waypoints.Length > 0;
            if (!isMoving)
            {
                ApplyPresentation(0f);
            }
        }

        private void OnDisable()
        {
            isMoving = false;
            velocity = Vector3.zero;
            ApplyPresentation(0f);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances the path by a supplied timestep. The explicit timestep
        /// makes this deterministic in EditMode tests and headless previews.
        /// </summary>
        public void Tick(float deltaTime)
        {
            CachePresentationReferences();

            if (!isMoving || deltaTime <= 0f || waypoints == null || waypoints.Length == 0)
            {
                velocity = Vector3.zero;
                ApplyPresentation(0f);
                return;
            }

            if (movementSpeed <= 0f)
            {
                Stop();
                return;
            }

            Vector3 previousPosition = transform.position;
            bool reachedEnd = AdvanceTowardsTarget(deltaTime);
            velocity = (transform.position - previousPosition) / deltaTime;

            float actualSpeed = velocity.magnitude;
            if (actualSpeed > 0.0001f)
            {
                SetFacingFromVelocity(velocity);
            }

            ApplyPresentation(actualSpeed);

            if (reachedEnd)
            {
                Stop();
            }
        }

        /// <summary>
        /// Replaces the world-space path and optionally starts walking from
        /// the first waypoint.
        /// </summary>
        public void SetPath(Vector3[] worldWaypoints, bool startImmediately = true)
        {
            waypoints = worldWaypoints == null
                ? Array.Empty<Vector3>()
                : (Vector3[])worldWaypoints.Clone();
            waypointIndex = 0;
            velocity = Vector3.zero;
            isMoving = startImmediately && waypoints.Length > 0;
            ApplyPresentation(0f);
        }

        public void Stop()
        {
            isMoving = false;
            velocity = Vector3.zero;
            ApplyPresentation(0f);
        }

        /// <summary>
        /// Sets the small prototype profile used by generated characters.
        /// </summary>
        public void ConfigurePrototype(float newMovementSpeed, float newArrivalDistance, float newWalkAnimationMetersPerSecond)
        {
            movementSpeed = Mathf.Max(0f, newMovementSpeed);
            arrivalDistance = Mathf.Max(0.0001f, newArrivalDistance);
            walkAnimationMetersPerSecond = Mathf.Max(0.0001f, newWalkAnimationMetersPerSecond);
        }

        private bool AdvanceTowardsTarget(float deltaTime)
        {
            float remainingStep = movementSpeed * deltaTime;
            int safetyCount = waypoints.Length + 1;

            while (safetyCount-- > 0)
            {
                Vector3 target = waypoints[waypointIndex];
                Vector3 offset = target - transform.position;
                float distance = offset.magnitude;

                if (distance <= arrivalDistance)
                {
                    transform.position = target;
                    if (!AdvanceWaypoint())
                    {
                        return true;
                    }
                    continue;
                }

                float travelDistance = Mathf.Min(remainingStep, distance);
                transform.position += offset / distance * travelDistance;

                if (travelDistance >= distance && !AdvanceWaypoint())
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        private bool AdvanceWaypoint()
        {
            waypointIndex++;

            if (waypointIndex < waypoints.Length)
            {
                return true;
            }

            if (loopPath && waypoints.Length > 0)
            {
                waypointIndex = 0;
                return true;
            }

            waypointIndex = waypoints.Length - 1;
            return false;
        }

        private void SetFacingFromVelocity(Vector3 movementVelocity)
        {
            if (cutoutRig == null)
            {
                return;
            }

            bool east = movementVelocity.x >= 0f;
            bool south = movementVelocity.z >= 0f;
            NpcFacing facing = east
                ? (south ? NpcFacing.SouthEast : NpcFacing.NorthEast)
                : (south ? NpcFacing.SouthWest : NpcFacing.NorthWest);

            if (cutoutRig.Facing != facing)
            {
                cutoutRig.SetFacing(facing);
            }
        }

        private void ApplyPresentation(float actualSpeed)
        {
            if (animator == null)
            {
                return;
            }

            if (hasSpeedParameter)
            {
                animator.SetFloat(SpeedParameter, actualSpeed);
            }

            animator.speed = actualSpeed > 0.0001f
                ? actualSpeed / walkAnimationMetersPerSecond
                : 1f;
        }

        private void CachePresentationReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                hasSpeedParameter = animator != null && HasSpeedParameter(animator);
            }

            if (cutoutRig == null)
            {
                cutoutRig = GetComponent<NpcCutoutRig>();
            }
        }

        private static bool HasSpeedParameter(Animator targetAnimator)
        {
            AnimatorControllerParameter[] parameters = targetAnimator.parameters;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].nameHash == SpeedParameter)
                {
                    return parameters[index].type == AnimatorControllerParameterType.Float;
                }
            }

            return false;
        }
    }
}
