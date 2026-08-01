
console.log("netcdfjs loaded");/*
 * netcdfjs v4.0.0
 * Read and explore NetCDF files
 * https://github.com/cheminfo/netcdfjs
 *
 * Licensed under the MIT license.
 */
(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
    typeof define === 'function' && define.amd ? define(['exports'], factory) :
    (global = typeof globalThis !== 'undefined' ? globalThis : global || self, factory(global.NetCDF = {}));
})(this, (function (exports) { 'use strict';

    /**
     * Decode bytes to text
     * @param bytes - Bytes to decode
     * @param encoding - Text encoding
     * @returns The decoded text
     */
    function decode(bytes, encoding = 'utf8') {
      const decoder = new TextDecoder(encoding);
      return decoder.decode(bytes);
    }
    const encoder = new TextEncoder();
    /**
     * Encode text to utf8
     * @param str - Text to encode
     * @returns The encoded bytes
     */
    function encode(str) {
      return encoder.encode(str);
    }

    const defaultByteLength = 1024 * 8;
    const hostBigEndian = (() => {
      const array = new Uint8Array(4);
      const view = new Uint32Array(array.buffer);
      return !((view[0] = 1) & array[0]);
    })();
    const typedArrays = {
      int8: globalThis.Int8Array,
      uint8: globalThis.Uint8Array,
      int16: globalThis.Int16Array,
      uint16: globalThis.Uint16Array,
      int32: globalThis.Int32Array,
      uint32: globalThis.Uint32Array,
      uint64: globalThis.BigUint64Array,
      int64: globalThis.BigInt64Array,
      float32: globalThis.Float32Array,
      float64: globalThis.Float64Array
    };
    class IOBuffer {
      /**
       * Reference to the internal ArrayBuffer object.
       */
      buffer;
      /**
       * Byte length of the internal ArrayBuffer.
       */
      byteLength;
      /**
       * Byte offset of the internal ArrayBuffer.
       */
      byteOffset;
      /**
       * Byte length of the internal ArrayBuffer.
       */
      length;
      /**
       * The current offset of the buffer's pointer.
       */
      offset;
      lastWrittenByte;
      littleEndian;
      _data;
      _mark;
      _marks;
      /**
       * Create a new IOBuffer.
       * @param data - The data to construct the IOBuffer with.
       * If data is a number, it will be the new buffer's length<br>
       * If data is `undefined`, the buffer will be initialized with a default length of 8Kb<br>
       * If data is an ArrayBuffer, SharedArrayBuffer, an ArrayBufferView (Typed Array), an IOBuffer instance,
       * or a Node.js Buffer, a view will be created over the underlying ArrayBuffer.
       * @param options - An object for the options.
       * @returns A new IOBuffer instance.
       */
      constructor(data = defaultByteLength, options = {}) {
        let dataIsGiven = false;
        if (typeof data === 'number') {
          data = new ArrayBuffer(data);
        } else {
          dataIsGiven = true;
          this.lastWrittenByte = data.byteLength;
        }
        const offset = options.offset ? options.offset >>> 0 : 0;
        const byteLength = data.byteLength - offset;
        let dvOffset = offset;
        if (ArrayBuffer.isView(data) || data instanceof IOBuffer) {
          if (data.byteLength !== data.buffer.byteLength) {
            dvOffset = data.byteOffset + offset;
          }
          data = data.buffer;
        }
        if (dataIsGiven) {
          this.lastWrittenByte = byteLength;
        } else {
          this.lastWrittenByte = 0;
        }
        this.buffer = data;
        this.length = byteLength;
        this.byteLength = byteLength;
        this.byteOffset = dvOffset;
        this.offset = 0;
        this.littleEndian = true;
        this._data = new DataView(this.buffer, dvOffset, byteLength);
        this._mark = 0;
        this._marks = [];
      }
      /**
       * Checks if the memory allocated to the buffer is sufficient to store more
       * bytes after the offset.
       * @param byteLength - The needed memory in bytes.
       * @returns `true` if there is sufficient space and `false` otherwise.
       */
      available(byteLength = 1) {
        return this.offset + byteLength <= this.length;
      }
      /**
       * Check if little-endian mode is used for reading and writing multi-byte
       * values.
       * @returns `true` if little-endian mode is used, `false` otherwise.
       */
      isLittleEndian() {
        return this.littleEndian;
      }
      /**
       * Set little-endian mode for reading and writing multi-byte values.
       * @returns This.
       */
      setLittleEndian() {
        this.littleEndian = true;
        return this;
      }
      /**
       * Check if big-endian mode is used for reading and writing multi-byte values.
       * @returns `true` if big-endian mode is used, `false` otherwise.
       */
      isBigEndian() {
        return !this.littleEndian;
      }
      /**
       * Switches to big-endian mode for reading and writing multi-byte values.
       * @returns This.
       */
      setBigEndian() {
        this.littleEndian = false;
        return this;
      }
      /**
       * Move the pointer n bytes forward.
       * @param n - Number of bytes to skip.
       * @returns This.
       */
      skip(n = 1) {
        this.offset += n;
        return this;
      }
      /**
       * Move the pointer n bytes backward.
       * @param n - Number of bytes to move back.
       * @returns This.
       */
      back(n = 1) {
        this.offset -= n;
        return this;
      }
      /**
       * Move the pointer to the given offset.
       * @param offset - The offset to move to.
       * @returns This.
       */
      seek(offset) {
        this.offset = offset;
        return this;
      }
      /**
       * Store the current pointer offset.
       * @see {@link IOBuffer#reset}
       * @returns This.
       */
      mark() {
        this._mark = this.offset;
        return this;
      }
      /**
       * Move the pointer back to the last pointer offset set by mark.
       * @see {@link IOBuffer#mark}
       * @returns This.
       */
      reset() {
        this.offset = this._mark;
        return this;
      }
      /**
       * Push the current pointer offset to the mark stack.
       * @see {@link IOBuffer#popMark}
       * @returns This.
       */
      pushMark() {
        this._marks.push(this.offset);
        return this;
      }
      /**
       * Pop the last pointer offset from the mark stack, and set the current
       * pointer offset to the popped value.
       * @see {@link IOBuffer#pushMark}
       * @returns This.
       */
      popMark() {
        const offset = this._marks.pop();
        if (offset === undefined) {
          throw new Error('Mark stack empty');
        }
        this.seek(offset);
        return this;
      }
      /**
       * Move the pointer offset back to 0.
       * @returns This.
       */
      rewind() {
        this.offset = 0;
        return this;
      }
      /**
       * Make sure the buffer has sufficient memory to write a given byteLength at
       * the current pointer offset.
       * If the buffer's memory is insufficient, this method will create a new
       * buffer (a copy) with a length that is twice (byteLength + current offset).
       * @param byteLength - The needed memory in bytes.
       * @returns This.
       */
      ensureAvailable(byteLength = 1) {
        if (!this.available(byteLength)) {
          const lengthNeeded = this.offset + byteLength;
          const newLength = lengthNeeded * 2;
          const newArray = new Uint8Array(newLength);
          newArray.set(new Uint8Array(this.buffer));
          this.buffer = newArray.buffer;
          this.length = newLength;
          this.byteLength = newLength;
          this._data = new DataView(this.buffer);
        }
        return this;
      }
      /**
       * Read a byte and return false if the byte's value is 0, or true otherwise.
       * Moves pointer forward by one byte.
       * @returns The read boolean.
       */
      readBoolean() {
        return this.readUint8() !== 0;
      }
      /**
       * Read a signed 8-bit integer and move pointer forward by 1 byte.
       * @returns The read byte.
       */
      readInt8() {
        return this._data.getInt8(this.offset++);
      }
      /**
       * Read an unsigned 8-bit integer and move pointer forward by 1 byte.
       * @returns The read byte.
       */
      readUint8() {
        return this._data.getUint8(this.offset++);
      }
      /**
       * Alias for {@link IOBuffer#readUint8}.
       * @returns The read byte.
       */
      readByte() {
        return this.readUint8();
      }
      /**
       * Read `n` bytes and move pointer forward by `n` bytes.
       * @param n - Number of bytes to read.
       * @returns The read bytes.
       */
      readBytes(n = 1) {
        return this.readArray(n, 'uint8');
      }
      /**
       * Creates an array of corresponding to the type `type` and size `size`.
       * For example, type `uint8` will create a `Uint8Array`.
       * @param size - size of the resulting array
       * @param type - number type of elements to read
       * @returns The read array.
       */
      readArray(size, type) {
        const bytes = typedArrays[type].BYTES_PER_ELEMENT * size;
        const offset = this.byteOffset + this.offset;
        const slice = this.buffer.slice(offset, offset + bytes);
        if (this.littleEndian === hostBigEndian && type !== 'uint8' && type !== 'int8') {
          const slice = new Uint8Array(this.buffer.slice(offset, offset + bytes));
          slice.reverse();
          const returnArray = new typedArrays[type](slice.buffer);
          this.offset += bytes;
          returnArray.reverse();
          return returnArray;
        }
        const returnArray = new typedArrays[type](slice);
        this.offset += bytes;
        return returnArray;
      }
      /**
       * Read a 16-bit signed integer and move pointer forward by 2 bytes.
       * @returns The read value.
       */
      readInt16() {
        const value = this._data.getInt16(this.offset, this.littleEndian);
        this.offset += 2;
        return value;
      }
      /**
       * Read a 16-bit unsigned integer and move pointer forward by 2 bytes.
       * @returns The read value.
       */
      readUint16() {
        const value = this._data.getUint16(this.offset, this.littleEndian);
        this.offset += 2;
        return value;
      }
      /**
       * Read a 32-bit signed integer and move pointer forward by 4 bytes.
       * @returns The read value.
       */
      readInt32() {
        const value = this._data.getInt32(this.offset, this.littleEndian);
        this.offset += 4;
        return value;
      }
      /**
       * Read a 32-bit unsigned integer and move pointer forward by 4 bytes.
       * @returns The read value.
       */
      readUint32() {
        const value = this._data.getUint32(this.offset, this.littleEndian);
        this.offset += 4;
        return value;
      }
      /**
       * Read a 32-bit floating number and move pointer forward by 4 bytes.
       * @returns The read value.
       */
      readFloat32() {
        const value = this._data.getFloat32(this.offset, this.littleEndian);
        this.offset += 4;
        return value;
      }
      /**
       * Read a 64-bit floating number and move pointer forward by 8 bytes.
       * @returns The read value.
       */
      readFloat64() {
        const value = this._data.getFloat64(this.offset, this.littleEndian);
        this.offset += 8;
        return value;
      }
      /**
       * Read a 64-bit signed integer number and move pointer forward by 8 bytes.
       * @returns The read value.
       */
      readBigInt64() {
        const value = this._data.getBigInt64(this.offset, this.littleEndian);
        this.offset += 8;
        return value;
      }
      /**
       * Read a 64-bit unsigned integer number and move pointer forward by 8 bytes.
       * @returns The read value.
       */
      readBigUint64() {
        const value = this._data.getBigUint64(this.offset, this.littleEndian);
        this.offset += 8;
        return value;
      }
      /**
       * Read a 1-byte ASCII character and move pointer forward by 1 byte.
       * @returns The read character.
       */
      readChar() {
        // eslint-disable-next-line unicorn/prefer-code-point
        return String.fromCharCode(this.readInt8());
      }
      /**
       * Read `n` 1-byte ASCII characters and move pointer forward by `n` bytes.
       * @param n - Number of characters to read.
       * @returns The read characters.
       */
      readChars(n = 1) {
        let result = '';
        for (let i = 0; i < n; i++) {
          result += this.readChar();
        }
        return result;
      }
      /**
       * Read the next `n` bytes, return a UTF-8 decoded string and move pointer
       * forward by `n` bytes.
       * @param n - Number of bytes to read.
       * @returns The decoded string.
       */
      readUtf8(n = 1) {
        return decode(this.readBytes(n));
      }
      /**
       * Read the next `n` bytes, return a string decoded with `encoding` and move pointer
       * forward by `n` bytes.
       * If no encoding is passed, the function is equivalent to @see {@link IOBuffer#readUtf8}
       * @param n - Number of bytes to read.
       * @param encoding - The encoding to use. Default is 'utf8'.
       * @returns The decoded string.
       */
      decodeText(n = 1, encoding = 'utf8') {
        return decode(this.readBytes(n), encoding);
      }
      /**
       * Write 0xff if the passed value is truthy, 0x00 otherwise and move pointer
       * forward by 1 byte.
       * @param value - The value to write.
       * @returns This.
       */
      writeBoolean(value) {
        this.writeUint8(value ? 0xff : 0x00);
        return this;
      }
      /**
       * Write `value` as an 8-bit signed integer and move pointer forward by 1 byte.
       * @param value - The value to write.
       * @returns This.
       */
      writeInt8(value) {
        this.ensureAvailable(1);
        this._data.setInt8(this.offset++, value);
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as an 8-bit unsigned integer and move pointer forward by 1
       * byte.
       * @param value - The value to write.
       * @returns This.
       */
      writeUint8(value) {
        this.ensureAvailable(1);
        this._data.setUint8(this.offset++, value);
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * An alias for {@link IOBuffer#writeUint8}.
       * @param value - The value to write.
       * @returns This.
       */
      writeByte(value) {
        return this.writeUint8(value);
      }
      /**
       * Write all elements of `bytes` as uint8 values and move pointer forward by
       * `bytes.length` bytes.
       * @param bytes - The array of bytes to write.
       * @returns This.
       */
      writeBytes(bytes) {
        this.ensureAvailable(bytes.length);
        // eslint-disable-next-line @typescript-eslint/prefer-for-of
        for (let i = 0; i < bytes.length; i++) {
          this._data.setUint8(this.offset++, bytes[i]);
        }
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 16-bit signed integer and move pointer forward by 2
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeInt16(value) {
        this.ensureAvailable(2);
        this._data.setInt16(this.offset, value, this.littleEndian);
        this.offset += 2;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 16-bit unsigned integer and move pointer forward by 2
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeUint16(value) {
        this.ensureAvailable(2);
        this._data.setUint16(this.offset, value, this.littleEndian);
        this.offset += 2;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 32-bit signed integer and move pointer forward by 4
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeInt32(value) {
        this.ensureAvailable(4);
        this._data.setInt32(this.offset, value, this.littleEndian);
        this.offset += 4;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 32-bit unsigned integer and move pointer forward by 4
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeUint32(value) {
        this.ensureAvailable(4);
        this._data.setUint32(this.offset, value, this.littleEndian);
        this.offset += 4;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 32-bit floating number and move pointer forward by 4
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeFloat32(value) {
        this.ensureAvailable(4);
        this._data.setFloat32(this.offset, value, this.littleEndian);
        this.offset += 4;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 64-bit floating number and move pointer forward by 8
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeFloat64(value) {
        this.ensureAvailable(8);
        this._data.setFloat64(this.offset, value, this.littleEndian);
        this.offset += 8;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 64-bit signed bigint and move pointer forward by 8
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeBigInt64(value) {
        this.ensureAvailable(8);
        this._data.setBigInt64(this.offset, value, this.littleEndian);
        this.offset += 8;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write `value` as a 64-bit unsigned bigint and move pointer forward by 8
       * bytes.
       * @param value - The value to write.
       * @returns This.
       */
      writeBigUint64(value) {
        this.ensureAvailable(8);
        this._data.setBigUint64(this.offset, value, this.littleEndian);
        this.offset += 8;
        this._updateLastWrittenByte();
        return this;
      }
      /**
       * Write the charCode of `str`'s first character as an 8-bit unsigned integer
       * and move pointer forward by 1 byte.
       * @param str - The character to write.
       * @returns This.
       */
      writeChar(str) {
        // eslint-disable-next-line unicorn/prefer-code-point
        return this.writeUint8(str.charCodeAt(0));
      }
      /**
       * Write the charCodes of all `str`'s characters as 8-bit unsigned integers
       * and move pointer forward by `str.length` bytes.
       * @param str - The characters to write.
       * @returns This.
       */
      writeChars(str) {
        for (let i = 0; i < str.length; i++) {
          // eslint-disable-next-line unicorn/prefer-code-point
          this.writeUint8(str.charCodeAt(i));
        }
        return this;
      }
      /**
       * UTF-8 encode and write `str` to the current pointer offset and move pointer
       * forward according to the encoded length.
       * @param str - The string to write.
       * @returns This.
       */
      writeUtf8(str) {
        return this.writeBytes(encode(str));
      }
      /**
       * Export a Uint8Array view of the internal buffer.
       * The view starts at the byte offset and its length
       * is calculated to stop at the last written byte or the original length.
       * @returns A new Uint8Array view.
       */
      toArray() {
        return new Uint8Array(this.buffer, this.byteOffset, this.lastWrittenByte);
      }
      /**
       *  Get the total number of bytes written so far, regardless of the current offset.
       * @returns - Total number of bytes.
       */
      getWrittenByteLength() {
        return this.lastWrittenByte - this.byteOffset;
      }
      /**
       * Update the last written byte offset
       * @private
       */
      _updateLastWrittenByte() {
        if (this.offset > this.lastWrittenByte) {
          this.lastWrittenByte = this.offset;
        }
      }
    }

    const types = {
      BYTE: 1,
      CHAR: 2,
      SHORT: 3,
      INT: 4,
      FLOAT: 5,
      DOUBLE: 6
    };
    /**
     * Parse a number into their respective type
     * @param type - integer that represents the type
     * @returns - parsed value of the type
     */
    function num2str(type) {
      switch (type) {
        case types.BYTE:
          return 'byte';
        case types.CHAR:
          return 'char';
        case types.SHORT:
          return 'short';
        case types.INT:
          return 'int';
        case types.FLOAT:
          return 'float';
        case types.DOUBLE:
          return 'double';
        default:
          return 'undefined';
      }
    }
    /**
     * Parse a number type identifier to his size in bytes
     * @param type - integer that represents the type
     * @returns size of the type
     */
    function num2bytes(type) {
      switch (type) {
        case types.BYTE:
          return 1;
        case types.CHAR:
          return 1;
        case types.SHORT:
          return 2;
        case types.INT:
          return 4;
        case types.FLOAT:
          return 4;
        case types.DOUBLE:
          return 8;
        default:
          return -1;
      }
    }
    /**
     * Reverse search of num2str
     * @param type - string that represents the type
     * @returns parsed value of the type
     */
    function str2num(type) {
      switch (type) {
        case 'byte':
          return types.BYTE;
        case 'char':
          return types.CHAR;
        case 'short':
          return types.SHORT;
        case 'int':
          return types.INT;
        case 'float':
          return types.FLOAT;
        case 'double':
          return types.DOUBLE;
        /* istanbul ignore next */
        default:
          return -1;
      }
    }
    /**
     * Auxiliary function to read numeric data
     * @param size - Size of the element to read
     * @param bufferReader - Function to read next value
     * @returns
     */
    function readNumber(size, bufferReader) {
      if (size !== 1) {
        const numbers = new Array(size);
        for (let i = 0; i < size; i++) {
          numbers[i] = bufferReader();
        }
        return numbers;
      } else {
        return bufferReader();
      }
    }
    /**
     * Given a type and a size reads the next element
     * @param buffer - Buffer for the file data
     * @param type - Type of the data to read
     * @param size - Size of the element to read
     * @returns
     */
    function readType(buffer, type, size) {
      switch (type) {
        case types.BYTE:
          return Array.from(buffer.readBytes(size));
        case types.CHAR:
          return trimNull(buffer.readChars(size));
        case types.SHORT:
          return readNumber(size, buffer.readInt16.bind(buffer));
        case types.INT:
          return readNumber(size, buffer.readInt32.bind(buffer));
        case types.FLOAT:
          return readNumber(size, buffer.readFloat32.bind(buffer));
        case types.DOUBLE:
          return readNumber(size, buffer.readFloat64.bind(buffer));
        default:
          throw new Error(`non valid type ${type}`);
      }
    }
    /**
     * Removes null terminate value
     * @param value - String to trim
     * @returns - Trimmed string
     */
    function trimNull(value) {
      if (value.codePointAt(value.length - 1) === 0) {
        return value.slice(0, Math.max(0, value.length - 1));
      }
      return value;
    }

    // const STREAMING = 4294967295;
    /**
     * Read data for the given non-record variable
     * @param buffer - Buffer for the file data
     * @param variable - Variable metadata
     * @returns - Data of the element
     */
    function nonRecord(buffer, variable) {
      // variable type
      const type = str2num(variable.type);
      // size of the data
      const size = variable.size / num2bytes(type);
      // iterates over the data
      const data = new Array(size);
      for (let i = 0; i < size; i++) {
        data[i] = readType(buffer, type, 1);
      }
      return data;
    }
    /**
     * Read data for the given record variable
     * @param buffer - Buffer for the file data
     * @param variable - Variable metadata
     * @param recordDimension - Record dimension metadata
     * @returns - Data of the element
     */
    function record(buffer, variable, recordDimension) {
      // variable type
      const type = str2num(variable.type);
      const width = variable.size > 0 ? variable.size / num2bytes(type) : 1;
      // size of the data
      // TODO streaming data
      const size = recordDimension.length;
      // iterates over the data
      const data = new Array(size);
      const step = recordDimension.recordStep;
      if (step) {
        for (let i = 0; i < size; i++) {
          const currentOffset = buffer.offset;
          data[i] = readType(buffer, type, width);
          buffer.seek(currentOffset + step);
        }
      } else {
        throw new Error('recordDimension.recordStep is undefined');
      }
      return data;
    }

    /**
     * Throws a non-valid NetCDF exception if the statement it's true
     * @ignore
     * @param statement - Throws if true
     * @param reason - Reason to throw
     */
    function notNetcdf(statement, reason) {
      if (statement) {
        throw new TypeError(`Not a valid NetCDF v3.x file: ${reason}`);
      }
    }
    /**
     * Moves 1, 2, or 3 bytes to next 4-byte boundary
     * @param buffer - Buffer for the file data
     */
    function padding(buffer) {
      if (buffer.offset % 4 !== 0) {
        buffer.skip(4 - buffer.offset % 4);
      }
    }
    /**
     * Reads the name
     * @param buffer - Buffer for the file data
     * @returns Name
     */
    function readName(buffer) {
      // Read name
      const nameLength = buffer.readUint32();
      const name = buffer.readChars(nameLength);
      // validate name
      // TODO
      // Apply padding
      padding(buffer);
      return name;
    }

    // Grammar constants
    const ZERO = 0;
    const NC_DIMENSION = 10;
    const NC_VARIABLE = 11;
    const NC_ATTRIBUTE = 12;
    const NC_UNLIMITED = 0;
    /**
     * Reads the file header as @see {@link Header}
     * @param buffer - Buffer for the file data
     * @param version - Version of the file
     * @returns
     */
    function header(buffer, version) {
      const header = {
        version
      };
      const recordDimension = {
        length: buffer.readUint32()
      };
      const dimList = dimensionsList(buffer);
      if (!Array.isArray(dimList)) {
        recordDimension.id = dimList.recordId;
        recordDimension.name = dimList.recordName;
        header.dimensions = dimList.dimensions;
      }
      header.globalAttributes = attributesList(buffer);
      const variables = variablesList(buffer, recordDimension?.id, version);
      if (!Array.isArray(variables)) {
        header.variables = variables.variables;
        recordDimension.recordStep = variables.recordStep;
      }
      header.recordDimension = recordDimension;
      return header;
    }
    /**
     * List of dimensions
     * @param buffer - Buffer for the file data
     * @returns List of dimensions
     */
    function dimensionsList(buffer) {
      const result = {};
      let recordId, recordName;
      const dimList = buffer.readUint32();
      let dimensions;
      if (dimList === ZERO) {
        notNetcdf(buffer.readUint32() !== ZERO, 'wrong empty tag for list of dimensions');
        return [];
      } else {
        notNetcdf(dimList !== NC_DIMENSION, 'wrong tag for list of dimensions');
        // Length of dimensions
        const dimensionSize = buffer.readUint32();
        dimensions = new Array(dimensionSize);
        //populate `name` and `size` for each dimension
        for (let dim = 0; dim < dimensionSize; dim++) {
          // Read name
          const name = readName(buffer);
          // Read dimension size
          const size = buffer.readUint32();
          if (size === NC_UNLIMITED) {
            // in netcdf 3 one field can be of size unlimited
            recordId = dim;
            recordName = name;
          }
          dimensions[dim] = {
            name,
            size
          };
        }
      }
      if (recordId !== undefined) {
        result.recordId = recordId;
      }
      if (recordName !== undefined) {
        result.recordName = recordName;
      }
      result.dimensions = dimensions;
      return result;
    }
    /**
     * List of attributes
     * @param buffer - Buffer for the file data
     * @returns - List of attributes with:
     */
    function attributesList(buffer) {
      const gAttList = buffer.readUint32();
      let attributes;
      if (gAttList === ZERO) {
        notNetcdf(buffer.readUint32() !== ZERO, 'wrong empty tag for list of attributes');
        return [];
      } else {
        notNetcdf(gAttList !== NC_ATTRIBUTE, 'wrong tag for list of attributes');
        // Length of attributes
        const attributeSize = buffer.readUint32();
        attributes = new Array(attributeSize);
        // Populate `name`, `type` and `value` for each attribute
        for (let gAtt = 0; gAtt < attributeSize; gAtt++) {
          // Read name
          const name = readName(buffer);
          // Read type
          const type = buffer.readUint32();
          notNetcdf(type < 1 || type > 6, `non valid type ${type}`);
          // Read attribute
          const size = buffer.readUint32();
          const value = readType(buffer, type, size);
          // Apply padding
          padding(buffer);
          attributes[gAtt] = {
            name,
            type: num2str(type),
            value
          };
        }
      }
      return attributes;
    }
    /**
     * @param buffer - Buffer for the file data
     * @param recordId - Id of the unlimited dimension (also called record dimension)
     * This value may be undefined if there is no unlimited dimension
     * @param version - Version of the file
     * @returns - Number of recordStep and list of variables @see {@link Variables}
     */
    function variablesList(buffer, recordId, version) {
      const varList = buffer.readUint32();
      let recordStep = 0;
      let variables;
      if (varList === ZERO) {
        notNetcdf(buffer.readUint32() !== ZERO, 'wrong empty tag for list of variables');
        return [];
      } else {
        notNetcdf(varList !== NC_VARIABLE, 'wrong tag for list of variables');
        // Length of variables
        const variableSize = buffer.readUint32();
        variables = new Array(variableSize);
        for (let v = 0; v < variableSize; v++) {
          // Read name
          const name = readName(buffer);
          // Read dimensionality of the variable
          const dimensionality = buffer.readUint32();
          // Index into the list of dimensions
          const dimensionsIds = new Array(dimensionality);
          for (let dim = 0; dim < dimensionality; dim++) {
            dimensionsIds[dim] = buffer.readUint32();
          }
          // Read variables size
          const attributes = attributesList(buffer);
          // Read type
          const type = buffer.readUint32();
          notNetcdf(type < 1 && type > 6, `non valid type ${type}`);
          // Read variable size
          // The 32-bit varSize field is not large enough to contain the size of variables that require
          // more than 2^32 - 4 bytes, so 2^32 - 1 is used in the varSize field for such variables.
          const varSize = buffer.readUint32();
          // Read offset
          let offset = buffer.readUint32();
          if (version === 2) {
            notNetcdf(offset > 0, 'offsets larger than 4GB not supported');
            offset = buffer.readUint32();
          }
          let record = false;
          // Count amount of record variables
          if (recordId !== undefined && dimensionsIds[0] === recordId) {
            recordStep += varSize;
            record = true;
          }
          variables[v] = {
            name,
            dimensions: dimensionsIds,
            attributes,
            type: num2str(type),
            size: varSize,
            offset,
            record
          };
        }
      }
      return {
        variables,
        recordStep
      };
    }

    /**
     * Describes the content of a NetCDF file as a human-readable string.
     * @param reader - Reader to describe.
     * @returns The description of the dimensions, global attributes and variables.
     */
    function netcdfToString(reader) {
      const result = ['DIMENSIONS'];
      for (const dimension of reader.dimensions) {
        result.push(`  ${dimension.name.padEnd(30)} = size: ${dimension.size}`);
      }
      result.push('', 'GLOBAL ATTRIBUTES');
      for (const attribute of reader.globalAttributes) {
        result.push(`  ${attribute.name.padEnd(30)} = ${attribute.value}`);
      }
      result.push('', 'VARIABLES:');
      for (const variable of reader.variables) {
        const value = reader.getDataVariable(variable);
        let stringify = JSON.stringify(value);
        if (stringify.length > 50) stringify = stringify.slice(0, 50);
        if (Array.isArray(value)) {
          stringify += ` (length: ${value.length})`;
        }
        result.push(`  ${variable.name.padEnd(30)} = ${stringify}`);
      }
      return result.join('\n');
    }

    /**
     * Reads a NetCDF v3.x file
     * [See specification](https://www.unidata.ucar.edu/software/netcdf/docs/file_format_specifications.html)
     * @param data - ArrayBuffer or any Typed Array (including Node.js' Buffer from v4) with the data
     * @class
     */
    class NetCDFReader {
      header;
      buffer;
      constructor(data) {
        const buffer = new IOBuffer(data);
        buffer.setBigEndian();
        // Validate that it's a NetCDF file
        notNetcdf(buffer.readChars(3) !== 'CDF', 'should start with CDF');
        // Check the NetCDF format
        const version = buffer.readByte();
        notNetcdf(version > 2, 'unknown version');
        // Read the header
        this.header = header(buffer, version);
        this.buffer = buffer;
      }
      /**
       * @returns - Version for the NetCDF format
       */
      get version() {
        if (this.header.version === 1) {
          return 'classic format';
        } else {
          return '64-bit offset format';
        }
      }
      /**
       * @returns - Metadata for the record dimension
       *  `length`: Number of elements in the record dimension
       *  `id`: Id number in the list of dimensions for the record dimension
       *  `name`: String with the name of the record dimension
       *  `recordStep`: Number with the record variables step size
       */
      get recordDimension() {
        return this.header.recordDimension;
      }
      /**
       * @returns - Array - List of dimensions with:
       *  `name`: String with the name of the dimension
       *  `size`: Number with the size of the dimension
       */
      get dimensions() {
        return this.header.dimensions;
      }
      /**
       * @returns - Array - List of global attributes with:
       *  `name`: String with the name of the attribute
       *  `type`: String with the type of the attribute
       *  `value`: A number or string with the value of the attribute
       */
      get globalAttributes() {
        return this.header.globalAttributes;
      }
      /**
       * Returns the value of an attribute
       * @param - - AttributeName
       * @param attributeName
       * @returns - Value of the attributeName or null
       */
      getAttribute(attributeName) {
        const attribute = this.globalAttributes.find(val => val.name === attributeName);
        if (attribute) return attribute.value;
        return null;
      }
      /**
       * Returns the value of a variable as a string
       * @param - - variableName
       * @param variableName
       * @returns - Value of the variable as a string or null
       */
      getDataVariableAsString(variableName) {
        const variable = this.getDataVariable(variableName);
        if (variable) return variable.join('');
        return null;
      }
      get variables() {
        return this.header.variables;
      }
      /**
       * Describes the content of the file as a human-readable string.
       * @returns The description of the dimensions, global attributes and variables.
       */
      toString() {
        return netcdfToString(this);
      }
      /**
       * Retrieves the data for a given variable
       * @param variableName - Name of the variable to search or variable object
       * @returns The variable values
       */
      getDataVariable(variableName) {
        let variable;
        if (typeof variableName === 'string') {
          // search the variable
          variable = this.header.variables.find(val => {
            return val.name === variableName;
          });
        } else {
          variable = variableName;
        }
        // throws if variable not found
        if (variable === undefined) {
          throw new Error('Not a valid NetCDF v3.x file: variable not found');
        }
        // go to the offset position
        this.buffer.seek(variable.offset);
        if (variable.record) {
          // record variable case
          return record(this.buffer, variable, this.header.recordDimension);
        } else {
          // non-record variable case
          return nonRecord(this.buffer, variable);
        }
      }
      /**
       * Check if a dataVariable exists
       * @param variableName - Name of the variable to find
       * @returns boolean
       */
      dataVariableExists(variableName) {
        const variable = this.header.variables.find(val => {
          return val.name === variableName;
        });
        return variable !== undefined;
      }
      /**
       * Check if an attribute exists
       * @param attributeName - Name of the attribute to find
       * @returns boolean
       */
      attributeExists(attributeName) {
        const attribute = this.globalAttributes.find(val => val.name === attributeName);
        return attribute !== undefined;
      }
    }

    exports.NetCDFReader = NetCDFReader;

}));
//# sourceMappingURL=netcdfjs.umd.js.map
