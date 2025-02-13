﻿// Listen on a specific host via the HOST environment variable
var host = process.env.NL3D_CORS_PROXY_HOST || '0.0.0.0';
// Listen on a specific port via the PORT environment variable
var port = process.env.NL3D_CORS_PROXY_PORT || 8080;

var cors_proxy = require('cors-anywhere');
cors_proxy.createServer({
    originWhitelist: [], // Allow all origins
    requireHeader: ['origin', 'x-requested-with'],
    removeHeaders: ['cookie', 'cookie2']
}).listen(port, host, function() {
    console.log('Running CORS Anywhere on ' + host + ':' + port);
});