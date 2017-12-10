using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppExchangeCoinAlert.Core
{
    public class Bittrex
    {
        //private string convertParameterListToString(IDictionary<string, string> parameters)
        //{
        //    if (parameters.Count == 0) return "";
        //    return parameters.Select(param => WebUtility.UrlEncode(param.Key) + "=" + WebUtility.UrlEncode(param.Value)).Aggregate((l, r) => l + "&" + r);
        //}
        //  HttpRequestMessage createRequest(HttpMethod httpMethod, string uri, IDictionary<string, string> parameters, bool includeAuthentication)
        //{
        //        var parameterString = convertParameterListToString(parameters);
        //        var completeUri = uri + "?" + parameterString;
        //        var request = new HttpRequestMessage(httpMethod, completeUri);
        //        return request;

        //}
        public async Task<ResponseWrapper<TResult>> Request<TResult>(HttpMethod httpMethod, string uri)
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(httpMethod, uri);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new ResponseWrapper<TResult>
                {
                    Success = false,
                    Message = "HTTP Error: " + response.StatusCode + " " + response.ReasonPhrase
                };
            var content = await response.Content.ReadAsStringAsync();

            var bittrexResponse = JsonConvert.DeserializeObject<BittrexResponse>(content);
            var result = new ResponseWrapper<TResult>
            {
                Success = bittrexResponse.Success,
                Message = bittrexResponse.Message
            };
            if (bittrexResponse.Success)
            {
                try
                {
                    result.Result = bittrexResponse.Result.ToObject<TResult>();
                }
                catch (Exception e)
                {
                    throw new JsonConversionException("Error converting json to .Net types", e, bittrexResponse.Result.ToString(Formatting.Indented), typeof(TResult));
                }
            }
            return result;
        }
    }
}