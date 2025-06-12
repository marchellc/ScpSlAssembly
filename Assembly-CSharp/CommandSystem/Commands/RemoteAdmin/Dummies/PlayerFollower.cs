using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin.Dummies;

public class PlayerFollower : MonoBehaviour
{
	private const float DefaultMaxDistance = 20f;

	private const float DefaultMinDistance = 1.75f;

	private const float DefaultSpeed = 30f;

	private ReferenceHub _hub;

	private ReferenceHub _hubToFollow;

	private float _maxDistance;

	private float _minDistance;

	private float _speed;

	public void Init(ReferenceHub playerToFollow, float maxDistance = 20f, float minDistance = 1.75f, float speed = 30f)
	{
		this._hub = base.GetComponent<ReferenceHub>();
		this._hubToFollow = playerToFollow;
		this._maxDistance = maxDistance;
		this._minDistance = minDistance;
		this._speed = speed;
	}

	private void Update()
	{
		if (!NetworkServer.active || this._hubToFollow == null || this._hub == null || !(this._hubToFollow.roleManager.CurrentRole is IFpcRole) || !(this._hub.roleManager.CurrentRole is IFpcRole { FpcModule: var fpcModule }))
		{
			Object.Destroy(this);
			return;
		}
		float num = Vector3.Distance(this._hubToFollow.transform.position, base.transform.position);
		if (num > this._maxDistance)
		{
			fpcModule.ServerOverridePosition(this._hubToFollow.transform.position);
		}
		else if (!(num < this._minDistance))
		{
			Vector3 position = base.transform.position;
			Vector3 dir = this._hubToFollow.transform.position - position;
			Vector3 vector = Time.deltaTime * this._speed * dir.normalized;
			fpcModule.Motor.ReceivedPosition = new RelativePosition(position + vector);
			fpcModule.MouseLook.LookAtDirection(dir);
		}
	}
}
