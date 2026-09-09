Product Requirements Document
Sprite Batch Slicer
Unity Editor Extension

Version: 1.0

Overview

Sprite Batch Slicer is a Unity Editor extension that streamlines the process of importing and preparing sprite sheets. Instead of opening Unity's Sprite Editor for every image, users can select one or more textures, configure slicing settings once, preview the results, and batch apply slicing and import settings to hundreds of sprites simultaneously.

The extension integrates directly into the Unity Editor and works entirely within the project, requiring no external applications.

Goals

The tool should allow users to:

Batch slice multiple sprite sheets.
Apply consistent import settings to all selected textures.
Set sprite pivots/anchors in bulk.
Preview slicing before applying.
Save reusable presets.
Reduce repetitive work inside Unity.
Target Users
Unity 2D developers
Pixel artists
Indie developers
Game jam teams
Students
Unity Integration

The tool should appear as:

Tools
 └── Sprite Batch Slicer

Opening it displays a dockable Editor Window.

The tool should support:

Docking
Window resizing
Undo support
Unity Progress Bars
Unity Console logging
User Workflow
Step 1: Open the Tool
Tools
 └── Sprite Batch Tools

The editor window opens.

Step 2: Select Sprites

The user can populate the asset list by:

Dragging textures into the window.
Dragging folders from the Project window.
Clicking "Add Selected" to import the current Project selection.

Supported assets:

PNG
JPG
PSD (future)

When folders are added, every sprite sheet inside the folder is automatically included.

Step 3: Configure Slicing

Choose the slicing mode.

Uniform Grid

Fields:

Cell Width

Cell Height

Padding X

Padding Y

Offset X

Offset Y
Auto Detect Grid

Attempts to determine

sprite size
spacing
offsets

The user may manually adjust the detected values.

Detect Individual Sprites

Uses transparency to detect separate sprite regions.

Useful for

particles
icons
VFX
irregular sprite sheets
Step 4: Configure Import Settings

Apply import settings to every selected texture.

Pixels Per Unit
16
32
64
100
Custom
Filter Mode
Point

Bilinear

Trilinear
Compression
None

Low

Normal

High
Mesh Type
Full Rect

Tight
Wrap Mode
Clamp

Repeat
Read/Write Enabled

Checkbox

Generate Mip Maps

Checkbox

Sprite Mode
Single

Multiple
Step 5: Configure Pivot (Anchor)

Apply the same pivot to every generated sprite.

Available presets:

Center

Top

Bottom

Left

Right

Top Left

Top Right

Bottom Left

Bottom Right

Bottom Center

Custom

Selecting Custom reveals:

Pivot X

Pivot Y

Values are normalized from

0.0 → 1.0

The preview updates immediately to display the selected pivot.

Step 6: Preview

The center panel displays the selected sprite sheet.

Overlay includes:

Slice rectangles
Sprite numbering
Pivot locations
Trim boundaries (optional)

Controls:

Zoom
Pan
Toggle overlay
Toggle pivot markers

Changing any setting updates the preview in real time.

Step 7: Apply

Click

Apply to Selected

The tool:

Changes TextureImporter settings.
Creates SpriteMetaData entries.
Reimports textures.
Displays progress.
Reports completion.
Window Layout
-------------------------------------------------------
| Toolbar                                              |
-------------------------------------------------------
| Assets | Preview Window | Import Settings            |
|        |                |                            |
|        |                | Slice Settings             |
|        |                |                            |
|        |                | Pivot Settings             |
|        |                |                            |
|        |                | Profiles                   |
-------------------------------------------------------
| Progress Bar                                      Log |
-------------------------------------------------------
Core Features
Asset List

Displays

Texture name
Resolution
Sprite count (after preview)
Status

Supports

Multi-select
Remove
Clear All
Live Preview

Displays

Original texture
Slice rectangles
Pivot markers
Sprite numbers

Updates instantly whenever settings change.

Batch Slicing

Processes

One texture
Multiple textures
Entire folders

All using identical settings.

Pivot Assignment

Apply one pivot to every sprite.

Supported presets:

