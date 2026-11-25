@tool
extends EditorPlugin

const VAEmitter = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAEmitter.cs")
const VAListener = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAListener.cs")
const VAPlayer = preload("res://addons/vaudio-godot-mono-openal-2d/common/player/VAPlayer.cs")
const VAPlayer2D = preload("res://addons/vaudio-godot-mono-openal-2d/player/VAPlayer2D.cs")
const VAStreamSource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAStreamSource.cs")
const VAInputStreamSource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VAInputStreamSource.cs")
const VANetworkedStreamSource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VANetworkedStreamSource.cs")
const VASource = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VASource.cs")
const VASourceLeech = preload("res://addons/vaudio-godot-mono-openal-2d/common/nodes/VASourceLeech.cs")

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
const VAHRTFExportPlugin = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VAHRTFExportPlugin.gd")
const VAProjectSettings = preload("res://addons/vaudio-godot-mono-openal-2d/common/editor/VAProjectSettings.gd")

var material_inspector_plugin
var material_properties_inspector_plugin
var conversion_context_menu_plugin
var debugger_plugin
var debugger_singleton
var device_refresh_inspector_plugin
var inspector_tooltip_plugin
var hrtf_export_plugin

const DEBUGGER_PLUGIN_SINGLETON_NAME = "VADebuggerPlugin"

func _enter_tree():
	var icon = preload("res://addons/vaudio-godot-mono-openal-2d/icons/vercidium.svg")
	var iconPlayer = preload("res://addons/vaudio-godot-mono-openal-2d/icons/vercidium_player.svg")

	add_custom_type("VAEmitter", "Node2D", VAEmitter, icon)
	add_custom_type("VAListener", "Node2D", VAListener, icon)

	add_custom_type("VAPlayer", "Node2D", VAPlayer, iconPlayer)
	add_custom_type("VAPlayer2D", "Node2D", VAPlayer2D, iconPlayer)
	add_custom_type("VAStreamSource", "Node2D", VAStreamSource, iconPlayer)
	add_custom_type("VAInputStreamSource", "Node2D", VAInputStreamSource, iconPlayer)
	add_custom_type("VANetworkedStreamSource", "Node2D", VANetworkedStreamSource, iconPlayer)
	add_custom_type("VASource", "Node2D", VASource, iconPlayer)
	add_custom_type("VASourceLeech", "Node2D", VASourceLeech, iconPlayer)

	add_custom_type("VASourceRelative", "Node2D", VASourceRelative, iconPlayer)
	add_custom_type("VASourceAmbient", "Node2D", VASourceAmbient, iconPlayer)

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

	hrtf_export_plugin = VAHRTFExportPlugin.new()
	add_export_plugin(hrtf_export_plugin)

	conversion_context_menu_plugin = VAConversionContextMenuPlugin.new()
	add_context_menu_plugin(EditorContextMenuPlugin.CONTEXT_SLOT_SCENE_TREE, conversion_context_menu_plugin)

	# Register audio/vaudio/* Project Settings
	VAProjectSettings.register()
	ProjectSettings.settings_changed.connect(VAProjectSettings.update_backend_visibility)

	# Populate the output_device dropdown with the backend's real device list
	VAWorld.RefreshOutputDeviceList()

	print("[vaudio-godot-mono-openal-2d] Vercidium Audio (vaudio) plugin enabled")

func _exit_tree():
	if ProjectSettings.settings_changed.is_connected(VAProjectSettings.update_backend_visibility):
		ProjectSettings.settings_changed.disconnect(VAProjectSettings.update_backend_visibility)

	remove_custom_type("VAEmitter")
	remove_custom_type("VAListener")
	remove_custom_type("VAPlayer")
	remove_custom_type("VAPlayer2D")
	remove_custom_type("VAStreamSource")
	remove_custom_type("VAInputStreamSource")
	remove_custom_type("VANetworkedStreamSource")
	remove_custom_type("VASource")
	remove_custom_type("VASourceLeech")
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

	if hrtf_export_plugin:
		remove_export_plugin(hrtf_export_plugin)
		hrtf_export_plugin = null

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
