using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.KeyStore;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TestNethereum
{
    class Program
    {
        const string Endpoint = "http://cqethe3bu.southeastasia.cloudapp.azure.com:8545";
        const string AdminPortal = "http://cqethe3bu.southeastasia.cloudapp.azure.com";

        static void Main(string[] args)
        {

            Console.WriteLine("Membuat account baru. Tidak perlu terhubung ke Ethereum Node");
            Console.WriteLine();
            Account account = CreateUser();
            Console.WriteLine();
            Console.WriteLine("Account berhasil dibuat: " + account.Address);
            Console.WriteLine();
            Console.WriteLine("Ketik enter untuk memulai koneksi ke Ethereum Node...");
            Console.ReadLine();

            //Web3 web3 = new Web3(account, Endpoint);
            Web3 web3 = new Web3(account);

            Console.WriteLine();
            Console.WriteLine("------------------------------");
            Console.WriteLine("Sebelum melanjutkan, harap buka halaman admin di browser:");
            Console.WriteLine();
            Console.WriteLine(AdminPortal);
            Console.WriteLine();
            Console.WriteLine("PASTE public key yang tadi sudah dicopy ke textbox admin portal lalu klik \"Submit\" untuk menerima Ether");
            Console.WriteLine("Pastikan ada response \"Ether sent!\" lalu tunggu 1-2 menit");
            Console.WriteLine();
            Console.WriteLine("------------------------------");
            Console.WriteLine();

            BigInteger balance = GetBalance(web3, account).GetAwaiter().GetResult();

            while (balance == 0)
            {
                Console.WriteLine("Anda belum meminta Ether dari admin portal atau pengiriman Ether belum selesai diproses yang menyebabkan account anda memiliki 0 Ether!");
                balance = GetBalance(web3, account).GetAwaiter().GetResult();
            }

            Console.WriteLine();
            Console.WriteLine("Ketik enter untuk demo mengirimkan Ether...");
            Console.ReadLine();

            SendEther(web3, account).GetAwaiter().GetResult();

            Console.WriteLine("Ketik enter untuk demo membaca smart contract...");
            Console.ReadLine();

            //var res = ReadContract(web3, account).GetAwaiter().GetResult();
            var res = ChainyContract(web3, account).GetAwaiter().GetResult();

            Console.ReadLine();
        }

        static Account CreateUser()
        {
            Console.WriteLine("Membuat account baru...");
            Console.WriteLine("Masukkan password baru:");
            var password = Console.ReadLine();

            //Generate a private key pair using SecureRandom
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();

            //Get the public address (derivied from the public key)
            var address = ecKey.GetPublicAddress();

            Console.WriteLine("User berhasil dibuat!");
            Console.WriteLine();
            Console.WriteLine("------------------------------");
            Console.WriteLine("HARAP COPY PUBLIC KEY ADDRESS:");
            Console.WriteLine();
            Console.WriteLine(address);
            Console.WriteLine();
            Console.WriteLine("------------------------------");
            Console.WriteLine();
            Console.WriteLine("Ketik enter untuk melanjutkan");
            Console.ReadLine();
            Console.WriteLine("Melakukan enkripsi private key, harap tunggu...");

            //Create a store service, to encrypt and save the file using the web3 standard
            var service = new KeyStoreService();
            var encryptedKey = service.EncryptAndGenerateDefaultKeyStoreAsJson(password, ecKey.GetPrivateKeyAsBytes(), address);

            Console.WriteLine("Private key berhasil dienkripsi dengan password!");
            Console.WriteLine("JSON hasil enkripsi private key:");
            Console.WriteLine(encryptedKey);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Ketik enter untuk mendapatkan objek Account!");
            Console.ReadLine();
            Console.WriteLine();
            Console.WriteLine("Mendekripsi json menjadi account, harap tunggu...");

            Account account = Account.LoadFromKeyStore(encryptedKey, password);

            return account;
        }

        static async Task<BigInteger> GetBalance(Web3 web3, Account account)
        {
            Console.WriteLine("Ketik enter untuk mengecek balance...");
            Console.ReadLine();

            Console.WriteLine("Mendapatkan Ether balance, harap tunggu...");
            var currentBalance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            Console.WriteLine("Account address: " + account.Address);
            Console.WriteLine("Account balance: " + currentBalance.Value);

            return currentBalance.Value;
        }

        static async Task SendEther(Web3 web3, Account account)
        {
            Console.WriteLine("Membuat account penerima dengan random private key...");

            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            Account receiver = new Account(ecKey.GetPrivateKey());

            Console.WriteLine("Account penerima berhasil dibuat: " + receiver.Address);

            //The transaction receipt polling service is a simple utility service to poll for receipts until mined
            var transactionPolling = web3.TransactionManager.TransactionReceiptService;

            Console.WriteLine("Ketik enter untuk melanjutkan...");
            Console.ReadLine();
            Console.WriteLine("Mendapatkan informasi balance pengirim, harap tunggu...");
            var senderBalance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            Console.WriteLine("Address dari account pengirim: " + account.Address);
            Console.WriteLine("Balance dari account pengirim: " + senderBalance.Value);
            Console.WriteLine();
            Console.WriteLine("Mendapatkan informasi balance penerima, harap tunggu...");
            var currentBalance = await web3.Eth.GetBalance.SendRequestAsync(receiver.Address);
            Console.WriteLine("Address dari account penerima: " + receiver.Address);
            Console.WriteLine("Balance dari account penerima: " + currentBalance.Value);
            Console.WriteLine();
            Console.WriteLine("Ketik enter untuk mengirim ether ke account penerima...");
            Console.ReadLine();
            //assumed client is mining already
            //when sending a transaction using an Account, a raw transaction is signed and send using the private key
            Console.WriteLine("Mengeksekusi transaksi pengiriman Ether, harap tunggu...");
            var transactionReceipt = await transactionPolling.SendRequestAsync(() =>
                web3.TransactionManager.SendTransactionAsync(account.Address, receiver.Address, new HexBigInteger(1000000))
            );

            Console.WriteLine("Mendapatkan Transaction Hash: " + transactionReceipt.TransactionHash);
            Console.WriteLine("Gas yang digunakan: " + transactionReceipt.CumulativeGasUsed.Value);
            Console.WriteLine();
            Console.WriteLine("Mendapatkan informasi balance baru pengirim, harap tunggu...");
            senderBalance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            Console.WriteLine("Address dari account pengirim: " + account.Address);
            Console.WriteLine("Balance dari account pengirim: " + senderBalance.Value);
            Console.WriteLine();
            Console.WriteLine("Mendapatkan informasi balance baru penerima, harap tunggu...");
            var newBalance = await web3.Eth.GetBalance.SendRequestAsync(receiver.Address);
            Console.WriteLine("Address dari account penerima: " + receiver.Address);
            Console.WriteLine("Balance dari account penerima: " + newBalance.Value);
        }

        static async Task<bool> ReadContract(Web3 web3, Account account)
        {
            Console.WriteLine("Berinteraksi dengan object Contract \"SimpleStorage\":");
            Console.WriteLine();
            Console.WriteLine("\"SimpleStorage\" memiliki 3 variable: string, integer, dan mapping (array)");
            Console.WriteLine("Demo ini akan membaca ketiga variable tersebut dan menampilkan isinya");
            Console.WriteLine();
            string contractABI = "[{\"constant\":false,\"inputs\":[],\"name\":\"getStorageInteger\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":true,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"getStorageArray\",\"outputs\":[{\"name\":\"\",\"type\":\"bytes32[4]\"}],\"payable\":true,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"storageInteger\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":true,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"storageString\",\"outputs\":[{\"name\":\"\",\"type\":\"bytes32\"}],\"payable\":true,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"getStorageString\",\"outputs\":[{\"name\":\"\",\"type\":\"bytes32\"}],\"payable\":true,\"type\":\"function\"},{\"inputs\":[],\"payable\":true,\"type\":\"constructor\"}]";
            string contractAddress = "0xa14b9156aae0acdb6a22adef3dd275511a742bbd";

            Contract contract = web3.Eth.GetContract(contractABI, contractAddress);
            Console.WriteLine("Ketik enter untuk melanjutkan");
            Console.ReadLine();
            Console.WriteLine("Memanggil string...");
            Function getString = contract.GetFunction("getStorageString");
            var resultstring = await getString.CallAsync<string>();

            Console.WriteLine("Mendapatkan result dari string yang disimpan: " + resultstring);
            Console.WriteLine("Ketik enter untuk melanjutkan");
            Console.ReadLine();

            Console.WriteLine("Memanggil integer...");
            Function getInteger = contract.GetFunction("getStorageInteger");
            var resultint = await getInteger.CallAsync<int>();

            Console.WriteLine("Mendapatkan result dari integer yang disimpan: " + resultint);
            Console.WriteLine("Ketik enter untuk melanjutkan");
            Console.ReadLine();

            Console.WriteLine("Memanggil array...");
            Function getArrayIndex = contract.GetFunction("getStorageArray");
            var resultarray = await getArrayIndex.CallAsync<List<string>>();

            Console.WriteLine("Mendapatkan result dari array: ");
            for (int i = 0; i < resultarray.Count; i++)
            {
                Console.WriteLine("Index " + i + ": " + resultarray[i]);
            }
            Console.WriteLine();

            Console.WriteLine("Ketik enter untuk selesai");

            return true;
        }

        static async Task<bool> ChainyContract(Web3 web3, Account account)
        {
            string contractABI = "[{\"constant\":false,\"inputs\":[{\"name\":\"code\",\"type\":\"string\"}],\"name\":\"getChainyData\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"view\"},{\"constant\":true,\"inputs\":[{\"name\":\"_address\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"bool\"}],\"name\":\"setServiceAccount\",\"outputs\":[],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"nonpayable\"},{\"constant\":false,\"inputs\":[],\"name\":\"getChainyURL\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"view\"},{\"constant\":true,\"inputs\":[{\"name\":\"_key\",\"type\":\"string\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"setConfig\",\"outputs\":[],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"nonpayable\"},{\"constant\":true,\"inputs\":[],\"name\":\"releaseFunds\",\"outputs\":[],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"nonpayable\"},{\"constant\":true,\"inputs\":[{\"name\":\"_address\",\"type\":\"address\"}],\"name\":\"setReceiverAddress\",\"outputs\":[],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"nonpayable\"},{\"constant\":false,\"inputs\":[],\"name\":\"owner\",\"outputs\":[{\"name\":\"\",\"type\":\"address\"}],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"view\"},{\"constant\":false,\"inputs\":[{\"name\":\"code\",\"type\":\"string\"}],\"name\":\"getChainySender\",\"outputs\":[{\"name\":\"\",\"type\":\"address\"}],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"view\"},{\"constant\":true,\"inputs\":[{\"name\":\"json\",\"type\":\"string\"}],\"name\":\"addChainyData\",\"outputs\":[],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"nonpayable\"},{\"constant\":false,\"inputs\":[{\"name\":\"_key\",\"type\":\"string\"}],\"name\":\"getConfig\",\"outputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"view\"},{\"constant\":false,\"inputs\":[{\"name\":\"code\",\"type\":\"string\"}],\"name\":\"getChainyTimestamp\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"view\"},{\"constant\":true,\"inputs\":[{\"name\":\"_url\",\"type\":\"string\"}],\"name\":\"setChainyURL\",\"outputs\":[],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"nonpayable\"},{\"constant\":true,\"inputs\":[{\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"transferOwnership\",\"outputs\":[],\"payable\":true,\"type\":\"function\",\"stateMutability\":\"nonpayable\"},{\"inputs\":[],\"payable\":true,\"type\":\"constructor\",\"stateMutability\":\"nonpayable\"},{\"anonymous\":true,\"inputs\":[{\"indexed\":true,\"name\":\"timestamp\",\"type\":\"uint256\"},{\"indexed\":true,\"name\":\"code\",\"type\":\"string\"}],\"name\":\"chainyShortLink\",\"type\":\"event\"}]";
            string contractAddress = "0xff279852ed3532c700343768d7cabd0c3852e5df";

            Contract contract = web3.Eth.GetContract(contractABI, contractAddress);

            var addChainyData = contract.GetFunction("addChainyData");
            var chainyShortLinkEvent = contract.GetEvent("chainyShortLink");
            var filterAll = await chainyShortLinkEvent.CreateFilterAsync();

            var jsonstring = "{\"id\":\"smartinvoice\",\"version\":1,\"type\":\"L\",\"filename\":\"apple.jpg\",\"hash\":\"f2551293c0cc13f8bdf8f04c4c220ac6613d8703a0d076bde54a5ae74a9a3583\",\"filetype\":\"img\",\"filesize\":\"22665\"}";

            TransactionInput input = new TransactionInput();
            input.From = account.Address;

            var price = addChainyData.EstimateGasAsync(jsonstring).GetAwaiter().GetResult();
            input.Gas = price;

            var transaction = await addChainyData.SendTransactionAndWaitForReceiptAsync(input, null, jsonstring);
            Console.WriteLine(transaction.TransactionHash);

            //var log = await chainyShortLinkEvent.GetFilterChanges<ChainyShortLinkEvent>(filterAll);

            //Console.WriteLine("Total event count: " + log.Count.ToString());
            //Console.WriteLine(log[0].Event.Code); -> pasti error

            return true;
        }
    }

    public class ChainyShortLinkEvent
    {
        [Parameter("int", "timestamp", 1, true)]
        public int Timestamp { get; set; }

        [Parameter("string", "code", 2, true)]
        public string Code { get; set; }
    }
}
