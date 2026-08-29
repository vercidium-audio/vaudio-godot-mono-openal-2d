@tool
extends EditorPlugin

const VAEmitter = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAEmitter.cs")
const VAListener = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAListener.cs")
const ALSource = preload("res://addons/vaudio-godot-mono-openal-2d/common/openal/nodes/ALSource.cs")
const ALSource2D = preload("res://addons/vaudio-godot-mono-openal-2d/openal/nodes/ALSource2D.cs")
const VAStreamSource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAStreamSource.cs")
const VAInputStreamSource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAInputStreamSource.cs")
const VANetworkedStreamSource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VANetworkedStreamSource.cs")
const VASource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VASource.cs")
const VASourceLeech = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VASourceLeech.cs")

const ALSourceRelative = preload("res://addons/vaudio-godot-mono-openal-2d/openal/nodes/ALSourceRelative.cs")
const VASourceRelative = preload("res://addons/vaudio-godot-mono-openal-2d/nodes/VASourceRelative.cs")
const VASourceAmbient = preload("res://addons/vaudio-godot-mono-openal-2d/nodes/VASourceAmbient.cs")
const VAVisualisation = preload("res://addons/vaudio-godot-mono-openal-2d/nodes/VAVisualisation.cs")
const VAWorld = preload("res://addons/vaudio-godot-mono-openal-2d/world/VAWorld.cs")
const VADefaultMaterial = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VADefaultMaterial.cs")
const VACustomMaterial = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VACustomMaterial.cs")

const VAMaterialInspectorPlugin = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VAMaterialInspectorPlugin.gd")
const VAMaterialPropertiesInspectorPlugin = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VAMaterialPropertiesInspectorPlugin.gd")
const VAConversionContextMenuPlugin = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VAConversionContextMenuPlugin.gd")
const VADebuggerPlugin = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VADebuggerPlugin.gd")
const VADebuggerSingleton = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VADebuggerSingleton.gd")
const VADeviceRefreshInspectorPlugin = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VADeviceRefreshInspectorPlugin.gd")
const VAInspectorTooltipPlugin = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VAInspectorTooltipPlugin.gd")

var material_inspector_plugin
var material_properties_inspector_plugin
var conversion_context_menu_plugin
var debugger_plugin
var debugger_singleton
var device_refresh_inspector_plugin
var inspector_tooltip_plugin

const DEBUGGER_PLUGIN_SINGLETON_NAME = "VADebuggerPlugin"

# "audio/vaudio/*" Project Settings
const DEFAULT_DEVICE_LABEL = "System Default"

func _enter_tree():
	var icon = preload("res://addons/vaudio-godot-mono-openal-2d/icons/vercidium.svg")
	var iconAL = preload("res://addons/vaudio-godot-mono-openal-2d/icons/vercidium_al.svg")

	add_custom_type("VAEmitter", "Node2D", VAEmitter, icon)
	add_custom_type("VAListener", "Node2D", VAListener, icon)

	add_custom_type("ALSource", "Node2D", ALSource, iconAL)
	add_custom_type("ALSource2D", "Node2D", ALSource2D, iconAL)
	add_custom_type("VAStreamSource", "Node2D", VAStreamSource, iconAL)
	add_custom_type("VAInputStreamSource", "Node2D", VAInputStreamSource, iconAL)
	add_custom_type("VANetworkedStreamSource", "Node2D", VANetworkedStreamSource, iconAL)
	add_custom_type("VASource", "Node2D", VASource, iconAL)
	add_custom_type("VASourceLeech", "Node2D", VASourceLeech, iconAL)

	add_custom_type("ALSourceRelative", "Node2D", ALSourceRelative, iconAL)
	add_custom_type("VASourceRelative", "Node2D", VASourceRelative, iconAL)
	add_custom_type("VASourceAmbient", "Node2D", VASourceAmbient, iconAL)

	add_custom_type("VAVisualisation", "Node2D", VAVisualisation, icon)
	add_custom_type("VAWorld", "Node2D", VAWorld, icon)
	add_custom_type("VADefaultMaterial", "Node", VADefaultMaterial, icon)
	add_custom_type("VACustomMaterial", "Node", VACustomMaterial, icon)

	debugger_plugin = VADebuggerPlugin.new()
	add_debugger_plugin(debugger_plugin)

	debugger_singleton = VADebuggerSingleton.new(debugger_plugin)
	Engine.register_singleton(DEBUGGER_PLUGIN_SINGLETON_NAME, debugger_singleton)

	material_inspector_plugin = VAMaterialInspectorPlugin.new()
	material_inspector_plugin.set_debugger_plugin(debugger_plugin)
	add_inspector_plugin(material_inspector_plugin)

	material_properties_inspector_plugin = VAMaterialPropertiesInspectorPlugin.new()
	material_properties_inspector_plugin.set_debugger_plugin(debugger_plugin)
	add_inspector_plugin(material_properties_inspector_plugin)

	device_refresh_inspector_plugin = VADeviceRefreshInspectorPlugin.new()
	add_inspector_plugin(device_refresh_inspector_plugin)

	inspector_tooltip_plugin = VAInspectorTooltipPlugin.new()
	add_inspector_plugin(inspector_tooltip_plugin)

	conversion_context_menu_plugin = VAConversionContextMenuPlugin.new()
	add_context_menu_plugin(EditorContextMenuPlugin.CONTEXT_SLOT_SCENE_TREE, conversion_context_menu_plugin)

	# Register audio/vaudio/* Project Settings
	_register_project_settings()

	print("[vaudio-godot-mono-openal-2d] Vercidium Audio (vaudio) plugin enabled")

