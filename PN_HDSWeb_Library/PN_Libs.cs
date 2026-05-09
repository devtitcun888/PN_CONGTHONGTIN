using hUltiLibraryN8;
using hDataLibraryN8;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace PN_HDSWeb_Library
{
    public static class PN_Libs
    {
        public static hResponce hCheckCondition(string funcName, String jsonBody, string[] bodyKeys, System.Diagnostics.Stopwatch watch)
        {
            hResponce responce = new hResponce();            
            if (jsonBody == "")
            {
                responce = hdataLib.hgetResponceError(funcName, jsonBody, watch, PN_Message.isBodyEmpty, null);
                return responce;
            }
            //check key
            string chkKey = hJsonLib.hchkKey(jsonBody, bodyKeys);
            if (chkKey != "")
            {
                responce = hdataLib.hgetResponceError(funcName, jsonBody, watch, PN_Message.isNotKey + chkKey, null);
                return responce;
            }
            //check token
            JObject job = JObject.Parse(jsonBody);
            String token = job["token"].ToString();
            if (!chkToken(token))
            {
                responce = hdataLib.hgetResponceError(funcName, jsonBody, watch, PN_Message.isInvalidToken, null);
                return responce;
            }
            return null;
        }

        private static bool chkToken(string token)
        {
            //Nâng cấp hàm này
            string tokenFromFile = hLibrary.hGetTextFromFile(hConstants.PN_LOGIN_TOKEN_FILE);
            if (token != tokenFromFile) return false;
            else return true;
        }
    } //class
} //namespace
