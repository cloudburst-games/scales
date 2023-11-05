@tool
extends EditorPlugin


func _enter_tree():
	add_custom_type("BasePanel", "Panel", preload("BasePanel.cs"), preload("BasePanelIcon.png"))
	


func _exit_tree():
	remove_custom_type("BasePanel")
