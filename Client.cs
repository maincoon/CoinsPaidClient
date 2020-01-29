using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinsPaid.V2 {
	/// <summary>
	/// CoinsPaid API implementation
	/// </summary>
	public class Client {
		public class Configuration {
			/// <summary>
			/// CoinsPaid API public key
			/// </summary>
			public readonly string PublicKey;
			/// <summary>
			/// CoinsPaid API secret to sign requests
			/// </summary>
			public readonly string APISecret;
			/// <summary>
			/// API endpoint
			/// </summary>
			public readonly string Endpoint;
			/// <summary>
			/// CTOR
			/// </summary>
			public Configuration(string endpoint, string pub, string sec) {
				Endpoint = endpoint;
				PublicKey = pub;
				APISecret = sec;
			}
		}

		/// <summary>
		/// Simplify request type
		/// </summary>
		public class Request : Dictionary<string, string> {
		}

		/// <summary>
		/// Client configuration
		/// </summary>
		public readonly Configuration Config;
		/// <summary>
		/// Signature hash generator
		/// </summary>
		public readonly HMACSHA512 HMAC;

		/// <summary>
		/// CTOR
		/// </summary>
		/// <param name="cfg">Client configuration</param>
		public Client(Configuration cfg) {
			Config = cfg;
			HMAC = new HMACSHA512(
				Encoding.ASCII.GetBytes(Config.APISecret)
			);
		}

		/// <summary>
		/// Create signature for X-Processing-Signature HTTP header using API secret
		/// </summary>
		/// <param name="request">Request variables</param>
		/// <returns>Signature or null on error</returns>
		public string SignRequest(Request request) {
			var json = "[]";
			if (request.Count > 0) {
				json = JsonConvert.SerializeObject(request);
			}
			var sign = HMAC.ComputeHash(
				Encoding.UTF8.GetBytes(json)
			);
			return BitConverter.ToString(sign).Replace("-", string.Empty).ToLower();
		}

		/// <summary>
		/// Create quthorization headers for client requests according to configuration and request data
		/// </summary>
		/// <param name="request">Request data</param>
		/// <returns>Authorization headers</returns>
		public Dictionary<string, string> CreateAuthHeaders(Request request) {
			return new Dictionary<string, string> {
				{ "X-Processing-Key", Config.PublicKey },
				{ "X-Processing-Signature", SignRequest(request) }
			};
		}

		/// <summary>
		/// Ping API as described in configuration
		/// </summary>
		/// <returns>true if API available otherwise false</returns>
		public async Task<bool> Ping(CancellationToken cancel = default) {
			using (HttpClient http = new HttpClient()) {
				var response = await HTTPHelper.GetRequestAsync(Config.Endpoint + "/ping", http, cancel);
				return response.Code == HttpStatusCode.OK;
			}
		}

		/// <summary>
		/// Implements /v2/currencies/list
		/// Get all supported currencies 
		/// </summary>
		/// <returns>Supported currencies list or default on error</returns>
		public async Task<Models.CurrenciesListResponse> CurrenciesList(CancellationToken cancel = default) {
			// prepare request
			var request = new Request();
			// send request
			using (HttpClient http = new HttpClient()) {
				var response = await HTTPHelper.PostRequest<Models.CurrenciesListResponse>(
					Config.Endpoint + "/currencies/list",
					http, request, CreateAuthHeaders(request),
					HttpStatusCode.OK,
					cancel
				);
				// check result
				if (response.Code == HttpStatusCode.OK) {
					return response.Result;
				} else {
					return default;
				}
			}
		}

		/// <summary>
		/// Implements /v2/currencies/pairs
		/// Get list of currency pairs if no parameters passed.
		/// Get particular pair and its price if currency parameters are passed.
		/// </summary>
		/// <param name="from">Filter by currency ISO that exchanges from</param>
		/// <param name="to">Filter by currency ISO that can be converted to</param>
		/// <returns>List of currency pairs or default on error</returns>
		public async Task<Models.CurrenciesPairsResponse> CurrenciesPairs(string from = "", string to = "",
			CancellationToken cancel = default) {
			// prepare request
			var request = new Request();
			if (!string.IsNullOrEmpty(from)) {
				request.Add("currency_from", from);
			}
			if (!string.IsNullOrEmpty(to)) {
				request.Add("currency_to", to);
			}
			// send request
			using (HttpClient http = new HttpClient()) {
				var response = await HTTPHelper.PostRequest<Models.CurrenciesPairsResponse>(
					Config.Endpoint + "/currencies/pairs",
					http, request, CreateAuthHeaders(request),
					HttpStatusCode.OK,
					cancel
				);
				// check result
				if (response.Code == HttpStatusCode.OK) {
					return response.Result;
				} else {
					return default;
				}
			}
		}

		/// <summary>
		/// Implements /v2/accounts/list
		/// Get list of all the balances (including zero balances).
		/// </summary>
		/// <returns>List of all the balances (including zero balances) or default on error</returns>
		public async Task<Models.AccountsListResponse> AccountsList(CancellationToken cancel = default) {
			// prepare request
			var request = new Request();
			// send request
			using (HttpClient http = new HttpClient()) {
				var response = await HTTPHelper.PostRequest<Models.AccountsListResponse>(
					Config.Endpoint + "/accounts/list",
					http, request, CreateAuthHeaders(request),
					HttpStatusCode.OK,
					cancel
				);
				// check result
				if (response.Code == HttpStatusCode.OK) {
					return response.Result;
				} else {
					return default;
				}
			}
		}

		/// <summary>
		/// Implements /v2/addresses/take
		/// Take address for depositing crypto and (it depends on specified params) exchange from crypto to fiat on-the-fly.
		/// </summary>
		/// <param name="id">Your info for this address, will returned as reference in Address responses</param>
		/// <param name="currency">ISO of currency to receive funds in</param>
		/// <param name="convert">Optional ISO of currency to convert funds</param>
		/// <returns>Address for depositing crypto or default on error</returns>
		public async Task<Models.AddressesTakeResponse> AddressesTake(string id, string currency, string convert = default, CancellationToken cancel = default) {
			// prepare request
			var request = new Request() {
				{ "foreign_id", id },
				{ "currency", currency }
			};
			if (!string.IsNullOrEmpty(convert)) {
				request.Add("convert_to", convert);
			}
			// send request
			using (HttpClient http = new HttpClient()) {
				var response = await HTTPHelper.PostRequest<Models.AddressesTakeResponse>(
					Config.Endpoint + "/addresses/take",
					http, request, CreateAuthHeaders(request),
					HttpStatusCode.Created,
					cancel
				);
				// check result
				if (response.Code == HttpStatusCode.Created) {
					return response.Result;
				} else {
					return default;
				}
			}
		}

		/// <summary>
		/// Implements /v2/withdrawal/crypto
		/// Withdraw in crypto to any specified address. 
		/// </summary>
		/// <param name="id">Unique operation foreign ID in your system</param>
		/// <param name="amount">Amount of funds to withdraw</param>
		/// <param name="currency">Currency ISO to be withdrawn</param>
		/// <param name="to">Cryptocurrency address where you want to send funds</param>
		/// <param name="convert">Optional ISO of currency to convert funds</param>
		/// <param name="tag">Tag (if it's Ripple or BNB) or memo (if it's Bitshares or EOS)</param>
		/// <returns>Withdraw state object or null on error</returns>
		public async Task<Models.WithdrawalCryptoResponse> WithdrawalCrypto(string id, string amount, string currency, string to,
			string convert = default, string tag = default, CancellationToken cancel = default) {
			// prepare request
			var request = new Request() {
				{ "foreign_id", id },
				{ "currency", currency },
				{ "amount", amount },
				{ "address", to }
			};
			if (!string.IsNullOrEmpty(convert)) {
				request.Add("convert_to", convert);
			}
			if (!string.IsNullOrEmpty(tag)) {
				request.Add("tag", tag);
			}
			// send request
			using (HttpClient http = new HttpClient()) {
				var response = await HTTPHelper.PostRequest<Models.WithdrawalCryptoResponse>(
					Config.Endpoint + "/withdrawal/crypto",
					http, request, CreateAuthHeaders(request),
					HttpStatusCode.Created,
					cancel
				);
				// check result
				if (response.Code == HttpStatusCode.Created) {
					return response.Result;
				} else {
					return default;
				}
			}
		}
	}
}
