using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Collections.Generic;
using Newtonsoft.Json;

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
            }else if (  req.Url.AbsolutePath == "/mine")
            {

                //
                // Write the response info
                //

                //Verifier la veraciter des info ( presence d'un montant , en chiffre ect))
                string idTrans = req.QueryString["idTrans"]; // verier si bien ash 256  et si hash de oTRash = a lui
                string oTrans = req.QueryString["oTrans"];  // Doit devenir un objet
                string difficulté = "0";
                byte[] data;
                if (idTrans != null && oTrans != null)
                {

                     bool verif = VerifTransaction(oTrans);
                     string hash = HashSHA256(oTrans);
                    if (!verif)
                    {
                        
                        data = Encoding.UTF8.GetBytes(" une erreur est survenue Verifier vos valeur puis reesayer");
                       
                    }else if (hash == idTrans && hash != null )
                     {
                         if (hash.StartsWith(difficulté))
                         {
                             data = Encoding.UTF8.GetBytes(" Difficulté respecté un nouveau block vien d'etre crée ! difficulté :  " + difficulté + "hash : " + hash);
                         }
                         else 
                         { 
                            data = Encoding.UTF8.GetBytes("Les Hash Correspondent !! origine : " + idTrans + " hash : " + hash + " Difficulté " + difficulté); 
                         }
                     }
                    else
                     {
                        data = Encoding.UTF8.GetBytes("Les Hash ne corresponde pas " + idTrans + " hash : " + hash);
                     }
                }
                else 
                {
                    data = Encoding.UTF8.GetBytes("Parametre non valide");
                }
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
                
            
                loadingScreen(data,resp);
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
    private bool VerifBlockValide(string hash)
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
    /// Verifie si les donnée envoyer par le clients sont entière
    /// </summary>
    /// <param name="json"></param>
    /// <returns>Bool</returns>
    private static bool VerifTransaction(string json)
    {
        try
        {
            Transaction transaction = JsonConvert.DeserializeObject<Transaction>(json);
            return true;
        }
        catch (Exception ex)
        {
            
            return false;
        }
           
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