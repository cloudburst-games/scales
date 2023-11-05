@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("BaseTextureButton", "TextureButton", preload("BaseTextureButton.cs"), preload("BaseTextureButtonIcon.png"))
	


func _exit_tree():
	remove_custom_type("BaseTextureButton")
