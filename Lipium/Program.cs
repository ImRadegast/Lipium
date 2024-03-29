﻿using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;

class HttpServer
{
    public static HttpListener listener;
    public static string url = "http://25.29.51.211:8000/";
    //public static int pageViews = 0;
    public static int requestCount = 0;
    public static string pageData =
        "<!DOCTYPE>" +
        "<html>" +
        "  <head>" +
        "    <title>Achetez Lipium et rendez moi riche</title>" +
        "  </head>" +
        "  <body>" +
        //"    <p>Page Views: {0}</p>" +
        "    <form method=\"post\" action=\"shutdown\">" +
        "      <input type=\"submit\" value=\"Shutdown\" {0}>" +
        "    </form>" +
        "  </body>" +
        "</html>";


    public static async Task HandleIncomingConnections()
    {
        bool runServer = true;
        List<Transaction> lstTransactions = new List<Transaction>();
        // While a user hasn't visited the `shutdown` url, keep on handling requests
        while (runServer)
        {
            // Will wait here until we hear from a connection
            HttpListenerContext ctx = await listener.GetContextAsync();
            // Peel out the requests and response objects
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;


            // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
            if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
            {
                Console.WriteLine("Shutdown requested");
                runServer = false;
            } else if (req.Url.AbsolutePath == "/mine")
            {

                //
                // Write the response info
                //

                //Verifier la veraciter des info ( presence d'un montant , en chiffre ect))
                string idTrans = req.QueryString["idTrans"]; // verier si bien ash 256  et si hash de oTRash = a lui
                string oTrans = req.QueryString["oTrans"];  // Doit devenir un objet
                string difficulté = "0";
                byte[] data;

                if (VerifTransaction(idTrans, oTrans))
                {
                    Transaction transaction = JsonConvert.DeserializeObject<Transaction>(oTrans);
                    lstTransactions.Add(transaction);
                    data = Encoding.UTF8.GetBytes("Les Hash Correspondent !! transaction enregistrer ");
                    
                    int nonce = 0;
                    string hash ="";
                    bool hashverifier = false;

                    while (!hashverifier) 
                    { 
                        hash = HashTransaction(lstTransactions, nonce);
                        hashverifier = VerifBlockValide(hash);
                        nonce++;
                    }

                    Block block = CreateNewBlock(hash, nonce, lstTransactions);
                    

                }
                else data = Encoding.UTF8.GetBytes("Information Invalide");
                
              
                loadingScreen(data, resp);
            }
            else
            {
                string disableSubmit = !runServer ? "disabled" : "";
                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, disableSubmit));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                // Write the response info


                loadingScreen(data, resp);
            }

        }
    }


    /// <summary>
    /// Gere l'affichage des differente reponses de la page mineur
    /// </summary>
    /// <param name="data"></param>
    /// <param name="resp"></param>
    public async static void loadingScreen(byte[] data, HttpListenerResponse resp)
    {
        // Write out to the response stream (asynchronously), then close it
        await resp.OutputStream.WriteAsync(data, 0, data.Length);
        resp.Close();

    }

    /// <summary>
    /// Hash une list de Transaction avec un nonce 
    /// </summary>
    /// <param name="lstTransaction"></param>
    /// <param name="nonce"></param>
    /// <returns> String</returns>
    private static string HashTransaction(List<Transaction> lstTransaction, int nonce)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var transaction in lstTransaction)
            {
                stringBuilder.Append(transaction);
            }

            stringBuilder.Append(nonce);

            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));

            StringBuilder hashBuilder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashBuilder.Append(hashBytes[i].ToString("x2")); // Convertit chaque octet en sa représentation hexadécimale
            }

            return hashBuilder.ToString();
        }
    }

    /// <summary>
    /// Renvoie la difficulté actuelle
    /// </summary>
    /// <returns>String </returns>
    private static string GetDifficulty()
    {
        string difficulty = "";
        return difficulty;
    }

    /// <summary>
    /// Verifie si le hash permet la creation d'un nouveau block par rapport a la difficulté
    /// </summary>
    /// <param name="hash"></param>
    /// <returns>Boolean</returns>
    public static bool VerifBlockValide(string hash)
    {
        string difficulty = GetDifficulty();
        bool retour = hash.StartsWith(difficulty) ? true : false;
        return retour;
    }
    public class Block
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string PreviousHash { get; set; }
        public string Hash { get; set; }
        public int Nonce { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
    private static Block CreateNewBlock(string hash, int nonce, List<Transaction> lstTransaction)
    {

        
        /*Task<int> lastBlockTask = GetLastBlock();
        int lastblock = lastBlockTask.GetAwaiter().GetResult();*/


        Block block = new Block();

        block.Index = 0; //lastblock;
        block.Timestamp = DateTime.Now;
        block.PreviousHash = "modifQuandJoryAFini";
        block.Hash = hash;
        block.Nonce = nonce;
        block.Transactions = lstTransaction;
        return block;

    }
    private async static void SendBlockDB(Block block)
    {
        try {
            string jsonString = JsonConvert.SerializeObject(block);
            string url = "http://25.28.20.82:8000/storage?block="+jsonString;

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();
            }
        }
        catch(Exception e) { throw new InvalidOperationException(e.Message); }   
    }
    /// <summary>
    /// Envoie une Requete a l'api de la BDD 
    /// </summary>
    /// <returns>id du dernier block int </returns>
    private async static Task<int> GetLastBlock()
    {
        string url = "http://25.28.20.82:8000/lastblock";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            string responseBody = await response.Content.ReadAsStringAsync();
            int lastBlock = int.Parse(responseBody);

            return lastBlock;
        }
    }
    
    /// <summary>
    /// hash une seule valeur en Sha256
    /// </summary>
    /// <param name="valeur"></param>
    /// <returns>hash sous forme de String </returns>
    private static string HashSHA256(string valeur)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(valeur));
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                stringBuilder.Append(hashBytes[i].ToString("x2")); // Convertit chaque octet en sa représentation hexadécimale
            }

            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// Verifie si les donnée sont vide et si les hash correspondent
    /// </summary>
    /// <param name="idTrans"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    public static bool VerifTransaction(string idTrans, string json)
    {
        //verifie si les donnée entrée sont vide
        if (string.IsNullOrEmpty(idTrans) && (string.IsNullOrEmpty(json)))
        {
            //hash le json pour vérifier qu'il coincide avec le hash envoyé
            string hash = HashSHA256(json);
            if (hash == idTrans)
            {
                return true;
            }
            else return false;
       
        }else return false;

    }
    public class Transaction
    {
        public string IdExp { get; set; }
        public string IdRcv { get; set; }
        public decimal Montant { get; set; }
    }
    
    public static void Main(string[] args)
    {
        // Create a Http server and start listening for incoming connections
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine("Listening for connections on {0}", url);

        // Handle requests
        Task listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();

        // Close the listener
        listener.Close();
    }
}