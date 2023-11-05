@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("AutoScrollLabel", "RichTextLabel", preload("AutoScrollLabel.cs"), preload("AutoScrollLabelIcon.png"))
	


func _exit_tree():
	remove_custom_type("AutoScrollLabel")
