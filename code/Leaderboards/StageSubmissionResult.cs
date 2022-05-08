﻿
using Strafe.Players;

namespace Strafe.Leaderboards;

internal class StageSubmissionResult
{

	public int NewRank { get; set; }
	public int OldRank { get; set; }
	public float NewTime { get; set; }
	public float OldTime { get; set; }
	public TimerFrame Comparison { get; set; }

}
