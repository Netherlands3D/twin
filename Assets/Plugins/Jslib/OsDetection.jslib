mergeInto(LibraryManager.library, {
    IsWindowsOS: function () {
        return /Win(dows)?/i.test(navigator.userAgent);
    }
});