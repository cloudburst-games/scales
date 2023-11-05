@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("AudioContainer", "Node", preload("AudioContainer.cs"), preload("AudioContainerIcon.png"))
	


func _exit_tree():
	remove_custom_type("AudioContainer")
