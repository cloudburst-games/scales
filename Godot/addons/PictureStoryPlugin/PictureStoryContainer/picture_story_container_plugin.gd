@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("PictureStoryContainer", "Control", preload("PictureStoryContainer.cs"), preload("PictureStoryContainerIcon.png"))
	


func _exit_tree():
	remove_custom_type("PictureStoryContainer")
