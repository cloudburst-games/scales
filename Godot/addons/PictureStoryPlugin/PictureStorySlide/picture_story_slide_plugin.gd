@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("PictureStorySlide", "AnimationPlayer", preload("PictureStorySlide.cs"), preload("PictureStorySlideIcon.png"))
	


func _exit_tree():
	remove_custom_type("PictureStorySlide")
