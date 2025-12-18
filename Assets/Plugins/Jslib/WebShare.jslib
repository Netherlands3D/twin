mergeInto(LibraryManager.library, {
    IsWebShareSupported: function () {
        return (typeof navigator.share !== 'undefined' && typeof navigator.canShare !== 'undefined');
    },

    ShareImage: function (filenamePtr, byteArray, byteArraySize, gameObjectNamePtr, callbackMethodNamePtr) {
        var filename = UTF8ToString(filenamePtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethodName = UTF8ToString(callbackMethodNamePtr);
        
        var bytes = new Uint8Array(byteArraySize);
        for (var i = 0; i < byteArraySize; i++) {
            bytes[i] = HEAPU8[byteArray + i];
        }

        var type = 'image/png';
        if (filename.toLowerCase().endsWith('.jpg') || filename.toLowerCase().endsWith('.jpeg')) {
            type = 'image/jpeg';
        }

        var blob = new Blob([bytes], { type: type });
        var file = new File([blob], filename, { type: type });
        
        if (navigator.canShare && navigator.canShare({ files: [file] })) {
            navigator.share({
                files: [file],
                title: filename, 
                text: 'Shared from Netherlands3D'
            }).then(function() {
                console.log('Share successful');
                if (gameObjectName && callbackMethodName) {
                    SendMessage(gameObjectName, callbackMethodName, "Share Success");
                }
            }).catch(function(error) {
                console.log('Share failed', error);
                if (gameObjectName && callbackMethodName) {
                    SendMessage(gameObjectName, callbackMethodName, "Share Failed: " + error);
                }
            });
        } else {
            console.log('Web Share API not supported for files.');
        }
    }
});
