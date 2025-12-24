mergeInto(LibraryManager.library, {

    IsWebShareSupported: function () {
        return typeof navigator !== "undefined" && typeof navigator.share === "function";
    },

    ShareImage: function (filenamePtr, byteArray, byteArraySize, gameObjectNamePtr, callbackMethodNamePtr) {

        const filename = UTF8ToString(filenamePtr);
        const gameObjectName = UTF8ToString(gameObjectNamePtr);
        const callbackMethodName = UTF8ToString(callbackMethodNamePtr);

        const bytes = new Uint8Array(byteArraySize);
        for (let i = 0; i < byteArraySize; i++) {
            bytes[i] = HEAPU8[byteArray + i];
        }

        let type = "image/png";
        if (filename.toLowerCase().endsWith(".jpg") || filename.toLowerCase().endsWith(".jpeg")) {
            type = "image/jpeg";
        }

        const blob = new Blob([bytes], { type });
        const file = new File([blob], filename, { type });

        // Store globally so it can be shared from a real click
        window.__unityShareData = {
            file,
            filename,
            gameObjectName,
            callbackMethodName
        };

        // Create (or reuse) a real DOM button
        let btn = document.getElementById("unity-share-btn");
        if (!btn) {
            btn = document.createElement("button");
            btn.id = "unity-share-btn";
            btn.style.position = "absolute";
            btn.style.left = "-9999px";
            document.body.appendChild(btn);

            btn.addEventListener("click", async () => {
                const data = window.__unityShareData;
                if (!data) return;

                try {
                    // Safari: do NOT trust canShare
                    await navigator.share({
                        files: [data.file],
                        title: data.filename
                    });

                    if (data.gameObjectName && data.callbackMethodName) {
                        SendMessage(data.gameObjectName, data.callbackMethodName, "Share Success");
                    }
                } catch (e) {
                    // Safari fallback: open image
                    const url = URL.createObjectURL(data.file);
                    window.open(url, "_blank");

                    if (data.gameObjectName && data.callbackMethodName) {
                        SendMessage(
                            data.gameObjectName,
                            data.callbackMethodName,
                            "Share Fallback (Opened)"
                        );
                    }
                }
            });
        }

        // Programmatic click — works because handler is user-gesture bound
        btn.click();
    }
});