mergeInto(LibraryManager.library, {
    DownloadFileImmediate: function(gameObjectNamePtr, callbackMethodNamePtr, filenamePtr, byteArray, byteArraySize) {
        gameObjectName = UTF8ToString(gameObjectNamePtr);
        callbackMethodName = UTF8ToString(callbackMethodNamePtr);
        filename = UTF8ToString(filenamePtr);
        
        var bytes = new Uint8Array(byteArraySize);
        for (var i = 0; i < byteArraySize; i++) {
            bytes[i] = HEAPU8[byteArray + i];
        }

        var downloader = window.document.createElement('a');
        downloader.setAttribute('id', gameObjectName);
        downloader.href = window.URL.createObjectURL(new Blob([bytes], { type: 'application/octet-stream' }));
        downloader.download = filename;
        document.body.appendChild(downloader);
        
        downloader.click();
        document.body.removeChild(downloader);

        if (callbackMethodName && callbackMethodName.length > 0) {
            SendMessage(gameObjectName, callbackMethodName);
        }
    }
});