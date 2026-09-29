extends Node2D

# Root script of both test_scene.tscn and reverb_room.tscn. Run with `-- --test` to run the scenarios in res://scenarios and then quit - see scenarios/lib/runner.gd for the options
const RUNNER := "res://scenarios/lib/runner.gd"
const VA_ADAPTER := "res://scenarios/lib/va.gd"

@onready var listener: Node2D = $Listener
@onready var source: Node2D = $VASource
@onready var world: Node = $VAWorld

var test_mode := OS.get_cmdline_user_args().has("--test")

# Runs before VAWorld._enter_tree (parents enter the tree first), which creates the world and would otherwise show the debug window straight away
func _enter_tree() -> void:
	if test_mode and not OS.get_cmdline_user_args().has("--debugwindow"):
		load(VA_ADAPTER).set_value($VAWorld, "rendering_enabled", false)

func _ready() -> void:
	# The runner outlives scene changes, so only the first scene starts it
	if test_mode and not get_tree().root.has_node("TestRunner"):
		var runner: Node = load(RUNNER).new()
		runner.name = "TestRunner"
		get_tree().root.add_child.call_deferred(runner)

func _process(_delta: float) -> void:
	if test_mode:
		return

	if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		source.global_position = get_global_mouse_position()
	else:
		listener.global_position = get_global_mouse_position()
