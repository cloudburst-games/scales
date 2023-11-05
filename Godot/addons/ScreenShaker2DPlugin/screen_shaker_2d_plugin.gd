@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("ScreenShaker2D", "Node", preload("ScreenShaker2D.cs"), preload("ScreenShaker2DIcon.png"))
	


func _exit_tree():
	remove_custom_type("ScreenShaker2D")
