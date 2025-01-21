using System.Linq;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.ExtensionMethods
{
    // This class contains extention methods for UnityWebRequest response codes
    // to quickly determine if a server response requires credentials or if it is a server error
    public static class UnityWebRequestResponseCodeExtentionMethods
    {     
        public static uint[] responseCodesRequiringCredentials = new uint[] { 
            401, //Unauthorized
            402, //Payment Required
            403, //Forbidden,
            407, //Proxy Authentication Required
            498, //Esri Token Expired
            499  //Esri Token Required
        };
        public static uint[] responseCodesServerErrors = new uint[] { 
            404, //Not Found
            405, //Method Not Allowed
            406, //Not Acceptable
            500, //Internal Server Error
            501, //Not Implemented
            502, //Bad Gateway
            503, //Service Unavailable
            504, //Gateway Timeout
            505, //HTTP Version Not Supported
            506, //Variant Also Negotiates
            507, //Insufficient Storage
            508, //Loop Detected
            510, //Not Extended
            511  //Network Authentication Required
        };

        public static bool RequiresCredentials(this UnityWebRequest request)
        {
            var responseCode = (uint)request.responseCode;

            return responseCodesRequiringCredentials.Contains(responseCode);
        }

        public static bool ReturnedServerError(this UnityWebRequest request)
        {
            var responseCode = (uint)request.responseCode;

            return responseCodesServerErrors.Contains(responseCode);
        }
    }
}
