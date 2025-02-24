﻿
using Sandbox;
using Sandbox.UI;

namespace Strafe.UI;

internal class UIEntity : HudEntity<RootPanel>
{

	public static UIEntity Current;

	public UIEntity()
	{
		Current = this;

		if ( IsServer ) return;

		RootPanel.StyleSheet.Load( "UI/Styles/_styles.scss" );

		Rebuild();
	}

	private void Rebuild()
	{
		if ( IsServer ) return;

		RootPanel.DeleteChildren();

		var hudElements = TypeLibrary.GetAttributes<HudAttribute>();
		foreach ( var element in hudElements )
		{
			var instance = TypeLibrary.Create<Panel>( element.TargetType );
			if ( instance == null ) continue;
			RootPanel.AddChild( instance );
		}

		RootPanel.AddChild<Panel>( "crosshair" );
	}

	[Event.Hotload]
	private void OnHotload()
	{
		Rebuild();
	}

	[ConCmd.Client( "strafe_toggle_hud" )]
	public static void ToggleHud()
	{
		Current.RootPanel.SetClass( "hidden", !Current.RootPanel.HasClass( "hidden" ) );
	}

}
