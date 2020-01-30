using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CoinsPaid.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	[TestClass]
	public class CoinsPaidTest {
		readonly Client Client;

		readonly Client.Configuration Config = new Client.Configuration(
			"https://sandbox.coinspaid.com/api/v2",
			"PUBLIC",
			"SECRET"
		);

		readonly string BTCDestAddress = "ADDRESS";

		public CoinsPaidTest() {
			Client = new Client(Config);
		}

		[TestMethod]
		public async Task PingTest() {
			var result = await Client.Ping();
			Assert.IsTrue(result);
		}

		[TestMethod]
		public async Task CurrenciesListTest() {
			var response = await Client.CurrenciesList();
			Assert.IsTrue(response.Success);
		}

		[TestMethod]
		public async Task CurrenciesPairsTest() {
			var response = await Client.CurrenciesPairs();
			Assert.IsTrue(response.Success);

			response = await Client.CurrenciesPairs(":)");
			Assert.IsFalse(response.Success);

			response = await Client.CurrenciesPairs("BTC");
			Assert.IsTrue(response.Success);

			response = await Client.CurrenciesPairs("BTC", "EUR");
			Assert.IsTrue(response.Success);

			response = await Client.CurrenciesPairs(default, "EUR");
			Assert.IsTrue(response.Success);
		}

		[TestMethod]
		public async Task AccountsListTest() {
			var response = await Client.AccountsList();
			Assert.IsTrue(response.Success);
		}

		[TestMethod]
		public async Task AddressesTakeTest() {
			string id = "TEST";

			var response = await Client.AddressesTake(id, "BTC");
			Assert.IsTrue(response.Success);

			response = await Client.AddressesTake(id, "ETH", "EUR");
			Assert.IsTrue(response.Success);

			response = await Client.AddressesTake(id, "BTC", "USD");
			Assert.IsTrue(response.Success);

			response = await Client.AddressesTake(id, "ETH", "ETH");
			Assert.IsFalse(response.Success);

			response = await Client.AddressesTake(id, ":)", "USD");
			Assert.IsFalse(response.Success);

			response = await Client.AddressesTake(id, "ETH", ":)");
			Assert.IsFalse(response.Success);

			response = await Client.AddressesTake(id, ":(", ":)");
			Assert.IsFalse(response.Success);
		}

		[TestMethod]
		public async Task WithdrawalCryptoTest() {
			var accounts = await Client.AccountsList();
			Assert.IsTrue(accounts.Success);

			var btc = accounts.Result.data.FirstOrDefault(d => d.currency == "BTC");
			var usd = accounts.Result.data.FirstOrDefault(d => d.currency == "USD");
			Assert.IsNotNull(btc);
			Assert.IsNotNull(usd);

			double btcBalance = double.Parse(btc.balance, CultureInfo.InvariantCulture);
			double usdBalance = double.Parse(usd.balance, CultureInfo.InvariantCulture);
			Assert.IsTrue(btcBalance >= 0.01);
			Assert.IsTrue(usdBalance >= 15);

			var response = await Client.WithdrawalCrypto(Guid.NewGuid().ToString("N"), "15", "USD", BTCDestAddress, "BTC");
			Assert.IsTrue(response.Success);

			response = await Client.WithdrawalCrypto(Guid.NewGuid().ToString("N"), "0.001", "BTC", BTCDestAddress);
			Assert.IsTrue(response.Success);
		}

		[TestMethod]
		public async Task ExchnageCalculateTest() {
			var response = await Client.ExchnageCalculateByReceived("USD", "BTC", "0.1");
			Assert.IsTrue(response.Success);

			response = await Client.ExchnageCalculateByReceived("BTC", "USD", "50");
			Assert.IsTrue(response.Success);

			response = await Client.ExchnageCalculateBySent("USD", "BTC", "50");
			Assert.IsTrue(response.Success);

			response = await Client.ExchnageCalculateBySent("BTC", "USD", "0.1");
			Assert.IsTrue(response.Success);

			response = await Client.ExchnageCalculateBySent("BTC", "USD", "0.0001");
			Assert.IsFalse(response.Success);

			response = await Client.ExchnageCalculateBySent("USD", "BTC", "0.1");
			Assert.IsFalse(response.Success);
		}

		[TestMethod]
		public async Task ExchnageFixedTest() {
			var accounts = await Client.AccountsList();
			Assert.IsTrue(accounts.Success);

			var btc = accounts.Result.data.FirstOrDefault(d => d.currency == "BTC");
			var usd = accounts.Result.data.FirstOrDefault(d => d.currency == "USD");
			Assert.IsNotNull(btc);
			Assert.IsNotNull(usd);

			double btcBalance = double.Parse(btc.balance, CultureInfo.InvariantCulture);
			double usdBalance = double.Parse(usd.balance, CultureInfo.InvariantCulture);
			Assert.IsTrue(btcBalance >= 0.01);
			Assert.IsTrue(usdBalance >= 100);

			var rate = await Client.ExchnageCalculateBySent("BTC", "USD", "0.01");
			Assert.IsTrue(rate.Success);
			var exchange = await Client.ExchangeFixed(Guid.NewGuid().ToString("N"), "BTC", "USD", "0.01", rate.Result.data.price);
			Assert.IsTrue(exchange.Success);

			rate = await Client.ExchnageCalculateBySent("USD", "BTC", "100");
			Assert.IsTrue(rate.Success);
			exchange = await Client.ExchangeFixed(Guid.NewGuid().ToString("N"), "USD", "BTC", "100", rate.Result.data.price);
			Assert.IsTrue(exchange.Success);
		}

		[TestMethod]
		public async Task ExchnageNowTest() {
			var accounts = await Client.AccountsList();
			Assert.IsTrue(accounts.Success);

			var btc = accounts.Result.data.FirstOrDefault(d => d.currency == "BTC");
			var usd = accounts.Result.data.FirstOrDefault(d => d.currency == "USD");
			Assert.IsNotNull(btc);
			Assert.IsNotNull(usd);

			double btcBalance = double.Parse(btc.balance, CultureInfo.InvariantCulture);
			double usdBalance = double.Parse(usd.balance, CultureInfo.InvariantCulture);
			Assert.IsTrue(btcBalance >= 0.01);
			Assert.IsTrue(usdBalance >= 100);

			var exchange = await Client.ExchangeNow(Guid.NewGuid().ToString("N"), "BTC", "USD", "0.01");
			Assert.IsTrue(exchange.Success);

			exchange = await Client.ExchangeNow(Guid.NewGuid().ToString("N"), "USD", "BTC", "100");
			Assert.IsTrue(exchange.Success);
		}
	}
}
