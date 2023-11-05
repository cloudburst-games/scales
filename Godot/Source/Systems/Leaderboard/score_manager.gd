extends Node


func _ready():
	pass

func wipe(ld_name = "main"):
	SilentWolf.Scores.wipe_leaderboard(ld_name)
