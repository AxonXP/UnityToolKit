# Product Requirements Document (PRD)

## Sprite Library Tools for Unity

**Version:** 1.0
**Target Unity Version:** Unity 6+ (Support Unity 2022.3 LTS where possible)
**Package Dependency:** Unity 2D Animation Package (`com.unity.2d.animation`)
**Platform:** Unity Editor Extension

---

# 1. Overview

Sprite Library Tools is a Unity Editor extension that dramatically improves the workflow of creating, organizing, editing, validating, and animating Unity Sprite Libraries.

The primary goal is to eliminate repetitive manual work when using Unity's Sprite Library system by providing intuitive batch operations, animation generation, visualization tools, and utility features.

This tool is intended for:

* 2D game developers
* Animation prototyping
* Character customization systems
* Modular sprite workflows
* Rapid game jam development

The extension should feel like a first-party Unity tool rather than a collection of scripts.

---

# 2. Design Goals

The entire package should focus on:

* Extremely fast workflows
* Minimal clicking
* Safe batch editing
* Visual previews
* Undo support
* Clean UI
* Unity styling
* Professional polish

Every destructive action should support Undo.

---

# 3. Main Editor Window

Menu

```
Tools
    Sprite Library Tools
```

Window title

```
Sprite Library Tools
```

The window consists of:

```
----------------------------------------------------
Toolbar
----------------------------------------------------

Sidebar                     Main Tool Area

Library Builder
Animation Generator
Library Manager
Library Inspector
Utilities

----------------------------------------------------
Status Bar
----------------------------------------------------
```

The left sidebar switches between tools.

The right side displays the selected tool.

---

# 4. Library Builder

Purpose:

Quickly create or populate Sprite Libraries.

---

## Tool 1

### Bulk Auto Fill Library

Supports:

* Drag Sprite Library Asset
* Drag Sprite Library Component
* Drag sprite folders

Options

```
Category Creation

○ Folder Names
○ Sprite Name Prefix
○ Existing Categories

Label Naming

○ Keep Original
○ Remove Prefix
○ Remove Suffix
○ Search Replace

Duplicates

○ Skip
○ Replace
○ Rename

Sorting

□ Alphabetical
□ Natural Sort

Preview

□ Show Changes
```

Preview displays

```
Category

Idle

    Idle_01
    Idle_02
    Idle_03

Walk

    Walk_01
```

Press

```
Apply
```

Library updates.

---

## Tool 2

### Batch Replace Sprites

Replace sprites while preserving labels.

Example

Old

```
Knight_Idle
```

becomes

```
Robot_Idle
```

Matching options

* Exact Name
* Ignore Prefix
* Ignore Suffix

---

## Tool 3

### Create Library From Folder

Accepts folder structure

```
Player

    Idle

    Walk

    Attack
```

Creates

```
Category Idle

Category Walk

Category Attack
```

Automatically.

---

# 5. Animation Generator

Purpose

Convert Sprite Library categories into animation assets.

---

## Category → Animation

Select

Sprite Library

Options

```
Output Folder

Frame Rate

Loop

Reverse

Ping Pong

Frame Duration

Animation Name Format

Overwrite Existing

Generate Animator Controller
```

Output

```
Idle.anim

Walk.anim

Attack.anim
```

---

## Batch Animation Generation

Generate animations for every category.

Supports

```
All Categories

Selected Categories

Filtered Categories
```

---

## Animation Preview

Preview playback directly inside the editor.

Controls

```
Play

Pause

Stop

FPS

Loop
```

No AnimationClip required.

---

# 6. Library Manager

Purpose

Edit existing libraries.

---

## Merge Libraries

Inputs

Library A

Library B

Options

```
Merge Categories

Overwrite

Skip Duplicates

Rename Duplicates
```

---

## Split Library

Split by

```
Selected Categories

Category Prefix

Maximum Categories
```

Outputs multiple Sprite Libraries.

---

## Duplicate Categories

Duplicate

```
Sword
```

to

```
Sword Bronze

Sword Silver

Sword Gold
```

---

## Move Labels

Move labels between categories.

Supports drag and drop.

---

## Rename Categories

Batch rename

Supports

