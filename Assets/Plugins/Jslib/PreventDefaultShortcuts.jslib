mergeInto(LibraryManager.library, {
    PreventDefaultShortcuts: function () {
        document.addEventListener("keydown", function(e) {
            // Prevent default shortcuts for save, undo and redo
            if ((e.key === 's' || e.key === 'z' || e.key === 'r' || e.key === 'o') && (navigator.platform.match("Mac") ? e.metaKey : e.ctrlKey)) {
                e.preventDefault();
            }
        }, false);
    }
});