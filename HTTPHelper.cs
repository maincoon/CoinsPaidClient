using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinsPaid {
	public static class HTTPHelper {
		/// <summary>
		/// Send HTTP POST request and return result as string
		/// </summary>
		public static async Task<(string Result, HttpStatusCode Code)> PostRequestAsync(string url, HttpClient http,
			HttpContent content, CancellationToken cancel = default) {
			try {
				using (HttpResponseMessage response = await http.PostAsync(url, content, cancel)) {
					var result = await response.Content.ReadAsStringAsync();
					return (result, response.StatusCode);
				}
			} catch (Exception ex){
				return (ex.ToString(), HttpStatusCode.BadRequest);
			}
		}

		/// <summary>
		/// Send HTTP GET request and return result as string
		/// </summary>
		public static async Task<(string Result, HttpStatusCode Code)> GetRequestAsync(string url, HttpClient http,
			CancellationToken cancel = default) {
			try {
				using (HttpResponseMessage response = await http.GetAsync(url, cancel)) {
					var result = await response.Content.ReadAsStringAsync();
					return (result, response.StatusCode);
				}
			} catch (Exception ex) {
				return (ex.ToString(), HttpStatusCode.BadRequest);
			}
		}

		/// <summary>
		/// Send POST HTTP request and interpret result as specified generic model
		/// </summary>
		public static async Task<(T Result, HttpStatusCode Code)> PostRequest<T>(string url, HttpClient http,
			Dictionary<string, string> request,
			Dictionary<string, string> headers,
			HttpStatusCode expected,
			CancellationToken cancel = default) {
			try {
				// creating POST data
				string json = "[]";
				if (request.Count > 0) {
					json = JsonConvert.SerializeObject(request);
				}
				using (HttpContent content = new StringContent(json, Encoding.UTF8, "application/json")) {
					// adding headers
					foreach (var header in headers) {
						content.Headers.Add(header.Key, header.Value);
					}
					// post request
					var response = await PostRequestAsync(url, http, content, cancel);
					if (response.Code == expected) {
						// try to deserialize response
						var data = JsonConvert.DeserializeObject<T>(response.Result);
						return (data, response.Code);
					} else {
						return (default, response.Code);
					}
				}
			} catch {
				return (default, HttpStatusCode.BadRequest);
			}
		}
	}
}
