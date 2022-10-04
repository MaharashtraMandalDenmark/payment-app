using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Text.Json.Nodes;
using Google.Apis.Services;
using Google.Apis.PeopleService.v1;
namespace PayMMD.Pages
{
    public partial class Payment
    {
        [Inject]
        private HttpClient _httpClient {get; set;}= default!;
        private void ClearAllFields()
        {
            var service = new PeopleServiceService (new BaseClientService.Initializer
                {
                    ApplicationName = "ContactsMMDApp",
                    ApiKey="AIzaSyDOjItb8kx895gGTKZ0bC8v-3v9NxdujE8",
                });
            var peopleRequest = service.People.Connections.List("people/me");
            peopleRequest.PersonFields ="names,emailAddresses";
            var result = peopleRequest.Execute();
            Console.Write(result.Connections);
        }
        private async Task Redirect2Pay()
        {
            var paylink = new PayLink();
            try
            {
                PaymentLinkRequest requestBody = new PaymentLinkRequest()
                {
                    Amount = 100,
                    Currency = "DKK",
                    OrderNumber = "888-1234",
                    BillingAddress = new PaymentLinkRequest.Address()
                    {
                        AddressLine1 = "",
                        City = "",
                        Country = 208,
                        PostCode = ""
                    },
                    ShippingAddress = new PaymentLinkRequest.Address()
                    {
                        AddressLine1 = "",
                        City = "",
                        Country = 208,
                        PostCode = ""
                    },
                    CustomerAcceptUrl = "https://localhost/?accept=true",
                    CustomerDeclineUrl = "https://localhost/?decline=true",
                    ServerCallbackUrl = "https://parag.dhande.info/",
                    EnforceLanguage = "en-US",
                    OtherOptions = new PaymentLinkRequest.Options()
                    {
                        TestMode = true
                    },
                    //Set savecard to true and uncomment recurring to test recurringPayment
                    SaveCard = false,
                    //RecurringPayment = new PaymentLinkRequest.Recurring()
                    //{
                    //    Expiry = "20210127",
                    //    PaymentFrequencyDays = 1
                    //}
                    
                };

                var body = JsonConvert.SerializeObject(requestBody);                        
                var request = new HttpRequestMessage(HttpMethod.Post, "https://payment-app-api.netlify.app/.netlify/functions/payment-link");                
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");                                        
                //request.Headers.Add("authorization","2974d5fa-8e25-4621-8d22-51d496a4f97b");                         
                WebAssemblyHttpRequestMessageExtensions.SetBrowserRequestMode(request, BrowserRequestMode.NoCors);                   
                using var response = await _httpClient.SendAsync(request);            
                if (!response.IsSuccessStatusCode)
                {
                    // set error message for display, log to console and return                  
                    Console.WriteLine($"Error! Response Code: {response.StatusCode} Response Phrase: {response.ReasonPhrase}");
                    return;
                }

                // convert response data to object
                var json = await response.Content.ReadAsStringAsync();
                var result = string.IsNullOrEmpty(json) ? null : JsonObject.Parse(json);
                //paylink = await response.Content.ReadFromJsonAsync<PayLink>();  

                Console.WriteLine(result);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public class PayLink
        {
            [JsonProperty(PropertyName = "paymentIdentifier")]
            public string PaymentIdentifier { get; set; }    
            [JsonProperty(PropertyName = "paymentWindowLink")]
            public string PaymentWindowLink { get; set; }   
        }
        public class PaymentLinkRequest
        {            
            private static readonly Regex _cultureRegex = new Regex(@"^\w{2}-\w{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);                        
            public class Address
            {
                /// <summary>
                /// Address line, maximum of 50 chars length
                /// </summary>
                [JsonRequired]
                [JsonProperty(PropertyName = "AddressLine1")]
                public string AddressLine1 { get; set; }

                /// <summary>
                /// Address line, maximum of 50 chars length
                /// </summary>
                [JsonProperty(PropertyName = "AddressLine2")]
                public string AddressLine2 { get; set; }

                /// <summary>
                /// Address line, maximum of 50 chars length
                /// </summary>
                [JsonProperty(PropertyName = "AddressLine3")]
                public string AddressLine3 { get; set; }

                /// <summary>
                /// City, maximum of 50 chars length
                /// </summary>
                [JsonRequired]
                [JsonProperty(PropertyName = "City")]
                public string City { get; set; }

                /// <summary>
                /// Zipcode, maximum of 16 chars
                /// </summary>
                [JsonRequired]
                [JsonProperty(PropertyName = "PostCode")]
                public string PostCode { get; set; }

                /// <summary>
                /// 3 char country code
                /// </summary>
                [JsonRequired]
                [JsonProperty(PropertyName = "Country")]
                public int Country { get; set; }

            }

            public class Recurring
            {
                /// <summary>
                /// Expiration date of the subscription. After that date it won't be possible to make further authorizations on that subscription. The format of the date provided is YYYYMMDD"
                /// </summary>
                [JsonRequired]
                [JsonProperty(PropertyName = "Expiry")]
                public string Expiry { get; set; }

                /// <summary>
                /// How many days is the period of the subscription. Each subsequent authorization on the subscription should happen after at least the provided amount of days. If you set the period of 30 days and make authorization after 31 days, that will be allowed but you shouldn't make authorizations before the period is elapsed."
                /// </summary>
                [JsonRequired]
                [JsonProperty(PropertyName = "PaymentFrequencyDays")]
                public int PaymentFrequencyDays { get; set; }

                private bool CanParseExpiry()
                {
                    if (string.IsNullOrEmpty(Expiry))
                    {
                        return false;
                    }

                    if (Expiry.Length != 8)
                    {
                        return false;
                    }

                    try
                    {
                        var year = int.Parse(Expiry.Remove(4));
                        var month = int.Parse(Expiry.Substring(4, 2));
                        var day = int.Parse(Expiry.Substring(6, 2));

                        var date = new DateTime(year, month, day);
                        return date > DateTime.Now;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            public class Options 
            {
                [JsonProperty(PropertyName = "TestMode")]
                public bool TestMode { get; set; }
                [JsonProperty(PropertyName = "RequestSubscriptionWithAuthorization")]
                public bool RequestSubscriptionWithAuthorization { get; set; }
            }
            /// <summary>
            /// Total amount of the order. This field is required. The amount is given in minor units, which means that a decimal amount of 100.20 has to be given as 10020
            /// </summary>
            [JsonRequired]
            [JsonProperty(PropertyName = "Amount")]
            public int Amount { get; set; }

            /// <summary>
            /// 3 char currency code of the order. This field is required.
            /// </summary>
            [JsonRequired]
            [JsonProperty(PropertyName = "Currency")]
            public string Currency { get; set; }

            /// <summary>
            /// Unique identifier of the order. This field is required.
            /// </summary>
            [JsonRequired]
            [JsonProperty(PropertyName = "OrderNumber")]
            public string OrderNumber { get; set; }

            /// <summary>
            /// Boolean flag defining if Freepay should create recurrent billing handler from that order. More information about recurrent billing can be found here. By default this field is `false` or `0`.
            /// </summary>
            [JsonProperty(PropertyName = "SaveCard")]
            public bool SaveCard { get; set; }

            /// <summary>
            /// The url to which Freepay will navigate your customer after his credit card has been processed successfully. This field is required.
            /// </summary>
            [JsonRequired]
            [JsonProperty(PropertyName = "CustomerAcceptUrl")]
            public string CustomerAcceptUrl { get; set; }

            /// <summary>
            /// The url to which Freepay will navigate your customer if there was a problem with processing of his/hers payment. This field is required.
            /// </summary>
            [JsonRequired]
            [JsonProperty(PropertyName = "CustomerDeclineUrl")]
            public string CustomerDeclineUrl { get; set; }

            /// <summary>
            /// This url is called by Freepay when the payment of your client is successfully processed. You can read more about the server callback in the following section here. This field is required.
            /// </summary>
            [JsonProperty(PropertyName = "ServerCallbackUrl")]
            public string ServerCallbackUrl { get; set; }

            /// <summary>
            /// Billing information of your client. This field is required.
            /// </summary>
            [JsonProperty(PropertyName = "BillingAddress")]
            public Address BillingAddress { get; set; }

            /// <summary>
            /// Shipping information of your client's order. This field is required.
            /// </summary>
            [JsonProperty(PropertyName = "ShippingAddress")]
            public Address ShippingAddress { get; set; }

            /// <summary>
            /// Subscription details. This field is required when you have SaveCard parameter set to True.
            /// </summary>
            [JsonProperty(PropertyName = "RecurringPayment")]
            public Recurring RecurringPayment { get; set; }

            [JsonProperty(PropertyName = "EnforceLanguage")]
            public string EnforceLanguage { get; set; }

            [JsonProperty(PropertyName = "Options")]
            public Options OtherOptions{ get; set; }
        
        }                 
    }
}