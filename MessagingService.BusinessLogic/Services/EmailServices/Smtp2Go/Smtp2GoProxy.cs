﻿namespace MessagingService.BusinessLogic.Services.EmailServices.Smtp2Go
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Service.Services.Email.Smtp2Go;
    using Shared.General;
    using Shared.Logger;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="MessagingService.BusinessLogic.Services.EmailServices.IEmailServiceProxy" />
    [ExcludeFromCodeCoverage]
    public class Smtp2GoProxy : IEmailServiceProxy
    {
        #region Fields

        /// <summary>
        /// The HTTP client
        /// </summary>
        private readonly HttpClient HttpClient;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Smtp2GoProxy"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        public Smtp2GoProxy(HttpClient httpClient)
        {
            this.HttpClient = httpClient;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends the email.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="fromAddress">From address.</param>
        /// <param name="toAddresses">To addresses.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        /// <param name="isHtml">if set to <c>true</c> [is HTML].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<EmailServiceProxyResponse> SendEmail(Guid messageId,
                                                               String fromAddress,
                                                               List<String> toAddresses,
                                                               String subject,
                                                               String body,
                                                               Boolean isHtml,
                                                               CancellationToken cancellationToken)
        {
            EmailServiceProxyResponse response = null;

            // Translate the request message
            Smtp2GoSendEmailRequest apiRequest = new Smtp2GoSendEmailRequest
                                                 {
                                                     ApiKey = ConfigurationReader.GetValue("SMTP2GoAPIKey"),
                                                     HTMLBody = isHtml ? body : string.Empty,
                                                     TextBody = isHtml ? string.Empty : body,
                                                     Sender = fromAddress,
                                                     Subject = subject,
                                                     TestMode = false,
                                                     To = toAddresses.ToArray()
                                                 };

            String requestSerialised = JsonConvert.SerializeObject(apiRequest);

            Logger.LogDebug($"Request Message Sent to Email Provider [SMTP2Go] {requestSerialised}");

            StringContent content = new StringContent(requestSerialised, Encoding.UTF8, "application/json");

            using(HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ConfigurationReader.GetValue("SMTP2GoBaseAddress"));

                HttpResponseMessage httpResponse = await client.PostAsync("email/send", content, cancellationToken);

                Smtp2GoSendEmailResponse apiResponse = JsonConvert.DeserializeObject<Smtp2GoSendEmailResponse>(await httpResponse.Content.ReadAsStringAsync());

                Logger.LogDebug($"Response Message Received from Email Provider [SMTP2Go] {JsonConvert.SerializeObject(apiResponse)}");

                // Translate the Response
                response = new EmailServiceProxyResponse
                           {
                               ApiStatusCode = httpResponse.StatusCode,
                               EmailIdentifier = apiResponse.Data.EmailId,
                               Error = apiResponse.Data.Error,
                               ErrorCode = apiResponse.Data.ErrorCode,
                               RequestIdentifier = apiResponse.RequestId
                           };
            }

            return response;
        }

        /// <summary>
        /// Gets the message status.
        /// </summary>
        /// <param name="providerReference">The provider reference.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<MessageStatusResponse> GetMessageStatus(String providerReference,
                                                                  DateTime startDate,
                                                                  DateTime endDate,
                                                                  CancellationToken cancellationToken)
        {
            MessageStatusResponse response = null;

            Smtp2GoEmailSearchRequest apiRequest = new Smtp2GoEmailSearchRequest
                                                   {
                                                       ApiKey = ConfigurationReader.GetValue("SMTP2GoAPIKey"),
                                                       EmailId = new List<String>{providerReference},
                                                       StartDate = startDate.ToString("yyyy-MM-dd"),
                                                       EndDate = endDate.ToString("yyyy-MM-dd"),
            };

            String requestSerialised = JsonConvert.SerializeObject(apiRequest);

            Logger.LogDebug($"Request Message Sent to Email Provider [SMTP2Go] {requestSerialised}");

            StringContent content = new StringContent(requestSerialised, Encoding.UTF8, "application/json");

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ConfigurationReader.GetValue("SMTP2GoBaseAddress"));

                HttpResponseMessage httpResponse = await client.PostAsync("email/search", content, cancellationToken);

                Smtp2GoEmailSearchResponse apiResponse = JsonConvert.DeserializeObject<Smtp2GoEmailSearchResponse>(await httpResponse.Content.ReadAsStringAsync());

                Logger.LogDebug($"Response Message Received from Email Provider [SMTP2Go] {JsonConvert.SerializeObject(apiResponse)}");

                // Translate the Response
                response = new MessageStatusResponse
                           {
                               ApiStatusCode = httpResponse.StatusCode,
                               MessageStatus = this.TranslateMessageStatus(apiResponse.Data.EmailDetails.Single().Status),
                               ProviderStatusDescription = apiResponse.Data.EmailDetails.Single().Status,
                               Timestamp = apiResponse.Data.EmailDetails.Single().EmailStatusDate
                           };
            }

            return response;
        }

        private MessageStatus TranslateMessageStatus(String status)
        {
            MessageStatus result;
            switch (status)
            {
                case "failed":
                case "deferred":
                    result = MessageStatus.Failed;
                    break;
                case "hardbounce":
                case "refused":
                case "softbounce":
                case "returned":
                    result = MessageStatus.Bounced;
                    break;
                case "delivered":
                case "ok":
                case "sent":
                    result = MessageStatus.Delivered;
                    break;
                case "rejected":
                    result = MessageStatus.Rejected;
                    break;
                case "complained":
                case "spam":
                    result = MessageStatus.Spam;
                    break;
                default:
                    result = MessageStatus.Unknown;
                    break;
            }

            return result;
        }

        #endregion
    }
}