using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using WCVIC.Web.Utilities;

namespace WCVIC.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NetSuiteController : ControllerBase
    {
        readonly IConfiguration configuration;
        readonly string NetSuite = "NetSuite";
        readonly string SERVICES_URL = "SERVICES_URL";
        readonly string CONSUMER_ID = "CONSUMER_ID";
        readonly string CONSUMER_SECRET = "CONSUMER_SECRET";
        readonly string TOKEN_ID = "TOKEN_ID";
        readonly string TOKEN_SECRET = "TOKEN_SECRET";
        readonly string ACCOUNT_ID = "ACCOUNT_ID";

        public NetSuiteController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private string SetHeader(string QueryString)
        {
            String header = "OAuth ";
            var NetSuiteReq = configuration.GetSection(NetSuite);
            var Services_Url = NetSuiteReq[SERVICES_URL];
            var consumer_id = NetSuiteReq[CONSUMER_ID];
            var consumer_secret = NetSuiteReq[CONSUMER_SECRET];
            var token_id = NetSuiteReq[TOKEN_ID];
            var token_secret = NetSuiteReq[TOKEN_SECRET];
            var NS_realm = NetSuiteReq[ACCOUNT_ID];

            string normalized_url;
            string normalized_params;

            OAuthBase oAuth = new();
            string nonce = oAuth.GenerateNonce();
            string time = oAuth.GenerateTimeStamp();
            Uri uri = new((Services_Url + QueryString));

            string signature = oAuth.GenerateSignature(uri, consumer_id, consumer_secret, token_id, token_secret, "GET", time, nonce, out normalized_url, out normalized_params);

            if (signature.Contains("+"))
            {
                signature = signature.Replace("+", "%2B");
            }

            // Construct the OAuth header		
            header += "oauth_consumer_key=\"" + consumer_id + "\",";
            header += "oauth_nonce=\"" + nonce + "\",";
            header += "oauth_signature_method=\"HMAC-SHA256\",";
            header += "oauth_signature=\"" + signature + "\",";
            header += "oauth_token=\"" + token_id + "\",";
            header += "oauth_timestamp=\"" + time + "\",";
            header += "oauth_version=\"1.0\",";
            header += "realm=\"" + NS_realm + "\"";

            return header;
        }

        [Route("FindByCompanyName")]
        [HttpGet]
        public async Task<List<NetSuiteVM>> FindByCompanyName(string CompanyName)
        {
            try
            {
                var NetSuiteReq = configuration.GetSection(NetSuite);
                var Services_Url = NetSuiteReq[SERVICES_URL];
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Services_Url + "record/v1/customer?q=companyName START_WITH " + CompanyName);
                request.Method = "GET";
                var Header = SetHeader("record/v1/customer?q=companyName START_WITH " + CompanyName);
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", Header);
                WebResponse response = request.GetResponse();
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                Stream resStream = httpResponse.GetResponseStream();
                StreamReader sr = new StreamReader(resStream);
                var result = await sr.ReadToEndAsync();
                var Items = JObject.Parse(result);

                var ItemsArray = Items["items"];
                List<string> IdArray = new();
                foreach (var item in ItemsArray)
                {
                    IdArray.Add(item["id"].ToString());
                }

                List<NetSuiteVM> NetSuiteCompnies = new();
                if (IdArray.Count != 0)
                {
                    NetSuiteCompnies = FindCompanyNameById(IdArray);
                }
                return NetSuiteCompnies;
            }
            catch
            {
                return null;
            }
        }

        public List<NetSuiteVM> FindCompanyNameById(List<string> ListOfId)
        {
            try
            {
                var NetSuiteReq = configuration.GetSection(NetSuite);
                var Services_Url = NetSuiteReq[SERVICES_URL];

                List<NetSuiteVM> ListOfCompanies = new();

                foreach (var item in ListOfId)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Services_Url + "record/v1/customer/" + Convert.ToInt32(item));
                    request.Method = "GET";
                    var Header = SetHeader("record/v1/customer/" + item);
                    request.ContentType = "application/json";
                    request.Headers.Add("Authorization", Header);

                    WebResponse response = request.GetResponse();
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Stream resStream = httpResponse.GetResponseStream();
                    StreamReader sr = new StreamReader(resStream);
                    var result = sr.ReadToEnd();
                    var Companies = JObject.Parse(result);
                    NetSuiteVM Item = SetCompanyAddress(Convert.ToInt32(item));

                    Item.CompanyName = Companies["entityId"].ToString();
                    Item.NetSuiteID = Convert.ToInt32(item);
                    ListOfCompanies.Add(Item);
                }
                return ListOfCompanies;
            }
            catch (Exception e)
            {
                var ex = e.Message;
                return null;
            }
        }

        private NetSuiteVM SetCompanyAddress(int CompanyId)
        {
            var NetSuiteReq = configuration.GetSection(NetSuite);
            var Services_Url = NetSuiteReq[SERVICES_URL];

            //for Address Book
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Services_Url + "record/v1/customer/" + CompanyId + "/addressBook");
            request.Method = "GET";
            var Header = SetHeader("record/v1/customer/" + CompanyId + "/addressBook");
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", Header);

            WebResponse response = request.GetResponse();
            HttpWebResponse httpResponse = (HttpWebResponse)response;
            Stream resStream = httpResponse.GetResponseStream();
            StreamReader sr = new StreamReader(resStream);
            var result = sr.ReadToEnd();
            var CompanyAddressLink = JObject.Parse(result);
            var CompanyItem = CompanyAddressLink["items"][0]["links"][0]["href"].ToString();
            var AddressId = CompanyItem.Substring(CompanyItem.LastIndexOf('/') + 1, CompanyItem.Length - CompanyItem.LastIndexOf('/') - 1);



            //For whole Address
            if (!string.IsNullOrWhiteSpace(AddressId))
            {
                HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create(Services_Url + "record/v1/customer/" + CompanyId + "/addressBook/" + AddressId + "/addressBookAddress");
                request2.Method = "GET";
                var Header2 = SetHeader("record/v1/customer/" + CompanyId + "/addressBook/" + AddressId + "/addressBookAddress");
                request2.ContentType = "application/json";
                request2.Headers.Add("Authorization", Header2);

                WebResponse response2 = request2.GetResponse();
                HttpWebResponse httpResponse2 = (HttpWebResponse)response2;
                Stream resStream2 = httpResponse2.GetResponseStream();
                StreamReader sr2 = new StreamReader(resStream2);
                var result2 = sr2.ReadToEnd();
                var CompanyCompleteAddress = JObject.Parse(result2);
                NetSuiteVM NewObject = new();

                NewObject.Address = CompanyCompleteAddress["addr1"]?.ToString();
                NewObject.Suburb = CompanyCompleteAddress["city"]?.ToString();
                NewObject.State = CompanyCompleteAddress["state"]?.ToString();
                //NewObject.Country = CompanyCompleteAddress["country"]["refName"]?.ToString();
                NewObject.Postcode = CompanyCompleteAddress["zip"]?.ToString();

                return NewObject;

            }


            return new NetSuiteVM();
        }
    }
}
