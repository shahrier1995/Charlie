using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace TSGG_ServerEntityFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            APIModel.LoginResponse obj = loginApiCall();
            string accoutToken = obj.accountToken;
            string storeIdentifier = obj.storeIdentifier;
           getExportData(accoutToken, storeIdentifier);
            importWhiteList(accoutToken, storeIdentifier);
            Console.ReadLine();

        }

        public static APIModel.LoginResponse loginApiCall()
        {
            string URL = ConfigurationManager.AppSettings["APILoginURL"];
            URL +=  "/login";
            var client = new RestClient(URL);
           // var client = new RestClient("https://walnutbackend.com/api/v1/login");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", "{ \"accountEmailAddress\" : \"shahrier.harun@tokheim-service.de\", \"accountPassword\" : \"TheimWalnut2021\", \"partnerToken\" : \"f2a04faebc912a86961257c9168b14f2793a00b6c1ebdfdec3e65535c9c3b0c0619629866841f9510b3b1c7ab99a49d9a5a1020ab2468a14186d1c477ad52a1c\" }", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {

                var rawResponse = JsonConvert.DeserializeObject<APIModel.accountTokenObject>(response.Content);
                Console.WriteLine("Login Successfull to walnut API");
                APIModel.LoginResponse obj = new APIModel.LoginResponse();
                obj.accountToken = rawResponse.accountToken;
                obj.storeIdentifier = rawResponse.storeIdentifier;

                return obj;


            }
            else
            {
                Console.WriteLine("Login failed..");
                return null;
            }
        }
        public static void getExportData(string accountTokenParam,string userIdentifier)
        {
            using (var result = new TSGGEntities())
            {
                TSGGEntities context = new TSGGEntities();
                DateTime toDate = DateTime.Now;
                string format = "dd-MM-yyyy";
                string formatYearWise = "yyyy-MM-dd";
                var results = (from a in context.tbl_Transaction
                               orderby a.Transaction_DateTime descending

                               select a.Transaction_DateTime).ToList();
                string dateFromDb = results.FirstOrDefault().ToString();
                if(string.IsNullOrEmpty(dateFromDb))
                {
                    dateFromDb = "01/01/2019 9:54:22 AM";
                }
                DateTime fromDate = Convert.ToDateTime(dateFromDb).AddDays(1);
                string URL= ConfigurationManager.AppSettings["APIBaseURL"];
                URL += userIdentifier + "/scans/dashboard?start=" + fromDate.ToString(format) + "&end=" + toDate.ToString(format) + "&page=&page=1";
                var client = new RestClient(URL);
                //var client = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/scans/dashboard?start=" + fromDate.ToString(format) + "&end=" + toDate.ToString(format) + "&page=&page=1");
                //var client = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/scans/dashboard?start=02-11-2020&end=30-03-2021&page=&page=1");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "WalnutPass " + accountTokenParam);
                IRestResponse response = client.Execute(request);
                var rawResponse = JsonConvert.DeserializeObject<APIModel.Rootobject>(response.Content);
                string totalResultPage = rawResponse.storeScans.totalResultPages.ToString();
                Console.WriteLine("Export Result Page found from " + fromDate + " to " + toDate + " is : " + totalResultPage);
                //StringBuilder csvContent = new StringBuilder();
                //csvContent.AppendLine("Card Number,Terminal/Store ID,Amount Spend, Date and Time");
                for (int j = 1; j <= Convert.ToInt32(totalResultPage); j++)
                {

                    //var newClient = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/scans/dashboard?start=02-11-2020&end=30-03-2021&page=" + j);
                    string NewURL = ConfigurationManager.AppSettings["APIBaseURL"];
                    NewURL += userIdentifier + "/scans/dashboard?start=" + fromDate.ToString(format) + "&end=" + toDate.ToString(format) + "&page=" + j;
                    var newClient = new RestClient(NewURL);
                    //var newClient = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/scans/dashboard?start=" + fromDate.ToString(format) + "&end=" + toDate.ToString(format) + "&page=" + j);
                    newClient.Timeout = -1;
                    var newRequest = new RestRequest(Method.GET);
                    newRequest.AddHeader("Authorization", "WalnutPass " + accountTokenParam);
                    IRestResponse newResponse = newClient.Execute(newRequest);
                    var newRawResponse = JsonConvert.DeserializeObject<APIModel.Rootobject>(newResponse.Content);
                    for (int i = 0; i < newRawResponse.storeScans.results.Length; i++)
                    {
                        string cardNumber = newRawResponse.storeScans.results[i].passBarcodeMessage;
                        string terminalID = newRawResponse.storeScans.results[i].accountEmailAddress;
                        string amountSpend = newRawResponse.storeScans.results[i].passScans;
                        string dateTime = newRawResponse.storeScans.results[i].passScanned;
                        var innerJoinForStation = (from a in context.tbl_Terminal
                                                  join b in context.tbl_Station on a.Station_ID equals b.ID
                                                  where a.Terminal_Name == terminalID
                                                  select b.Station_Name).ToList();
                        string stationName = innerJoinForStation.FirstOrDefault().ToString();
                        var innerJoinForClient = (from a in context.tbl_Station
                                                 join b in context.tbl_Terminal on a.ID equals b.Station_ID
                                                 join c in context.tbl_Client on a.Client_ID equals c.ID
                                                 where b.Terminal_Name == terminalID
                                                 select c.Client_Name).ToList();

                        string clientName = innerJoinForClient.FirstOrDefault().ToString();
                        //string stationName = objCDA.getSingleValue("select b.Station_Name from tbl_Terminal a inner join  tbl_Station b on a.Station_ID=b.ID where a.Terminal_Name='" + terminalID + "'", "TSGG").ToString();
                        //string clientName = objCDA.getSingleValue("select c.Client_UniqueString from tbl_Station a inner join tbl_Terminal b on a.ID = b.Station_ID inner join tbl_Client c on a.Client_ID = c.ID where b.Terminal_Name='" + terminalID + "'", "TSGG").ToString();
                        string folderPath = "C:\\Files\\Export Files\\" + clientName;

                        bool exists = Directory.Exists(folderPath);

                        if (!exists)
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        StringBuilder csvContent = new StringBuilder();


                        if (!File.Exists(folderPath + "\\" + DateTime.Parse(dateTime).ToString(formatYearWise) + ".csv"))
                        {
                            //csvContent.AppendLine("Card Number,Terminal ID,Station Name,Amount Spend,Date and Time");
                            csvContent.AppendLine("Card Number;Amount Spend;Date and Time;Station Name");
                            csvContent.AppendLine(cardNumber + ";" + amountSpend + ";" + dateTime + ";" + stationName);
                            string csvPath = folderPath + "\\" + DateTime.Parse(dateTime).ToString(formatYearWise) + ".csv";
                            File.AppendAllText(csvPath, csvContent.ToString());
                        }
                        else
                        {
                            //csvContent.AppendLine(cardNumber + "," + terminalID + "," + stationName + "," + amountSpend + "," + dateTime);
                            csvContent.AppendLine(cardNumber + ";" + amountSpend + ";" + dateTime + ";" + stationName);
                            string csvPath = folderPath + "\\" + DateTime.Parse(dateTime).ToString(formatYearWise) + ".csv";
                            File.AppendAllText(csvPath, csvContent.ToString());
                        }
                        string ID = Guid.NewGuid().ToString();
                        DateTime TimeStamp = DateTime.Now;

                        //craeteFolder(terminalID,stationName, clientName);
                        //csvContent.AppendLine(cardNumber + "," + terminalID + "," + amountSpend + "," + dateTime);
                        try
                        {
                            tbl_Transaction objTransaction = new tbl_Transaction();
                            objTransaction.ID = ID;
                            objTransaction.Card_Number = cardNumber;
                            objTransaction.Amount_Spend = amountSpend;
                            objTransaction.Transaction_DateTime = DateTime.Parse(dateTime);
                            objTransaction.Station_Name = stationName;
                            objTransaction.Terminal_ID = terminalID;
                            objTransaction.Client_Name = clientName;
                            objTransaction.TimeStamp = TimeStamp;
                            context.tbl_Transaction.Add(objTransaction);
                            context.SaveChanges();
                        }
                        catch (DbEntityValidationException e)
                        {
                            foreach (var eve in e.EntityValidationErrors)
                            {
                                Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                    eve.Entry.Entity.GetType().Name, eve.Entry.State);
                                foreach (var ve in eve.ValidationErrors)
                                {
                                    Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                                        ve.PropertyName, ve.ErrorMessage);
                                }
                            }
                            throw;
                        }
      


                        //objCDA.ExecuteNonQuery("INSERT INTO tbl_Transaction (Card_Number,Amount_Spend,Transaction_DateTime,Station_Name,Terminal_ID,Client_Name) VALUES ('" + cardNumber + "','" + amountSpend + "','" + DateTime.Parse(dateTime) + "','" + stationName + "','" + terminalID + "','" + clientName + "'); ", "TSGG");

                    }
                    Console.WriteLine("Resul page no : " + j + " is exported");
                    //string csvPath = "C:\\Files\\Export Files\\" + toDate.ToString(format) + ".csv";
                    //File.AppendAllText(csvPath, csvContent.ToString());
                }

            }

        }

        public static void importWhiteList(string accountTokenParam,string storeIdentifier)
        {
            using (var result = new TSGGEntities())
            {
                TSGGEntities context = new TSGGEntities();
                List<tbl_Client> clientList = result.tbl_Client.ToList();
              
                if (clientList != null)
                {
                    foreach (tbl_Client obj in clientList)
                    {
                        string path = obj.Client_Folder_Address;
                        string cliientID = obj.ID;
                        string clientName = obj.Client_Name;
                        string clientUserName = obj.Client_UserName;
                        string clientEmailAddress = obj.Client_EmailAddress;
                        string[] fileName = Directory.GetFiles(path);

                        //List<SubscriptionTypeModel> list =(List<SubscriptionTypeModel>)SubcriptionType;

                        for (int k = 0; k < fileName.Length; k++)
                        {
                            string csvData = File.ReadAllText(fileName[k]);
                            List<string> cardNumberList = new List<string>();
                            //Execute a loop over the rows.  
                            foreach (string row in csvData.Split('\n'))
                            {
                                if (!string.IsNullOrEmpty(row))
                                {
                                    cardNumberList.Add(row);
                                }
                            }

                            //string stationName = Path.GetFileNameWithoutExtension(fileName[k]);
                            //Application _excel = new excel.Application();
                            //Workbook workBook;
                            //Worksheet workSheet;
                            //workBook = _excel.Workbooks.Open(fileName[k]);
                            //workSheet = workBook.Worksheets[1];
                            int i = 1;
                            //int cell = workSheet.UsedRange.Rows.Count;C:\Files\Import\A270B9D2-5F40-49F1-A06F-1D102F253261

                            for (i = 1; i < cardNumberList.Count; i++)
                            {

                                string cardNumberWithChar = cardNumberList[i].ToString();
                                string cardNumber = cardNumberWithChar.Remove(cardNumberWithChar.Length - 2, 1);
                                cardNumber = cardNumber.Replace("\r", string.Empty);

                                tbl_IsCardExist objTempCardNumber = new tbl_IsCardExist();
                                objTempCardNumber.Card_Number = cardNumber;
                                objTempCardNumber.ID = Guid.NewGuid().ToString();
                                context.tbl_IsCardExist.Add(objTempCardNumber);
                                context.SaveChanges();

                                var cardList = context.tbl_CardList.Where(s => s.Card_Number == cardNumber && s.User_Status == 1).FirstOrDefault<tbl_CardList>();


                                if (cardList != null)
                                {
                                    Console.WriteLine("No new card number is found");
                                    continue;

                                }
                                else
                                {
                                    Console.WriteLine("New Card Number Found. Which is : " + cardNumber);
                                    string URL = ConfigurationManager.AppSettings["APIBaseURL"];
                                    URL += storeIdentifier + "/pass/";
                                    var client = new RestClient(URL);
                                    //var client = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/pass/");
                                    client.Timeout = -1;
                                    var request = new RestRequest(Method.POST);
                                    request.AddHeader("Authorization", "WalnutPass " + accountTokenParam);
                                    request.AddHeader("Content-Type", "application/json");
                                    request.AddParameter("application/json", "{ \"storecardActivationCode\" : \"" + cardNumber + "\", \"userName\" : \"" + clientUserName + "\", \"userEmail\" : \"" + clientEmailAddress + "\" }", ParameterType.RequestBody);
                                    IRestResponse response = client.Execute(request);
                                    var rawResponse = JsonConvert.DeserializeObject<APIModel.NewMemberObject>(response.Content);
                                    Console.WriteLine("Card Number : " + cardNumber + " is added into member list of Walnut");
                                    string WalnutuserIdentifier = rawResponse.userIdentifier.ToString();
                                    string newURL = ConfigurationManager.AppSettings["APIBaseURL"];
                                    newURL += storeIdentifier + "/user/" + WalnutuserIdentifier + "/subscription";
                                    var clientSubscription = new RestClient(newURL);
                                    //var clientSubscription = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/user/" + WalnutuserIdentifier + "/subscription");
                                    client.Timeout = -1;
                                    var requestSubscription = new RestRequest(Method.PUT);
                                    requestSubscription.AddHeader("Authorization", "WalnutPass " + accountTokenParam);
                                    requestSubscription.AddHeader("Content-Type", "application/json");
                                    var SubcriptionType = from a in context.tbl_SubscriptionType
                                                          join b in context.tbl_Client on a.ID equals b.SubscriptionType_ID
                                                          where b.ID == cliientID
                                                          select new
                                                          {
                                                              Subscription_Name = a.Subscription_Name,
                                                              Subscription_Description = a.Subscription_Description,
                                                              Subscription_Price = a.Subscription_Price,
                                                              Subscription_Program = a.Subscription_Program,
                                                              Subscription_Recurring_Payment = a.Subscription_Recurring_Payment,
                                                              Subscription_Status = a.Subscription_Status
                                                          };
                                    //dsSubscriptionType = objCDA.GetDataSet("SELECT a.Subscription_Name,a.Subscription_Description,a.Subscription_Price,a.Subscription_Program,a.Subscription_Recurring_Payment,a.Subscription_Status from tbl_SubscriptionType a inner join tbl_Client b on a.ID=b.SubscriptionType_ID where b.ID='" + cliientID + "'", "TSGG");
                                    string subscriptionName = SubcriptionType.FirstOrDefault().Subscription_Name.ToString();
                                    string subscriptionDescription = SubcriptionType.FirstOrDefault().Subscription_Description.ToString();
                                    string subscriptionPrice = SubcriptionType.FirstOrDefault().Subscription_Price.ToString();
                                    string subscriptionStatus = SubcriptionType.FirstOrDefault().Subscription_Status.ToString();
                                    string subscriptionProgram = SubcriptionType.FirstOrDefault().Subscription_Program.ToString();
                                    string subscriptionRecurringPayment = SubcriptionType.FirstOrDefault().Subscription_Recurring_Payment.ToString();

                                    requestSubscription.AddParameter("application/json", "{ \"subscriptionName\": \"" + subscriptionName + "\",\r\n\"subscriptionDescription\": \"" + subscriptionDescription + "\",\r\n\"subscriptionRecurringPayment\": \"" + subscriptionRecurringPayment + "\", \r\n\"subscriptionProgram\": \"" + subscriptionProgram + "\",\r\n\"subscriptionUpgradeProgram\": \"10\",\r\n\"subscriptionImage\": \"\",\r\n\"subscriptionPrice\":\"" + subscriptionPrice + "\", \r\n\"subscriptionStatus\": \"" + subscriptionStatus + "\" }", ParameterType.RequestBody);
                                    IRestResponse responseSubscription = clientSubscription.Execute(requestSubscription);
                                    var rawResponseSubscription = JsonConvert.DeserializeObject<APIModel.NewMemberObject>(responseSubscription.Content);

                                    string walnutubscriptionIdentifier = rawResponseSubscription.subscriptionIdentifier.ToString();


                                    Console.WriteLine("Card number : " + cardNumber + " added in to subscription list. UserIdentifier is :" + WalnutuserIdentifier + " and  \n Subscription Id is : " + walnutubscriptionIdentifier);

                                    var ExistQuery = context.tbl_CardList.Where(x => x.Card_Number == cardNumber).FirstOrDefault();
                                    if (ExistQuery == null)
                                    {
                                        tbl_CardList objcardList = new tbl_CardList();
                                        objcardList.Card_Number = cardNumber;
                                        objcardList.User_Status = 1;
                                        objcardList.Client_ID = clientName;
                                        objcardList.WalnutSubscriptionID = walnutubscriptionIdentifier;
                                        objcardList.WalnutUserIdentifier = WalnutuserIdentifier;
                                        context.tbl_CardList.Add(objcardList);
                                        context.SaveChanges();
                                    }
                                    else
                                    {
                                        ExistQuery.User_Status = 1;
                                        context.SaveChanges();
                                    }
                                    //objCDA.ExecuteNonQuery("IF EXISTS(select * from tbl_CardList where Card_Number = '" + cardNumber + "') update tbl_CardList set User_Status = 1 ,WalnutSubscriptionID ='" + walnutubscriptionIdentifier + "' where Card_Number = '" + cardNumber + "' ELSE INSERT INTO tbl_CardList (Card_Number,User_Status,Client_ID,WalnutUserIdentifier,WalnutSubscriptionID) VALUES  ('" + cardNumber + "',1,'" + clientName + "','" + WalnutuserIdentifier + "','" + walnutubscriptionIdentifier + "')", "TSGG");



                                }
                           
                            }
                            var ChangeStatusOfDeactivatecard = (from a in context.tbl_CardList
                                                                where !context.tbl_IsCardExist.Any(s => (s.Card_Number == a.Card_Number))
                                                                select a).ToList<tbl_CardList>();
                            if (ChangeStatusOfDeactivatecard != null)
                            {
                                foreach (tbl_CardList delObj in ChangeStatusOfDeactivatecard)
                                {
                                    
                                    delObj.User_Status = 0;
                                    context.SaveChanges();
                                }
                               // ChangeStatusOfDeactivatecard.User_Status = 0;
                                //context.SaveChanges();

                            }


                      


                        }


                    }
             
                  


                }

                var Deletesubscription = context.tbl_CardList.Where(x => x.User_Status == 0).ToList<tbl_CardList>();
                if (Deletesubscription != null)
                {
                    foreach (tbl_CardList delObj in Deletesubscription)
                    {
                        string WalnutuserIdentifier = delObj.WalnutUserIdentifier;
                        string walnutubscriptionIdentifier = delObj.WalnutSubscriptionID;
                        string URL = ConfigurationManager.AppSettings["APIBaseURL"];
                        URL += storeIdentifier + "/user/" + WalnutuserIdentifier + "/subscription/" + walnutubscriptionIdentifier + "/";
                        var clientDelete = new RestClient(URL);
                        //var clientDelete = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/user/" + WalnutuserIdentifier + "/subscription/" + walnutubscriptionIdentifier + "/");
                        clientDelete.Timeout = -1;
                        var requestDelete = new RestRequest(Method.DELETE);
                        requestDelete.AddHeader("Authorization", "WalnutPass " + accountTokenParam);
                        requestDelete.AddHeader("Content-Type", "application/json");
                        requestDelete.AddParameter("application/json", "{ \"subscriptionName\": \"jaar Abonnement voor Invoice\",\r\n\"subscriptionDescription\": \"This is a test description\",\r\n\"subscriptionRecurringPayment\": \"Jaar\", \r\n\"subscriptionProgram\": \"Invoice\",\r\n\"subscriptionUpgradeProgram\": \"10\",\r\n\"subscriptionImage\": \"\", \r\n\"subscriptionStatus\": \"active\" }", ParameterType.RequestBody);
                        IRestResponse responseDelete = clientDelete.Execute(requestDelete);
                    }
                    // ChangeStatusOfDeactivatecard.User_Status = 0;
                    //context.SaveChanges();

                }
                List<tbl_IsCardExist> tempCardNumber = result.tbl_IsCardExist.ToList();
                foreach (tbl_IsCardExist obj in tempCardNumber)
                {
                    var deleteCardFromTemp = context.tbl_IsCardExist.FirstOrDefault(s => s.ID == obj.ID);
                    context.tbl_IsCardExist.Remove(deleteCardFromTemp);
                    context.SaveChanges();
                    //context.tbl_TempCardNumber.RemoveRange(obj);

                }

                //objCDA.ExecuteNonQuery("UPDATE tbl_CardList SET User_Status= 0  WHERE NOT EXISTS ( SELECT 1 FROM tbl_TempCardNumber WHERE  tbl_TempCardNumber.TempCardNumber = tbl_CardList.Card_Number)", "TSGG");
                //DataSet dsDeactivatedCard = new DataSet();
                //dsDeactivatedCard = objCDA.GetDataSet("SELECT * FROM tbl_CardList WHERE User_Status=0", "TSGG");
                //if (dsDeactivatedCard != null)
                //{
                //    for (int a = 0; a < dsDeactivatedCard.Tables[0].Rows.Count; a++)
                //    {
                //        string WalnutuserIdentifier = dsDeactivatedCard.Tables[0].Rows[a]["WalnutUserIdentifier"].ToString();
                //        string walnutubscriptionIdentifier = dsDeactivatedCard.Tables[0].Rows[a]["WalnutSubscriptionID"].ToString();
                //        var clientDelete = new RestClient("https://walnutbackend.com/api/v1/store/caf365662e9611ebb36422000a79120e/user/" + WalnutuserIdentifier + "/subscription/" + walnutubscriptionIdentifier + "/");
                //        clientDelete.Timeout = -1;
                //        var requestDelete = new RestRequest(Method.DELETE);
                //        requestDelete.AddHeader("Authorization", "WalnutPass " + accountTokenParam);
                //        requestDelete.AddHeader("Content-Type", "application/json");
                //        requestDelete.AddParameter("application/json", "{ \"subscriptionName\": \"jaar Abonnement voor Invoice\",\r\n\"subscriptionDescription\": \"This is a test description\",\r\n\"subscriptionRecurringPayment\": \"Jaar\", \r\n\"subscriptionProgram\": \"Invoice\",\r\n\"subscriptionUpgradeProgram\": \"10\",\r\n\"subscriptionImage\": \"\", \r\n\"subscriptionStatus\": \"active\" }", ParameterType.RequestBody);
                //        IRestResponse responseDelete = clientDelete.Execute(requestDelete);

                //    }

                //}
                //objCDA.ExecuteNonQuery("DElETE FROM tbl_TempCardNumber", "TSGG");
                //Console.WriteLine("All deleted card number is removed from the subscription list of walnut");
                //Console.ReadKey();

            }
     
        }
    }
}
