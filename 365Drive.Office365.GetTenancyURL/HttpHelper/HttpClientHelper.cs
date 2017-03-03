using CsQuery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;


namespace _365Drive.Office365.GetTenancyURL
{
    public static class HttpClientHelper
    {

        /// <summary>
        /// General get call with NO parameter and will return simple response
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url)
        {
            using (HttpClient request = new HttpClient())
            {
                var responseMessage = await request.GetAsync(url);
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Call with header preset
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header">header</param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url, NameValueCollection header)
        {
            using (HttpClient request = new HttpClient())
            {
                ///set the header
                if (header != null)
                    SetHeader(request, header);

                var responseMessage = await request.GetAsync(url);
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Call with cookies preset
        /// </summary>
        /// <param name="url"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url, CookieContainer container)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (HttpClient request = new HttpClient(handler))
            {
                var responseMessage = await request.GetAsync(url);
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }


        /// <summary>
        /// Call with cookies preset and extra header values
        /// </summary>
        /// <param name="url"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url, CookieContainer container, NameValueCollection header)
        {
            //settting cookies
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (HttpClient request = new HttpClient(handler))
            {
                ///set the header
                if (header != null)
                    SetHeader(request, header);

                var responseMessage = await request.GetAsync(url);
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }


        /// <summary>
        /// General get call with NO parameter and will return simple response
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string postBody, string contentType)
        {
            using (HttpClient request = new HttpClient())
            {
                var responseMessage = await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Call with header preset
        /// </summary>
        /// <param name="url"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string postBody, string contentType, NameValueCollection header)
        {

            using (HttpClient request = new HttpClient())
            {
                ///set the header
                if (header != null)
                    SetHeader(request, header);

                var responseMessage = await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Call with cookies preset
        /// </summary>
        /// <param name="url"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string postBody, string contentType, CookieContainer container)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (HttpClient request = new HttpClient(handler))
            {


                var responseMessage = await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }


        /// <summary>
        /// Call with cookies preset and extra header values
        /// </summary>
        /// <param name="url"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string postBody, string contentType, CookieContainer container, NameValueCollection header)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (HttpClient request = new HttpClient(handler))
            {
                ///set the header
                if (header != null)
                    SetHeader(request, header);

                var responseMessage = await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Call with cookies preset and extra header values
        /// </summary>
        /// <param name="url"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> PostAsyncFullResponse(string url, string postBody, string contentType, CookieContainer container, NameValueCollection header)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (HttpClient request = new HttpClient(handler))
            {
                ///set the header
                if (header != null)
                    SetHeader(request, header);

                //var responseMessage = await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
                //responseMessage.EnsureSuccessStatusCode();
                return await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
            }
        }

        /// <summary>
        /// return full response
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postBody"></param>
        /// <param name="contentType"></param>
        /// <param name="container"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> PostAsyncFullResponse(string url, string postBody, string contentType, NameValueCollection header)
        {
            
            using (HttpClient request = new HttpClient())
            {
                ///set the header
                if (header != null)
                    SetHeader(request, header);

                //var responseMessage = await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
                //responseMessage.EnsureSuccessStatusCode();
                return await request.PostAsync(url, new StringContent(postBody, Encoding.UTF8, contentType));
            }
        }
        /// <summary>
        /// Set the header to HTTPClient
        /// </summary>
        /// <param name="request"></param>
        /// <param name="header"></param>
        static void SetHeader(HttpClient request, NameValueCollection header)
        {
            foreach (string key in header.AllKeys)
            {
                request.DefaultRequestHeaders.Add(key, Convert.ToString(header[key]));
            }
        }
    }
}