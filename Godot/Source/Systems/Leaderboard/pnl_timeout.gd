extends Panel

var scene_to_hide

func _ready():
	visible = false

func start_timeout(scene):
	scene_to_hide = scene
	$TimerConnect.start()
	
func stop_timeout():
	scene_to_hide = null
	visible = false
	$TimerConnect.stop()

func _on_BtnClose_pressed():
	visible = false
	scene_to_hide.close_all()

func _on_TimerConnect_timeout():
	visible = true
	if scene_to_hide != null:
		scene_to_hide.on_timeout()
