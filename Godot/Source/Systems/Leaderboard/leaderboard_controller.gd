extends Node

var pnl_scoreunit_scn = preload("res://Source/Systems/Leaderboard/pnl_scoreunit.tscn")
@onready
var pnl_leaderboard = $".."
@onready
var loading_sprite = $"../loading_sprite"
@onready
var anim = $"../anim"
@onready
var pnl_timeout = $"../pnl_timeout"
@onready
var pnl_scores = $"../pnl_scores"

var max_scores_to_show = 10
var timeout = false
var current_ldboard_name = "main"

func _ready():
	pnl_leaderboard.visible = false
	loading_sprite.visible = false
	show_and_refresh_board(10)

func show_and_refresh_board(max_scores, ldboard_name = "main", ad = false):
	show_board(ad)
	refresh_board(max_scores, ldboard_name)

func refresh_board(max_scores, ldboard_name = "main"):
	timeout = false
	max_scores_to_show = max_scores
	current_ldboard_name = ldboard_name
	clear_board()
	loading_sprite.visible = true
	anim.play("loading")
	pnl_timeout.start_timeout(self)
	var _sw_result: Dictionary = await SilentWolf.Scores.get_scores().sw_get_scores_complete
	#await SilentWolf.Scores.get_high_scores(max_scores, ldboard_name).sw_scores_received
	if timeout:
		return
	pnl_timeout.stop_timeout()
	loading_sprite.visible = false
	var pnl_size = pnl_scores.size.y
	var increment = round(pnl_size/max_scores)
	var total_scores = 0
	for n in range(max_scores):
		if SilentWolf.Scores.scores.size() < n+1:
			return
		var item = SilentWolf.Scores.scores[n]
		var pnl_scoreunit = pnl_scoreunit_scn.instantiate()
		$"../pnl_scores/VBoxContainer".add_child(pnl_scoreunit)
		total_scores = n
#		pnl_scoreunit.position.y = increment*n
#		pnl_scoreunit.position.x = 0
#		pnl_scoreunit.size = Vector2(pnl_scores.size.x,increment)
		pnl_scoreunit.get_node("HBoxContainer").size = Vector2(pnl_scoreunit.size.x - 30, pnl_scoreunit.size.y)
#		pnl_scoreunit.get_node("HBoxContainer").position.x = 15
		pnl_scoreunit.get_node("HBoxContainer/lbl_name").text = item["player_name"]
		pnl_scoreunit.get_node("HBoxContainer/lbl_pos").text = str(n+1)+"."
		pnl_scoreunit.get_node("HBoxContainer/lbl_score").text = str(item["score"])
		if item.has("metadata"):
			for meta_item in item["metadata"]:
				var meta_label = pnl_scoreunit.get_node("HBoxContainer/lbl_name").duplicate()
				pnl_scoreunit.get_node("HBoxContainer").add_child(meta_label)
				meta_label.text = str(item["metadata"][meta_item])
				if meta_item == "Player2Name":
					meta_label.name = "lbl_p2name"

			var p1_pos = pnl_scoreunit.get_node("HBoxContainer/lbl_name").get_index()
			if pnl_scoreunit.get_node("HBoxContainer").has_node("lbl_p2name"):
				pnl_scoreunit.get_node("HBoxContainer").move_child(pnl_scoreunit.get_node("HBoxContainer/lbl_p2name"), p1_pos+1)
	
	$"../pnl_scores/VBoxContainer".size.y = min(increment*(total_scores+1), pnl_scores.size.y)
		# move the score to the end of the list
#		pnl_scoreunit.get_node("HBoxContainer").move_child(pnl_scoreunit.get_node("HBoxContainer/lbl_score"),pnl_scoreunit.get_node("HBoxContainer").get_child_count()-1)

func show_board(ad):
	pnl_leaderboard.visible = true
	if OS.get_name() == "Android" or OS.get_name() == "iOS":
		if ad == true:
			#$AdHandler.show_banner()
			#$AdHandler.show_interstitial()# show_interstitial
			pass

func _on_btn_close_pressed():
	close_all()
	
func on_timeout():
	timeout = true
	for node in get_children():
		if node is Button:
			node.disabled = true

func close_all():
	pnl_leaderboard.visible = false
	if OS.get_name() == "Android" or OS.get_name() == "iOS":
		#$AdHandler.hide_banner()
		pass
	loading_sprite.visible = false
	if anim.is_playing():
		anim.seek(0, true)
	anim.stop(true)
	for node in get_children():
		if node is Button:
			node.disabled = false

func clear_board():
	for node in pnl_scores.get_children():
		if node.name != "VBoxContainer":
			node.queue_free()
	
func _on_btn_refresh_pressed():
	refresh_board(max_scores_to_show, current_ldboard_name)
