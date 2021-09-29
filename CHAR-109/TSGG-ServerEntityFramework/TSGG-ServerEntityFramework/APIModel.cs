using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSGG_ServerEntityFramework
{
    class APIModel
    {
        public class NewMemberObject
        {
            public string passIdentifier { get; set; }
            public string passBarcodeMessage { get; set; }
            public string userIdentifier { get; set; }
            public string userRegistered { get; set; }
            public string subscriptionIdentifier { get; set; }
            public string subscriptionStatus { get; set; }

        }

        public class Cardnumber
        {
            public double cardNumber { get; set; }

        }
        public class LoginResponse
        {
            public string accountToken { get; set; }
            public string storeIdentifier { get; set; }

        }
        public class accountTokenObject
        {
            public string accountToken { get; set; }
            public string accountEmailAddress { get; set; }
            public string storeIdentifier { get; set; }
        }


        public class Rootobject
        {
            public Storescans storeScans { get; set; }
        }

        public class Storescans
        {
            public int resultPage { get; set; }
            public int totalResultPages { get; set; }
            public int totalResults { get; set; }
            public Result[] results { get; set; }
        }

        public class Result
        {
            public string accountIdentifier { get; set; }
            public string accountEmailAddress { get; set; }
            public string passIdentifier { get; set; }
            public string passType { get; set; }
            public string passScans { get; set; }
            public string passScanned { get; set; }
            public string passBarcodeMessage { get; set; }
            public string userIdentifier { get; set; }
            public string userName { get; set; }
            public string userEmail { get; set; }
        }
    }
}
