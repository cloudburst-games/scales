@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("Cam2DTopDown", "Camera2D", preload("Cam2DTopDown.cs"), preload("Cam2DTopDownIcon.png"))
	


func _exit_tree():
	remove_custom_type("Cam2DTopDown")
