@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("SceneTransition", "Node", preload("SceneTransition.cs"), preload("SceneTransitionIcon.png"))
	


func _exit_tree():
	remove_custom_type("SceneTransition")
