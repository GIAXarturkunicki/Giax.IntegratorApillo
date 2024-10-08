using Giax.IntegratorApillo.settings;
using Mono.CSharp;
using Soneta.Business;
using Soneta.Business.Db;
using Soneta.Business.UI;
using Soneta.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using static Humanizer.On;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Giax.IntegratorApillo.models;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http.Headers;
using Soneta.Handel;

[assembly: Worker(typeof(SettingsConfig))]

namespace Giax.IntegratorApillo.settings
{
    public class SettingsConfig : ISessionable
    {

        [Context]
        public Context Context { get; set; }
        public Session Session { get; set; }

        public SettingsConfig(ISessionable sessionable)
        {
            Session = sessionable.Session;
            isVisible = IsFeaturesCreated();
            if (isVisible)
            {
                List<(string, string)> val_list = GetFeaturesFromList(BusinessFeaturesList());
                foreach (var (name, value) in val_list)
                {
                    if (name == "GIA_access_token")
                    {
                        AccessToken = value;
                    }
                    else if (name == "GIA_refresh_token")
                    {
                        RefreshToken = value;
                    }
                    else if (name == "GIA_client_token")
                    {
                        ClientToken = value;
                    }
                    else if (name == "GIA_api_endpoint")
                    {
                        ApiEndpoint = value;
                    }
                    else if (name == "GIA_timestamp")
                    {
                        Timestamp = DateTime.Now;
                    }
                    else if (name == "GIA_client_id")
                    {
                        ClientId = value;
                    }
                    else if (name == "GIA_authorization_code")
                    {
                        AuthorizationCode = value;
                    }
                }

                CheckAuthAPI();
            }
        }

        private (string, FeatureReadOnlyMode, string)[] BusinessFeaturesList()
        {
            (string, FeatureReadOnlyMode, string)[] features =
            {
                ("GIA_access_token", FeatureReadOnlyMode.Standard, ""),
                ("GIA_refresh_token", FeatureReadOnlyMode.Standard, ""),
                ("GIA_client_token", FeatureReadOnlyMode.Standard, ""),
                ("GIA_api_endpoint", FeatureReadOnlyMode.Standard, ""),
                ("GIA_timestamp", FeatureReadOnlyMode.Standard, ""),
                ("GIA_client_id", FeatureReadOnlyMode.Standard, ""), 
                ("GIA_authorization_code", FeatureReadOnlyMode.Standard, "") 
            };

            return features;
        }

        private List<(string, string)> GetFeaturesFromList((string, FeatureReadOnlyMode, string)[] list)
        {
            List<(string, string)> valueList = new List<(string, string)>();

            using (Session configSession = Session.Login.CreateSession(false, true))
            {
                using (var s = configSession.Logout(true))
                {
                    foreach (var (name, mode, extra) in list)
                    {
                        (string, string) value = (name, (string)configSession.Global.Features[name]);
                        valueList.Add(value);
                    }
                }
            }
            DokHandlowe test;

            return valueList;
        }

        private bool IsFeaturesCreated()
        {
            var module = BusinessModule.GetInstance(Session);

            foreach (var x in BusinessFeaturesList())
            {
                if (module.FeatureDefs.Rows.FirstOrDefault(fd => ((FeatureDefinition)fd).Name == x.Item1) == null)
                {
                    return false;
                }

                
            }
            return true;
        }

        public void CreateFeatures()
        {
            using (Session configsession = Session.Login.CreateSession(false, true))
            {
                using (var ses = configsession.Logout(true))
                {
                    var bmodule = BusinessModule.GetInstance(configsession);
                    foreach (var feature in BusinessFeaturesList())
                    {
                        if (bmodule.FeatureDefs.Rows.FirstOrDefault(f => ((FeatureDefinition)f).Name == feature.Item1) != null)
                        {
                            new Log("GIA", true).WriteLine($"Nie udało się utworzyć cechy o nazwie: {feature.Item1} - taka cecha już istnieje");
                            return;
                        }

                        var fd = new FeatureDefinition("CfgNodes");
                        fd.TypeNumber = FeatureTypeNumber.String;
                        fd.Name = feature.Item1;
                        fd.ReadOnlyMode = feature.Item2;
                        fd.InitValue = feature.Item3;

                        bmodule.FeatureDefs.AddRow(fd);
                    }

                 //   ses.CommitUI();
                    ses.Commit();
                }
                configsession.Save();
               // configsession.InvokeSaved();
            }
        }

        public void CheckAuth()
        {
            
        }

