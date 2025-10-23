Obj Importer
============

Using this package it is possible to create a GameObject from a .obj file in runtime.

## Usage

Create a GameObject in your scene and attach ObjImportManager.cs to it.
Make sure a Base Material is set. This material will be used as template for the materials of the created model.

Start the import by calling the public function ParseFiles() with a string as variable.
This string variable contains the filename of the .obj file, and optionally a .mtl-file and an image file.
Filenames are seperated by a comma (,).

To let a user select their own files from their computer, use the Filebrowser-package.

Using the public function Cancel(), the importing can be cancelled. During cancelling all temporary files will be removed.

> Important: All the files are assumed to be placed in the application.persistentDataPath, and will be deleted after 
> the file is imported.

## Settings

- Create SubMeshes: when selected all the geometry in the obj-file will be combined into a single mesh, with submeshes for the different materials.
When not selected, a gameobject will be crated for each material in the obj-file.

## Result

- Created Moveable Game Object: gameobject-event will be invoked when the imported obj-file did not contain RD-coordinates.
- Created Immoveable Game Object: gameobject-event will be invoked when the imported obj-file DID contain RD-coordinates.

## Progress

- Busy: boolEvent will be invoked with the value "true" on starting and will be invoked with the value "false" after finishing.
- Current Activity: stringEvent for disclosing the active activity.
- Current Action: stringEvent for disclosing the active action. (an action is a subtask of an activity).
- Progress Percentage: Invoked every frame to show the progress of the current activity in perCent.

## Alerts and errors

- AlertMessage: StringEvent that is invoked if something didn't go as expected, for example an imageFile was not found.
- Errormessage: StringEvent that is invoked if something went wrong that caused the import to be unsuccesfull.

