mergeInto(LibraryManager.library, {
    LockCursorInternal: function () {
        document.getElementById("unity-canvas").requestPointerLock();
    },

    UnlockCursorInternal: function () {
        document.exitPointerLock();
    }
});