        private void CheckAuthAPI()
        {
            var endpoint = (string)Session.Global.Features["GIA_api_endpoint"] + "api/";
            var token = (string)Session.Global.Features["GIA_access_token"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(token))
            {
                IsAuthenticated = false;
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


                HttpResponseMessage response = client.GetAsync(endpoint).GetAwaiter().GetResult();
                IsAuthenticated = response.IsSuccessStatusCode;
                return;
            }
        }

        public void GetAccesToken()
        {
            var endpoint = (string)Session.Global.Features["GIA_api_endpoint"] + "/rest/auth/token/";
            //ten idzie w auth
            var client_secret = (string)Session.Global.Features["GIA_client_token"];
            var client_id = (string)Session.Global.Features["GIA_client_id"];
            var client_code = (string)Session.Global.Features["GIA_authorization_code"];


            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var authheader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authheader);

                var requestBody = new
                {
                    grantType = "authorization_code",
                    token = client_code,
                    //developerId = (string)null
                };

                var requestBodyJSON = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,"application/json"
                    );

                
                HttpResponseMessage response = client.PostAsync(endpoint,requestBodyJSON).GetAwaiter().GetResult();
                if(response.IsSuccessStatusCode)
                {
                    //sprawdzic czy nie wywalilo sie bledem bo inaczej zapisze zle dane
                    string responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    var tokens = JsonConvert.DeserializeObject<AccessTokenModel>(responseContent);
                   
                    SaveFeature("GIA_access_token", tokens.AccessToken);
                    SaveFeature("GIA_refresh_token", tokens.RefreshToken);
                }
                else
                {
                  //blad autoryzacji
                }
               
            }
        }

        private void GetRefreshedToken()
        {
            var endpoint = (string)Session.Global.Features["GIA_api_endpoint"] + "/rest/auth/token/";
            var client_secret = (string)Session.Global.Features["GIA_client_token"];
            var client_id = (string)Session.Global.Features["GIA_client_id"];
            var client_refresh_code = (string)Session.Global.Features["GIA_refresh_token"];


            using (HttpClient client = new HttpClient())
            {
                var authheader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authheader);

                var requestBody = new
                {
                    grantType = "refresh_token",
                    token = client_refresh_code,
                    //developerId = (string)null
                };

                var requestBodyJSON = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8, "application/json"
                    );


                HttpResponseMessage response = client.PostAsync(endpoint, requestBodyJSON).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    //sprawdzic czy nie wywalilo sie bledem bo inaczej zapisze zle dane
                    string responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    var tokens = JsonConvert.DeserializeObject<AccessTokenModel>(responseContent);

                    SaveFeature("GIA_access_token", tokens.AccessToken);
                    SaveFeature("GIA_refresh_token", tokens.RefreshToken);
                }
                else
                {
                    //blad autoryzacji
                }

            }
        }

        private void SaveFeature(string name, string value)
        {
            using (Session configSession = Session.Login.CreateSession(false, true))
            {
                using (var s = configSession.Logout(true))
                {
                    configSession.Global.Features[name] = value;
                    s.Commit();
                }
                //configSession.InvokeSaved();
                configSession.Save();
            }
        }

        private string _access_token { get; set; }
        private string _refresh_token { get; set; }
        private string _client_token { get; set; }
        private string _api_endpoint { get; set; }
        private DateTime _timestamp { get; set; }
        private string _client_id { get; set; } // Nowe pole
        private string _authorization_code { get; set; } // Nowe pole

        private bool _is_authenticated { get; set; }

        private bool _isVisible { get; set; }

        public string AccessToken
        {
            get { return _access_token; }
            set
            {
                _access_token = value;
                SaveFeature("GIA_access_token", value);
            }
        }

        public string RefreshToken
        {
            get { return _refresh_token; }
            set
            {
                _refresh_token = value;
                SaveFeature("GIA_refresh_token", value);
            }
        }

        public string ClientToken
        {
            get { return _client_token; }
            set
            {
                _client_token = value;
                SaveFeature("GIA_client_token", value);
            }
        }

        public string ApiEndpoint
        {
            get { return _api_endpoint; }
            set
            {
                _api_endpoint = value;
                SaveFeature("GIA_api_endpoint", value);
            }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set
            {
                _timestamp = value;
                SaveFeature("GIA_timestamp", value.ToString("o"));
            }
        }

        public string ClientId
        {
            get { return _client_id; }
            set
            {
                _client_id = value;
                SaveFeature("GIA_client_id", value); 
            }
        }

        public string AuthorizationCode
        {
            get { return _authorization_code; }
            set
            {
                _authorization_code = value;
                SaveFeature("GIA_authorization_code", value); 
            }
        }

        public bool isVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; }
        }

        public bool IsAuthenticated
        {
            get { return _is_authenticated; }
            set { _is_authenticated = value; }
        }
    }
}
