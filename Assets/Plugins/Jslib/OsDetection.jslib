mergeInto(LibraryManager.library, {
    IsWindowsOS: function () {
        console.log(navigator.userAgent);
        return /Win(dows)?/i.test(navigator.userAgent);
    }
});