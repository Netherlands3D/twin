mergeInto(LibraryManager.library, {
    IsWindowsOS: function () {
        return /Win(dows)?/i.test(navigator.userAgent);
    },
	
	IsMobileDevice: function () {
        return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent);
    }
});