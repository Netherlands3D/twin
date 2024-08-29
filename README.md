# Netherlands3D Digital Twin

The goal of this project is twofold:

1. To provide a basic, easy-to-use foundation for your local Digital Twin, based on Unity Gaming Technology.
2. To provide a skeleton project for building your own Digital Twin using Netherlands3D based on Unity Gaming Technology.

## Do not commit these files

There are several files that we use as ScriptableObjects that will be changed in while running the editor, but which
should not be changed. These are the following:

- Assets/Scriptables/Projects/CurrentProject.asset

It is recommended to set up your local git repository to ignore changes to these files using the `git update-index`
command, on which you can read more here: https://medium.com/@adi.ashour/dont-git-angry-skip-in-worktree-e9c77dec9d15

The command should be:

```bash
$ git update-index --skip-worktree .\Assets\Scriptables\Projects\CurrentProject.asset
```

At time of writing, no convenient way has been found to use a ScriptableObject for runtime configuration without
this drawback.