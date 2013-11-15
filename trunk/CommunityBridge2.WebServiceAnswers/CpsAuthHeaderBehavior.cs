//------------------------------------------------------------------------------
// <copyright file="CpsAuthHeaderBehavior.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//    CpsAuthHeaderBehavior class.
// </summary>
//------------------------------------------------------------------------------

namespace Microsoft.Support.Community
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.Text;
    using System.Web;

    public class CpsAuthHeaderBehavior : BehaviorExtensionElement, IEndpointBehavior
    {
      public CpsAuthHeaderBehavior(string ticket)
      {
        _Ticket = ticket;
      }

      private string _Ticket;
      private string GetTicket()
      {
        lock (this)
        {
          return _Ticket;
        }
      }
      public void UpdateTicket(string ticket)
      {
        lock(this)
        {
          _Ticket = ticket;
        }
        lock(_inspectors)
        {
          foreach (var insp in _inspectors)
          {
            insp.UpdateTicket(_Ticket);            
          }
        }
      }

      public override Type BehaviorType
        {
            get
            {
                return typeof(CpsAuthHeaderBehavior);
            }
        }

        [ConfigurationProperty("bindingType")]
        public string BindingType
        {
            get { return (string)this["bindingType"]; }
            set { this["bindingType"] = value; }
        }

        protected override object CreateBehavior()
        {
          return new CpsAuthHeaderBehavior(_Ticket)
          {
            BindingType = this.BindingType
          };
        }

        #region IEndpointBehavior Members

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        List<CustomAuthHeaderInspector> _inspectors = new List<CustomAuthHeaderInspector>();
        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
          var ticket = GetTicket();
          var insp = new CustomAuthHeaderInspector(this.BindingType, ticket);
          lock (_inspectors)
          {
            _inspectors.Add(insp);
          }
          clientRuntime.MessageInspectors.Add(insp);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        #endregion

        protected class CustomAuthHeaderInspector : IClientMessageInspector
        {
          public CustomAuthHeaderInspector(string bindingType, string ticket)
          {
            this.BindingType = (bindingType ?? string.Empty).ToLowerInvariant();
            _Ticket = ticket;
          }
            public string BindingType { get; private set; }
            string token = "e5cf6050-25ad-4a00-b20e-d749ba178241";
            string secretkey = "Sk/vlBlYxT/X0zRB7E6TjQ==";
            string ns = "Microsoft.Support.Community";

            private string _Ticket;
            private string GetTicket()
            {
              lock (this)
              {
                return _Ticket;
              }
            }
            public void UpdateTicket(string ticket)
            {
              lock (this)
              {
                _Ticket = ticket;
              }
            }


            #region IClientMessageInspector Members
 
            public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
            {
            }

            public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel)
            {
                string dateTimeString = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);

                if (this.BindingType == "tcp")
                {
                    // For TCP binding, attach token to SOAP header
                    MessageHeader<string> header = new MessageHeader<string>(token);
                    request.Headers.Add(header.GetUntypedHeader("CpsToken",ns));

                    header = new MessageHeader<string>(dateTimeString);
                    request.Headers.Add(header.GetUntypedHeader("CpsDate", ns));

                    header = new MessageHeader<string>(ComputeHash(token + ";" + dateTimeString,secretkey));
                    request.Headers.Add(header.GetUntypedHeader("CpsAuth", ns));
                } 
                else
                {
                    // Fot HTTP binding, attach token to HTTP header
                    HttpRequestMessageProperty httpRequestMessage;
                    object httpRequestMessageObject;
                    if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
                    {
                        httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                    }
                    else
                    {
                        httpRequestMessage = new HttpRequestMessageProperty();
                        request.Properties[HttpRequestMessageProperty.Name] = httpRequestMessage;
                    }

                    httpRequestMessage.Headers["CpsToken"] = token;
                    httpRequestMessage.Headers["CpsDate"] = dateTimeString;
                    httpRequestMessage.Headers["CpsAuth"] = ComputeHash(token + ";" + dateTimeString, secretkey);


                    string ticket = HttpUtility.UrlDecode(GetTicket());
                    httpRequestMessage.Headers["LiveAuthTicket"] = ticket;
                }

                return null;
            }

            #endregion
        }

        protected static string ComputeHash(string inputData, string key)
        {
            byte[] keyArray = Convert.FromBase64String(key);
            byte[] dataToHash = Encoding.UTF8.GetBytes(inputData);

            using (HMACSHA256 hmacsha1 = new HMACSHA256(keyArray))
            {
                return System.Convert.ToBase64String(hmacsha1.ComputeHash(dataToHash));
            }
        }
    }
}