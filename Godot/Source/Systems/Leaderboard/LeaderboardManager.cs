// // ScoreManagerMono script. This wraps a ScoreManager UI written in gdscript that utilises SilentWolf Leaderboard addon
// // To use, change the config in score_unit.gd, and instance this node in a scene.
// // Don't forget to customise the UI elements in this node and in pnl_scoreunit
// // If you expect the positions / high scores to reach higher numbers, adjust the label min_rect_size.x

// // Can request to submit a high score - this checks the position of the high score and if it is within the maxScores..
// // .. it submits the score (with loading animations that can be customised), and optionally then displays the leaderboard:
// // StartScoreSaver(highScore:30, maxScores:10);
// // Can just directly show the leaderboard (up to x max scores):
// // ShowAndRefreshLeaderboard(maxScores:10);
// // Or can refresh and show the leaderboard separately:
// // RefreshLeaderboard(maxScores:10)
// // ShowLeaderboard()
// // To wipe the leaderboard:
// // GetNode("score_manager").CallDeferred("wipe");

// // See below for optional arguments
// // Enable player2 by passing "Player2Name" in the metaData dict as the KEY (the value doesnt matter)

// // Signals can be connected when this is instanced, for UI transition purposes (e.g. on high_score play victory sound, on score saver finished go to end menu)

// // NOTE: in html5 as of 3.2.3, this addon FAILS if the game is paused while the leaderboard is active...
// // ... instead consider "pausing" individual nodes

using Godot;
using System;
using Godot.Collections;

public partial class LeaderboardManager : Control
{
    public override void _Ready()
    {

        // GetNode("score_manager").CallDeferred("wipe", "multi");
        // GetNode("score_manager").CallDeferred("wipe", "main");
        //StartScoreSaver(20, 10);
    }

    public void StartScoreSaver(int highScore, int maxScores, string ldBoardName = "main", Dictionary<string, string> metaData = null)
    {
        GetNode("score_saver").CallDeferred("start", highScore, maxScores, ldBoardName, metaData);
    }

    public void ShowAndRefreshLeaderboard(int maxScores, string ldboardName = "main", bool ad = false)
    {
        GetNode("PnlLeaderboard/leaderboard_controller").CallDeferred("show_and_refresh_board", maxScores, ldboardName, ad);
    }

    public void ShowLeaderboard(bool ad = false)
    {
        GetNode("PnlLeaderboard/leaderboard_controller").CallDeferred("show_board", ad);
    }
    public void RefreshLeaderboard(int maxScores, string ldboardName = "main")
    {
        GetNode("PnlLeaderboard/leaderboard_controller").CallDeferred("refresh_board", maxScores, ldboardName);
    }

    // public void ToggleAd(bool visible) // TODO!
    // {
    //     GetNode("PnlLeaderboard/leaderboard_controller/AdHandler").CallDeferred(visible ? "show_banner" : "hide_banner");
    // }

    public void CloseLeaderboardAll()
    {
        GetNode("PnlLeaderboard/leaderboard_controller").CallDeferred("close_all");
    }
}
// _scoreManager.ShowAndRefreshLeaderboard(_maxScoresLeaderboard, _leaderBoardMode);
