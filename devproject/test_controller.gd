extends Node2D

# Run with `-- --test` to sweep the listener around the scene for a fixed duration and then quit - used by the vaudio package script's headless tests
const TEST_DURATION_SECONDS := 5.0
const TEST_PASSED_MARKER := "[devproject] Test passed"

@onready var listener: Node2D = $Listener
@onready var source: Node2D = $VASource

var test_mode := OS.get_cmdline_user_args().has("--test")
var test_elapsed := 0.0

func _process(delta: float) -> void:
	if test_mode:
		_process_test(delta)
		return

	if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		source.global_position = get_global_mouse_position()
	else:
		listener.global_position = get_global_mouse_position()

func _process_test(delta: float) -> void:
	test_elapsed += delta
	listener.global_position = Vector2(cos(test_elapsed * 2.0) * 450.0, sin(test_elapsed * 3.0) * 250.0)

	if test_elapsed >= TEST_DURATION_SECONDS:
		print("%s after %d frames" % [TEST_PASSED_MARKER, Engine.get_process_frames()])
		get_tree().quit()