func _exit_tree():
	remove_custom_type("VAEmitter")
	remove_custom_type("VAListener")
	remove_custom_type("ALSource")
	remove_custom_type("ALSource2D")
	remove_custom_type("VAStreamSource")
	remove_custom_type("VAInputStreamSource")
	remove_custom_type("VANetworkedStreamSource")
	remove_custom_type("VASource")
	remove_custom_type("VASourceLeech")
	remove_custom_type("ALSourceRelative")
	remove_custom_type("VASourceRelative")
	remove_custom_type("VASourceAmbient")
	remove_custom_type("VAVisualisation")
	remove_custom_type("VAWorld")
	remove_custom_type("VADefaultMaterial")
	remove_custom_type("VACustomMaterial")

	if material_inspector_plugin:
		remove_inspector_plugin(material_inspector_plugin)
		material_inspector_plugin = null

	if material_properties_inspector_plugin:
		remove_inspector_plugin(material_properties_inspector_plugin)
		material_properties_inspector_plugin = null

	if device_refresh_inspector_plugin:
		remove_inspector_plugin(device_refresh_inspector_plugin)
		device_refresh_inspector_plugin = null

	if inspector_tooltip_plugin:
		remove_inspector_plugin(inspector_tooltip_plugin)
		inspector_tooltip_plugin = null

	if Engine.has_singleton(DEBUGGER_PLUGIN_SINGLETON_NAME):
		Engine.unregister_singleton(DEBUGGER_PLUGIN_SINGLETON_NAME)

	if debugger_singleton:
		# Plain Object from .new() - not RefCounted, so it must be freed explicitly.
		debugger_singleton.free()
		debugger_singleton = null

	if debugger_plugin:
		remove_debugger_plugin(debugger_plugin)
		debugger_plugin = null

	if conversion_context_menu_plugin:
		remove_context_menu_plugin(conversion_context_menu_plugin)
		conversion_context_menu_plugin = null

	print("Vercidium Audio (vaudio-godot-mono-openal-2d) plugin disabled")

func _register_project_settings():
	if not ProjectSettings.has_setting("audio/vaudio/output_device"):
		ProjectSettings.set_setting("audio/vaudio/output_device", DEFAULT_DEVICE_LABEL)

	ProjectSettings.set_initial_value("audio/vaudio/output_device", DEFAULT_DEVICE_LABEL)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/output_device",
		"type": TYPE_STRING,
		"hint": PROPERTY_HINT_ENUM,
		"hint_string": DEFAULT_DEVICE_LABEL,
	})

	# max_reverb_sends: dev-only setting (not end-user-facing), default 1
	if not ProjectSettings.has_setting("audio/vaudio/max_reverb_sends"):
		ProjectSettings.set_setting("audio/vaudio/max_reverb_sends", 1)

	ProjectSettings.set_initial_value("audio/vaudio/max_reverb_sends", 1)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/max_reverb_sends",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_RANGE,
		"hint_string": "1,16,or_greater",
	})

	# sample_rate: 0 means "driver default" - never shown to the user as 0.
	if not ProjectSettings.has_setting("audio/vaudio/sample_rate"):
		ProjectSettings.set_setting("audio/vaudio/sample_rate", 0)

	ProjectSettings.set_initial_value("audio/vaudio/sample_rate", 0)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/sample_rate",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_ENUM,
		"hint_string": "System Default:0,22050,44100,48000,96000",
	})

	# hrtf_enabled: default true
	if not ProjectSettings.has_setting("audio/vaudio/hrtf_enabled"):
		ProjectSettings.set_setting("audio/vaudio/hrtf_enabled", true)

	ProjectSettings.set_initial_value("audio/vaudio/hrtf_enabled", true)

	# max_mono_sources/max_stereo_sources: project-level settings set by the developer, matching the native Godot plugin's register_types.cpp
	# read once at device-open time, can't be changed at runtime.
	if not ProjectSettings.has_setting("audio/vaudio/max_mono_sources"):
		ProjectSettings.set_setting("audio/vaudio/max_mono_sources", 16)

	ProjectSettings.set_initial_value("audio/vaudio/max_mono_sources", 16)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/max_mono_sources",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_RANGE,
		"hint_string": "0,256,or_greater",
	})

	if not ProjectSettings.has_setting("audio/vaudio/max_stereo_sources"):
		ProjectSettings.set_setting("audio/vaudio/max_stereo_sources", 240)

	ProjectSettings.set_initial_value("audio/vaudio/max_stereo_sources", 240)

	ProjectSettings.add_property_info({
		"name": "audio/vaudio/max_stereo_sources",
		"type": TYPE_INT,
		"hint": PROPERTY_HINT_RANGE,
		"hint_string": "0,256,or_greater",
	})