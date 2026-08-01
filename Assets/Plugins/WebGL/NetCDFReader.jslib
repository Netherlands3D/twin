mergeInto(LibraryManager.library, {

    InitNetCDFReader: function (arrayPtr, length)
    {
        try {
            var bytes = new Uint8Array(HEAPU8.buffer, arrayPtr, length);
            var buf = bytes.slice().buffer; // copy out of the WASM heap, independent lifetime
            window.currentNetCDFReader = new NetCDF.NetCDFReader(buf);
            return 1;
        } catch (e) {
            console.error("NetCDF parse failed: " + e);
            return 0;
        }
    },

    GetVariableInfoJson: function (namePtr)
    {
        var name = UTF8ToString(namePtr);
        var reader = window.currentNetCDFReader;
        if (!reader) return 0;

        var variable = reader.variables.find(function (v) { return v.name === name; });
        if (!variable) return 0;

        var dims = variable.dimensions.map(function (dimId) {
            return reader.dimensions[dimId];
        });

        var info = {
            name: variable.name,
            type: variable.type,
            dimensionNames: dims.map(function (d) { return d.name; }),
            dimensionSizes: dims.map(function (d) { return d.size; })
        };

        var json = JSON.stringify(info);
        var bufferSize = lengthBytesUTF8(json) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(json, buffer, bufferSize);
        return buffer;
    },

    CopyVariableDataToBuffer: function (namePtr, destPtr, destLength)
    {
        var name = UTF8ToString(namePtr);
        var reader = window.currentNetCDFReader;
        if (!reader) return 0;

        var data = reader.getDataVariable(name); // flat JS number array
        var count = Math.min(data.length, destLength);

        for (var i = 0; i < count; i++) {
            HEAPF32[(destPtr >> 2) + i] = data[i];
        }

        return count;
    }

});