Center
Bottom Center
Bottom Left
Bottom Right
Top Center
Top Left
Top Right
Left Center
Right Center
Custom
Transparent Trimming

Optional.

Modes

Disabled

Trim Sprites

Trim Entire Texture
Naming

Rename sliced sprites.

Examples

Player_0

Player_1

Player_2

or

Player_Row1_Col3
Saved Profiles

Users can save complete configurations.

Example:

Pixel Art

32 x 32

PPU 32

Point Filter

Bottom Center Pivot

No Compression

Profiles include

Slice settings
Import settings
Pivot
Naming
Trimming
Project Scanner

Scan selected folders for sprites that don't match a chosen profile.

Reports

Wrong PPU

Wrong Filter Mode

Wrong Compression

Not sliced

Allows

Fix Selected

Fix All
Logging

Every processed asset displays

✔ Player.png

✔ Enemy.png

✔ UI.png

⚠ Explosion.png

Error:
Texture too small
Undo Support

Before applying changes

Store importer settings using Unity's Undo system whenever possible, allowing users to revert accidental changes.

Performance Requirements

The tool should:

Handle 500+ sprite sheets in one operation.
Keep the Unity Editor responsive.
Display Unity's progress bar during imports.
Never stop processing because one texture fails.
Technical Requirements
Unity Version

Unity 6+

Language

C#

APIs
EditorWindow
TextureImporter
AssetDatabase
SpriteMetaData
SerializedObject
EditorGUI
Undo
EditorPrefs
AssetPostprocessor (future)
Future Features (Version 2)
Automatic Animation Generation

Generate

Animation Clips
Animator Controllers

from sliced sprites.

Smart Detection

Automatically determine

Cell size
Padding
Margins
Auto Pivot Detection

Find the lowest opaque pixel and place the pivot at the character's feet.

Sprite Atlas Integration

Automatically assign sprites to Sprite Atlases.

AssetPostprocessor Integration

Allow folders to automatically use a saved profile whenever new sprite sheets are imported.

Example:

Assets/
    Characters/
    Enemies/
    UI/

Each folder can have its own import profile that is applied automatically.

Bulk Metadata Editing

Modify existing sprites without re-slicing.

Examples:

Change pivots
Update PPU
Change filter mode
Update compression
Validation Mode

Highlight assets that differ from the selected profile.

One click:

Apply Profile

fixes every selected asset.

Success Criteria

A Unity developer should be able to:

Select an entire folder of sprite sheets.
Open Tools → Sprite Batch Tools.
Configure slicing, import settings, and pivot once.
Preview the results before making changes.
Click Apply to Selected.
Have every sprite sheet imported, sliced, assigned the correct pivot, and configured with consistent import settings in a single operation.

The entire workflow should take under a minute for dozens of sprite sheets and eliminate the need to open Unity's Sprite Editor for routine batch import tasks. This creates a polished, professional editor extension that feels like a natural part of the Unity Editor rather than an external utility.

1. MVP (this session): Menu item opens dockable window → Asset List panel (drag-drop textures/folders, "Add Selected", multi-select, remove/clear, shows name+resolution) → Uniform Grid slice settings (cell W/H, padding X/Y, offset X/Y) → Import settings (PPU, Filter Mode, Compression, Sprite Mode fixed to Multiple) → single default pivot (Center) → "Apply to Selected" button that slices + reimports with progress bar and console logging (✔/⚠ per PRD Logging section).
2. Preview & fine control: Live preview panel (zoom/pan, slice-rect overlay, sprite numbering, pivot markers, toggle overlay), full pivot preset dropdown + custom X/Y, naming pattern field, transparent trimming modes.
3. Smart detection: Auto Detect Grid heuristic, Detect Individual Sprites (alpha connected-component flood fill), selectable alongside Uniform Grid in the same preview/apply pipeline.
4. Profiles & Scanner: Save/Load/New SpriteBatchSlicerProfile assets from the window, EditorPrefs remembers last used profile; Project Scanner (scan a folder against a profile, report mismatches, Fix Selected/Fix All).
5. Polish: snapshot-based revert, further perf validation against 500+ textures, edge-case handling (non-sliceable/corrupt textures), final pass on window layout/docking/resizing.