﻿
using Sandbox;
using SandboxEditor;
using Strafe.Players;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Strafe.Map;

[Library( "strafe_trigger_teleport" )]
[Display( Name = "Teleport Trigger" ), Category( "Triggers" ), Icon( "rotate_left" )]
[HammerEntity]
internal partial class StrafeTriggerTeleport : StrafeTrigger
{

	/// <summary>
	/// The entity specifying a location to which entities should be teleported to.
	/// </summary>
	[Property( "target", Title = "Remote Destination" )]
	public string TargetEntity { get; set; }

	/// <summary>
	/// If set, teleports the entity with an offset depending on where the entity was in the trigger teleport. Think world portals. Place the target entity accordingly.
	/// </summary>
	[Property( "teleport_relative", Title = "Teleport Relatively" )]
	[Net]
	public bool TeleportRelative { get; set; }

	/// <summary>
	/// If set, the teleported entity will not have it's velocity reset to 0.
	/// </summary>
	[Property( "keep_velocity", Title = "Keep Velocity" )]
	[Net]
	public bool KeepVelocity { get; set; }

	/// <summary>
	/// If set, the player's view angles will match the target entity's rotation
	/// </summary>
	[Property("set_viewangles", Title = "Apply ViewAngles" )]
	[Net]
	public bool SetViewAngles { get; set; }

	[Net]
	public Transform TargetTransform { get; set; }

	/// <summary>
	/// Fired when the trigger teleports an entity
	/// </summary>
	protected Output OnTriggered { get; set; }

	[Event.Entity.PostSpawn]
	private void OnEntityPostSpawn()
	{
		var Targetent = FindByName( TargetEntity );
		if ( Targetent.IsValid() )
		{
			TargetTransform = Targetent.Transform;
		}
	}

	public override void SimulatedStartTouch( StrafeController ctrl )
	{
		base.SimulatedStartTouch( ctrl );

		if ( !IsEnabled ) return;

		var tx = TargetTransform;
		if ( tx == default )
		{
			var ent = Entity.FindByName( TargetEntity );
			if ( ent.IsValid() )
			{
				tx = ent.Transform;
			}
		}

		Vector3 offset = Vector3.Zero;
		if ( TeleportRelative )
		{
			offset = ctrl.Position - Position;
		}

		if ( !KeepVelocity ) ctrl.Velocity = Vector3.Zero;

		// Fire the output, before actual teleportation so entity IO can do things like disable a trigger_teleport we are teleporting this entity into
		OnTriggered.Fire( ctrl.Pawn );

		ctrl.Position = tx.Position;
		ctrl.Position += offset;

		if ( SetViewAngles && ctrl.Pawn is StrafePlayer pl )
		{
			pl.SetViewAngles( tx.Rotation.Angles() );
		}
	}

}
