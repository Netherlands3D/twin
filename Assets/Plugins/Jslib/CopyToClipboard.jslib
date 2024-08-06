mergeInto(LibraryManager.library, {
    CopyToClipboard: function (textToCopy) {
        const toCopy = UTF8ToString(textToCopy);
        navigator.clipboard.writeText(toCopy).catch(function (error) {
            console.error("Failed to copy to clipboard", error);
        });
    }
});