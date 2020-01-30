using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
			/// <summary>
			/// Request path
			/// </summary>
			[JsonIgnore]
			public readonly string Path;
			/// <summary>
			/// Expected HTTP result code
			/// </summary>
			[JsonIgnore]
			public readonly HttpStatusCode Expected;
			/// <summary>
			/// CTOR
			/// </summary>
			/// <param name="path">Request path</param>
			/// <param name="expected">Expected success HTTP result code</param>
			public Request(string path = default, HttpStatusCode expected = HttpStatusCode.BadRequest) {
				Path = path;
				Expected = expected;
			}
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
		/// Run POST request and interpret response as specified model
		/// </summary>
		/// <typeparam name="T">Reponse model type</typeparam>
		/// <param name="request">Request object</param>
		/// <returns>Response model or null on error</returns>
		async Task<Models.Response<T>> RunRequest<T>(Request request, CancellationToken cancel) {
			using (HttpClient http = new HttpClient()) {
				// send request
				var response = await HTTPHelper.PostRequest<T>(
					Config.Endpoint + request.Path,
					http, request, CreateAuthHeaders(request),
					request.Expected,
					cancel
				);
				// prepare result
				Models.Response<T> result = new Models.Response<T>(response.Result);
				if (response.Code != request.Expected) {
					try {
						// try parse errors
						var error = JsonConvert.DeserializeObject<Models.Error>(response.Message);
						// copy values
						foreach (var e in error.errors) {
							result.Errors.Add(e.Key, e.Value);
						}
					} catch {
						result.Errors.Add("message", response.Message);
					}
				}
				return result;
			}
		}

		/// <summary>
		/// Implements /v2/currencies/list
		/// Get all supported currencies 
		/// </summary>
		/// <returns>Supported currencies list or default on error</returns>
		public async Task<Models.Response<Models.CurrenciesList>> CurrenciesList(CancellationToken cancel = default) {
			// run request
			return await RunRequest<Models.CurrenciesList>(
				new Request("/currencies/list", HttpStatusCode.OK),
				cancel
			);
		}

		/// <summary>
		/// Implements /v2/currencies/pairs
		/// Get list of currency pairs if no parameters passed.
		/// Get particular pair and its price if currency parameters are passed.
		/// </summary>
		/// <param name="from">Filter by currency ISO that exchanges from</param>
		/// <param name="to">Filter by currency ISO that can be converted to</param>
		/// <returns>List of currency pairs or default on error</returns>
		public async Task<Models.Response<Models.CurrenciesPairs>> CurrenciesPairs(string from = "", string to = "",
			CancellationToken cancel = default) {
			// prepare request
			var request = new Request("/currencies/pairs", HttpStatusCode.OK);
			if (!string.IsNullOrEmpty(from)) {
				request.Add("currency_from", from);
			}
			if (!string.IsNullOrEmpty(to)) {
				request.Add("currency_to", to);
			}
			// send request
			return await RunRequest<Models.CurrenciesPairs>(request, cancel);
		}

		/// <summary>
		/// Implements /v2/accounts/list
		/// Get list of all the balances (including zero balances).
		/// </summary>
		/// <returns>List of all the balances (including zero balances) or default on error</returns>
		public async Task<Models.Response<Models.AccountsList>> AccountsList(CancellationToken cancel = default) {
			// send request
			return await RunRequest<Models.AccountsList>(
				new Request("/accounts/list", HttpStatusCode.OK),
				cancel
			);
		}

		/// <summary>
		/// Implements /v2/addresses/take
		/// Take address for depositing crypto and (it depends on specified params) exchange from crypto to fiat on-the-fly.
		/// </summary>
		/// <param name="id">Your info for this address, will returned as reference in Address responses</param>
		/// <param name="currency">ISO of currency to receive funds in</param>
		/// <param name="convert">Optional ISO of currency to convert funds</param>
		/// <returns>Address for depositing crypto or default on error</returns>
		public async Task<Models.Response<Models.AddressesTake>> AddressesTake(string id, string currency, string convert = default, CancellationToken cancel = default) {
			// prepare request
			var request = new Request("/addresses/take", HttpStatusCode.Created) {
				{ "foreign_id", id },
				{ "currency", currency }
			};
			if (!string.IsNullOrEmpty(convert)) {
				request.Add("convert_to", convert);
			}
			// send request
			return await RunRequest<Models.AddressesTake>(request, cancel);
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
		public async Task<Models.Response<Models.WithdrawalCrypto>> WithdrawalCrypto(string id, string amount, string currency, string to,
			string convert = default, string tag = default, CancellationToken cancel = default) {
			// prepare request
			var request = new Request("/withdrawal/crypto", HttpStatusCode.Created) {
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
			return await RunRequest<Models.WithdrawalCrypto>(request, cancel);
		}

		/// <summary>
		/// Implements /v2/exchange/calculate
		/// Calculate exchnage by received amount
		/// </summary>
		/// <param name="from">Currency ISO for which you want to calculate the exchange rate</param>
		/// <param name="to">Currency ISO to be exchanged</param>
		/// <param name="amount">Amount you want to calculate</param>
		/// <returns>Info about exchange rate or null on error</returns>
		public async Task<Models.Response<Models.ExchnageCalculate>> ExchnageCalculateByReceived(string from, string to, string amount,
			CancellationToken cancel = default) {
			// prepare request
			var request = new Request("/exchange/calculate", HttpStatusCode.OK) {
				{ "sender_currency", from },
				{ "receiver_currency", to },
				{ "receiver_amount", amount }
			};
			// send request
			return await RunRequest<Models.ExchnageCalculate>(request, cancel);
		}

		/// <summary>
		/// Implements /v2/exchange/calculate
		/// Calculate exchnage by sent amount
		/// </summary>
		/// <param name="from">Currency ISO for which you want to calculate the exchange rate</param>
		/// <param name="to">Currency ISO to be exchanged</param>
		/// <param name="amount">Amount you want to calculate</param>
		/// <returns>Info about exchange rate or null on error</returns>
		public async Task<Models.Response<Models.ExchnageCalculate>> ExchnageCalculateBySent(string from, string to, string amount,
			CancellationToken cancel = default) {
			// prepare request
			var request = new Request("/exchange/calculate", HttpStatusCode.OK) {
				{ "sender_currency", from },
				{ "receiver_currency", to },
				{ "sender_amount", amount }
			};
			// send request
			return await RunRequest<Models.ExchnageCalculate>(request, cancel);
		}

		/// <summary>
		/// Implements /v2/exchange/fixed
		/// Make exchange on a given fixed exchange rate
		/// </summary>
		/// <param name="id">Unique foreign ID in your system</param>
		/// <param name="from">Currency ISO which you want to exchange</param>
		/// <param name="to">Currency ISO to be exchanged</param>
		/// <param name="amount">Amount you want to exchange</param>
		/// <param name="rate">Exchange rate price on which exchange will be placed</param>
		/// <returns>Exchange state or null on error</returns>
		public async Task<Models.Response<Models.Exchange>> ExchangeFixed(string id, string from, string to, string amount, string rate,
			CancellationToken cancel = default) {
			// prepare request
			var request = new Request("/exchange/fixed", HttpStatusCode.Created) {
				{ "sender_currency", from },
				{ "receiver_currency", to },
				{ "sender_amount", amount },
				{ "foreign_id", id },
				{ "price", rate }
			};
			// send request
			return await RunRequest<Models.Exchange>(request, cancel);
		}

		/// <summary>
		/// Implements /v2/exchange/now
		/// Make exchange without mentioning the price
		/// </summary>
		/// <param name="id">Unique foreign ID in your system</param>
		/// <param name="from">Currency ISO which you want to exchange</param>
		/// <param name="to">Currency ISO to be exchanged</param>
		/// <param name="amount">Amount you want to exchange</param>
		/// <returns>Exchange state or null on error</returns>
		public async Task<Models.Response<Models.Exchange>> ExchangeNow(string id, string from, string to, string amount,
			CancellationToken cancel = default) {
			// prepare request
			var request = new Request("/exchange/now", HttpStatusCode.Created) {
				{ "sender_currency", from },
				{ "receiver_currency", to },
				{ "sender_amount", amount },
				{ "foreign_id", id }
			};
			// send request
			return await RunRequest<Models.Exchange>(request, cancel);
		}
	}
}
