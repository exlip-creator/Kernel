using StarterAssets;
using UnityEngine;

/// <summary>
/// Удержание E: луч из камеры только для выбора блока. Перенос — по земле впереди робота
/// в направлении его <see cref="Transform.forward"/> (как третье лицо тащит ящик).
/// У удерживаемого Rigidbody на время переноса отключаются коллизии, чтобы CharacterController
/// не выталкивал игрока вверх при крупных объёмах.
/// </summary>
public class PhysicsBlockGrab : MonoBehaviour
{
	[SerializeField] private Camera playerCamera;
	[SerializeField] private Transform playerAnchor;
	[SerializeField] private CharacterController characterController;
	[SerializeField] private ThirdPersonController thirdPerson;

	[Header("Pick (camera ray)")]
	[SerializeField] private float maxGrabDistance = 4f;
	[SerializeField] private LayerMask draggableMask = ~0;

	[Header("Ground drag")]
	[SerializeField] private float dragForwardBase = 0.85f;
	[SerializeField] private float dragForwardBlockScale = 0.55f;
	[SerializeField] private float groundRaycastUp = 4f;
	[SerializeField] private float groundRaycastDown = 24f;
	[SerializeField] private LayerMask groundSnapMask = ~0;

	[Header("Input")]
	[SerializeField] private KeyCode grabKey = KeyCode.E;

	[Header("Release")]
	[SerializeField] private bool inheritCarryVelocity = true;
	[SerializeField] private float maxReleaseVelocity = 6f;
	[SerializeField] private bool horizontalCarryVelocityOnly = true;
	[SerializeField] private bool resetAngularVelocityOnRelease = true;

	private Rigidbody heldBody;
	private Vector3 prevHoldTarget;
	private Vector3 carryVelocity;
	private bool heldPrevDetectCollisions;

	private void Awake()
	{
		if (playerCamera == null)
			playerCamera = Camera.main;
		if (playerAnchor == null)
			playerAnchor = transform;
		if (characterController == null)
			characterController = GetComponent<CharacterController>();
		if (thirdPerson == null)
			thirdPerson = GetComponent<ThirdPersonController>();

		if (thirdPerson != null && groundSnapMask.value == 0)
			groundSnapMask = thirdPerson.GroundLayers;
	}

	private void Update()
	{
		if (heldBody != null)
		{
			if (!Input.GetKey(grabKey))
				Release();
		}
		else
		{
			if (Input.GetKeyDown(grabKey))
				TryGrab();
		}
	}

	private void FixedUpdate()
	{
		if (heldBody == null || playerAnchor == null)
			return;

		if (!Input.GetKey(grabKey))
			return;

		Vector3 target = ComputeGroundDragTarget();
		carryVelocity = (target - prevHoldTarget) / Time.fixedDeltaTime;
		prevHoldTarget = target;
		heldBody.MovePosition(target);
	}

	private void TryGrab()
	{
		if (playerCamera == null)
			playerCamera = Camera.main;
		if (playerCamera == null)
			return;

		Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		if (!Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, draggableMask, QueryTriggerInteraction.Ignore))
			return;

		Rigidbody rb = hit.collider.attachedRigidbody != null
			? hit.collider.attachedRigidbody
			: hit.collider.GetComponentInParent<Rigidbody>();
		if (rb == null)
			return;

		heldBody = rb;
		heldPrevDetectCollisions = heldBody.detectCollisions;
		heldBody.detectCollisions = false;
		heldBody.isKinematic = true;
		heldBody.useGravity = false;
		heldBody.linearVelocity = Vector3.zero;
		heldBody.angularVelocity = Vector3.zero;

		Vector3 target = ComputeGroundDragTarget();
		heldBody.position = target;
		prevHoldTarget = target;
		carryVelocity = Vector3.zero;
	}

	private void Release()
	{
		if (heldBody == null)
			return;

		heldBody.detectCollisions = heldPrevDetectCollisions;
		heldBody.isKinematic = false;
		heldBody.useGravity = true;

		if (inheritCarryVelocity)
		{
			Vector3 v = carryVelocity;
			if (horizontalCarryVelocityOnly)
				v.y = 0f;
			heldBody.linearVelocity = Vector3.ClampMagnitude(v, maxReleaseVelocity);
		}

		if (resetAngularVelocityOnRelease)
			heldBody.angularVelocity = Vector3.zero;

		heldBody = null;
	}

	/// <summary>Точка на земле впереди робота, нижняя грань блока на поверхности.</summary>
	private Vector3 ComputeGroundDragTarget()
	{
		Vector3 flatForward = Vector3.ProjectOnPlane(playerAnchor.forward, Vector3.up);
		if (flatForward.sqrMagnitude < 1e-6f)
			flatForward = Vector3.forward;
		flatForward.Normalize();

		TryGetWorldBounds(heldBody, out Bounds bounds);
		float halfHz = Mathf.Max(bounds.extents.x, bounds.extents.z);
		float forwardDist = dragForwardBase + dragForwardBlockScale * halfHz;
		if (characterController != null)
			forwardDist += characterController.radius + characterController.skinWidth + 0.05f;

		Vector3 anchor = playerAnchor.position + flatForward * forwardDist;

		Vector3 rayOrigin = anchor + Vector3.up * groundRaycastUp;

		if (!TryRaycastGround(rayOrigin, heldBody, out RaycastHit groundHit))
		{
			float lift = heldBody.position.y - bounds.min.y;
			float fallbackY = playerAnchor.position.y + lift + 0.05f;
			return new Vector3(anchor.x, fallbackY, anchor.z);
		}

		float bottomDelta = groundHit.point.y - bounds.min.y;
		Vector3 pos = heldBody.position;
		pos.y += bottomDelta;
		pos.x = anchor.x;
		pos.z = anchor.z;
		return pos;
	}

	private bool TryRaycastGround(Vector3 origin, Rigidbody ignoreRb, out RaycastHit bestHit)
	{
		const int maxHits = 16;
		RaycastHit[] hits = new RaycastHit[maxHits];
		Ray ray = new Ray(origin, Vector3.down);
		int count = Physics.RaycastNonAlloc(ray, hits, groundRaycastDown, groundSnapMask, QueryTriggerInteraction.Ignore);
		if (count <= 0)
		{
			bestHit = default;
			return false;
		}

		int best = -1;
		float bestDist = float.MaxValue;
		for (int i = 0; i < count; i++)
		{
			Collider c = hits[i].collider;
			if (c == null)
				continue;
			if (ignoreRb != null && c.attachedRigidbody == ignoreRb)
				continue;
			if (ignoreRb != null && c.transform.root == ignoreRb.transform)
				continue;

			if (hits[i].distance < bestDist)
			{
				bestDist = hits[i].distance;
				best = i;
			}
		}

		if (best < 0)
		{
			bestHit = default;
			return false;
		}

		bestHit = hits[best];
		return true;
	}

	private static bool TryGetWorldBounds(Rigidbody rb, out Bounds merged)
	{
		if (rb == null)
		{
			merged = default;
			return false;
		}

		Collider[] colliders = rb.GetComponentsInChildren<Collider>();
		if (colliders.Length == 0)
		{
			merged = new Bounds(rb.worldCenterOfMass, Vector3.one * 0.5f);
			return false;
		}

		merged = colliders[0].bounds;
		for (int i = 1; i < colliders.Length; i++)
			merged.Encapsulate(colliders[i].bounds);
		return true;
	}
}
