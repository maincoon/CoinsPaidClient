using System;
using System.Collections.Generic;
using System.Text;

namespace CoinsPaid.V2 {
	/// <summary>
	/// Generated via http://json2csharp.com 
	/// according to https://app.coinspaid.com/api/v2 specification
	/// </summary>
	public class Models {
		/// <summary>
		/// v2/currencies/list response
		/// </summary>
		public class CurrenciesListResponse {
			public class Item {
				public int id {
					get; set;
				}
				public string type {
					get; set;
				}
				public string currency {
					get; set;
				}
				public string minimum_amount {
					get; set;
				}
				public string deposit_fee_percent {
					get; set;
				}
				public string withdrawal_fee_percent {
					get; set;
				}
				public int precision {
					get; set;
				}
			}
			public List<Item> data {
				get; set;
			}
		}

		/// <summary>
		/// v2/currencies/pairs response
		/// </summary>
		public class CurrenciesPairsResponse {
			public class CurrencyFrom {
				public string currency {
					get; set;
				}
				public string type {
					get; set;
				}
				public string min_amount {
					get; set;
				}
				public string min_amount_deposit_with_exchange {
					get; set;
				}
			}
			public class CurrencyTo {
				public string currency {
					get; set;
				}
				public string type {
					get; set;
				}
			}
			public class Item {
				public CurrencyFrom currency_from {
					get; set;
				}
				public CurrencyTo currency_to {
					get; set;
				}
				public string rate_from {
					get; set;
				}
				public string rate_to {
					get; set;
				}
			}
			public List<Item> data {
				get; set;
			}
		}

		/// <summary>
		/// /v2/accounts/list
		/// </summary>
		public class AccountsListResponse {
			public class Item {
				public string currency {
					get; set;
				}
				public string type {
					get; set;
				}
				public string balance {
					get; set;
				}
			}
			public List<Item> data {
				get; set;
			}
		}

		/// <summary>
		/// /v2/addresses/take
		/// </summary>
		public class AddressesTakeResponse {
			public class Data {
				public int id {
					get; set;
				}
				public string currency {
					get; set;
				}
				public string convert_to {
					get; set;
				}
				public string address {
					get; set;
				}
				public string tag {
					get; set;
				}
				public string foreign_id {
					get; set;
				}
			}
			public Data data {
				get; set;
			}
		}

		/// <summary>
		/// /v2/withdrawal/crypto
		/// </summary>
		public class WithdrawalCryptoResponse {
			public class Data {
				public int id {
					get; set;
				}
				public string foreign_id {
					get; set;
				}
				public string type {
					get; set;
				}
				public string status {
					get; set;
				}
				public string amount {
					get; set;
				}
				public string sender_amount {
					get; set;
				}
				public string sender_currency {
					get; set;
				}
				public string receiver_amount {
					get; set;
				}
				public string receiver_currency {
					get; set;
				}
			}
			public Data data {
				get; set;
			}
		}
	}
}
