mergeInto(LibraryManager.library, {
    ReplaceUrl: function (url) {
        history.replaceState(history.state, '', UTF8ToString(url));
    }
});