Search

Replace

Prefix

Suffix

Regex

---

## Rename Labels

Same functionality.

---

## Sort Categories

Alphabetical

Reverse

Natural

Manual drag reorder

---

# 7. Library Inspector

Purpose

Visualize and validate libraries.

---

## Visual Browser

Display

```
Category

Idle

Sprite
Sprite
Sprite

Walk

Sprite
Sprite
```

Supports

Zoom

Thumbnail size

Search

---

## Search

Search categories

Search labels

Filter

Live updating

---

## Statistics

Display

```
Categories

Labels

Unique Sprites

Duplicate Sprites

Empty Categories

Missing Sprites
```

---

## Validation

Detect

Duplicate labels

Duplicate sprites

Missing references

Empty categories

Invalid names

Unused categories

Clickable results.

---

## Dependency Viewer

Show

Sprite

↓

Referenced by

Category A

Category B

Category C

---

# 8. Utilities

---

## Export

Export

JSON

CSV

---

## Import

Import JSON

Import CSV

---

## Backup

Create backup before modifications.

Restore backups.

---

## Repair References

Reconnect missing sprites.

Methods

GUID

Filename

Folder Search

---

## Library Diff

Compare two libraries.

Show

Added

Removed

Modified

Duplicates

Missing

---

# 9. User Experience

All tools should include

Progress bars

Cancelable operations

Status messages

Undo support

Confirmation dialogs

Error reporting

---

# 10. Visual Style

Use standard Unity Editor UI.

No custom skin.

Use:

HelpBoxes

Foldouts

Toolbar

SplitView

TreeView

SearchField

ObjectField

ProgressBar

Large previews

Consistent spacing.

---

# 11. Performance

Large libraries should remain responsive.

Target

10,000 sprites

without freezing.

Requirements

Lazy loading

Virtualized lists

Cached previews

Background processing where possible

Minimal AssetDatabase refreshes

---

# 12. Code Architecture

```
Editor

    SpriteLibraryToolsWindow

    Pages

        BuilderPage

        AnimationPage

        ManagerPage

        InspectorPage

        UtilitiesPage

Services

    LibraryBuilder

    AnimationGenerator

    LibraryMerger

    LibrarySplitter

    Validator

    Statistics

    BackupService

    RepairService

Models

Utilities

Preview

Styles
```

Every feature should exist as an independent service.

The window only controls UI.

---

# 13. Required Unity APIs

Use

```
SpriteLibraryAsset

SpriteResolver

AnimationClip

AnimationUtility

AssetDatabase

Undo

SerializedObject

EditorGUILayout

TreeView

EditorCoroutineUtility

EditorUtility

Progress

PrefabUtility
```

Avoid reflection wherever possible.

---

# 14. Error Handling

Never silently fail.

Examples

Missing sprites

Duplicate labels

Read-only assets

Invalid folders

Package missing

Display informative HelpBoxes with suggested fixes.

---

# 15. Future-Proofing

The architecture should allow new tools to be added without modifying existing ones. Each page should be self-contained and registered through a central page manager, making it easy to expand the suite with additional features in future versions.

---

# 16. Nice-to-Have (Stretch Goals)

If time permits, implement these additional productivity features:

### Sprite Variant Generator

* Generate alternate Sprite Libraries by swapping sprites from another folder using matching filenames.
* Useful for enemy recolors, character skins, or equipment variants.

### Auto Animator Controller Generator

* Optionally create an Animator Controller alongside generated Animation Clips.
* Automatically create one state per category with configurable default state.

### Naming Convention Wizard

* Detect inconsistent naming and offer one-click fixes.
* Support regex, prefixes, suffixes, and numbering formats.

### Drag-and-Drop Folder Conversion

* Drag a root folder directly into the window.
* Automatically infer categories from subfolders or sprite name prefixes.

### Session Persistence

* Remember the last selected page, folders, options, and output paths between Unity sessions using `EditorPrefs`.

### Batch Processing

* Allow multiple Sprite Libraries to be queued and processed in a single operation.

### Favorites / Presets

* Save commonly used generation settings (FPS, naming conventions, duplicate handling, etc.) as reusable presets.

---

## Success Criteria

A user should be able to:

* Build an entire Sprite Library from a folder structure in under one minute.
* Generate Animation Clips for every category with a single button click.
* Batch rename, reorganize, merge, or repair Sprite Libraries without manually editing assets.
* Validate and preview large Sprite Libraries efficiently.
* Perform all operations with full Undo support and clear visual feedback.
## Implementation Plan

Prerequisite: `com.unity.2d.animation` is not currently in `Packages/manifest.json` — it must be added (pulls in `com.unity.2d.sprite` / `com.unity.2d.common`) before any `SpriteLibraryAsset`/`SpriteResolver` code will compile. The window should detect its absence and show a HelpBox with a fix rather than throwing compile errors if someone opens the project without it.

Conventions carried over from the sibling `SpriteBatchSlicer` tool for consistency: pure IMGUI (`EditorWindow`/`EditorGUILayout`/`UnityEditor.IMGUI.Controls.TreeView`, not UI Toolkit), `EditorPrefs` for window/session persistence, `ScriptableObject` assets for saved presets, and snapshot objects for revert where Unity's built-in `Undo` doesn't cleanly cover an operation (bulk `SpriteLibraryAsset` edits should mostly work directly with `Undo.RecordObject`/`Undo.RegisterCreatedObjectUndo` since it's a plain asset, unlike `TextureImporter`).

1. **Foundations** — add the package dependency; scaffold the `Editor/SpriteLibraryTools` folder per Section 12 (`Pages/`, `Services/`, `Models/`, `Utilities/`, `Preview/`, `Styles/`); build the window shell (menu item, sidebar, status bar) around a data-driven `IToolPage` + page-registry so new pages/tools can be added later without touching existing ones (Section 15); add shared plumbing every page needs: a thumbnail/preview cache, an `EditorPrefs` session-state helper, and category/label model wrappers around `SpriteLibraryAsset`.
2. **Library Builder (vertical-slice MVP)** — Bulk Auto Fill (folder drag-drop → category creation strategy → label naming transform → duplicate handling → sort → diff-style preview → Apply), then Batch Replace Sprites and Create Library From Folder. This is the first end-to-end usable slice: build a real library from a folder structure with Undo support.
3. **Library Inspector (read side)** — pulled forward because Builder output needs to be visually verified: virtualized thumbnail Visual Browser, Search/Filter, Statistics panel, and Validation (duplicate labels/sprites, missing refs, empty categories, invalid names, unused categories) with clickable results. Dependency Viewer deferred to Phase 5, reusing the sprite-usage index Validation already builds.
4. **Animation Generator** — Category → AnimationClip service (`AnimationUtility` + `SpriteResolver` keyframes) with frame rate/loop/reverse/ping-pong/duration/naming/overwrite options and optional Animator Controller generation; batch mode over All/Selected/Filtered categories with progress + cancel; in-editor scrubber preview that samples `SpriteResolver` directly without requiring a baked `AnimationClip`.
5. **Library Manager** — Merge, Split, Duplicate Categories, Move Labels (drag-drop), Rename Categories/Labels (search/replace/prefix/suffix/regex), Sort Categories (incl. manual drag reorder), all Undo-recorded.
6. **Utilities & cross-cutting hardening** — Export/Import (JSON/CSV), Backup/Restore, Repair References (GUID/filename/folder-search), Library Diff, Dependency Viewer (if not already done), a full HelpBox-with-suggested-fix pass on every error path, and a performance pass validated against a 10,000-sprite library (virtualized lists, lazy thumbnail loading, `EditorCoroutineUtility` for long operations, minimized `AssetDatabase` refreshes).
7. **Stretch goals** — of the Section 16 list, Session Persistence and Favorites/Presets are cheap to fold into Phase 1/2 using the same `EditorPrefs`/`ScriptableObject`-profile pattern as `SpriteBatchSlicerProfile` rather than saving them for last; Naming Convention Wizard, Sprite Variant Generator, and multi-library Batch Processing remain true stretch goals attempted only if time remains after Phase 6.

Each phase should be independently shippable and testable inside the Editor before moving to the next, mirroring how `SpriteBatchSlicer` was built up in stages.
