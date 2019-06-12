// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Management.RecoveryServices.Backup.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This is the base class for operation result responses.
    /// </summary>
    public partial class OperationWorkerResponse
    {
        /// <summary>
        /// Initializes a new instance of the OperationWorkerResponse class.
        /// </summary>
        public OperationWorkerResponse()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the OperationWorkerResponse class.
        /// </summary>
        /// <param name="statusCode">HTTP Status Code of the operation.
        /// Possible values include: 'Continue', 'SwitchingProtocols', 'OK',
        /// 'Created', 'Accepted', 'NonAuthoritativeInformation', 'NoContent',
        /// 'ResetContent', 'PartialContent', 'MultipleChoices', 'Ambiguous',
        /// 'MovedPermanently', 'Moved', 'Found', 'Redirect', 'SeeOther',
        /// 'RedirectMethod', 'NotModified', 'UseProxy', 'Unused',
        /// 'TemporaryRedirect', 'RedirectKeepVerb', 'BadRequest',
        /// 'Unauthorized', 'PaymentRequired', 'Forbidden', 'NotFound',
        /// 'MethodNotAllowed', 'NotAcceptable', 'ProxyAuthenticationRequired',
        /// 'RequestTimeout', 'Conflict', 'Gone', 'LengthRequired',
        /// 'PreconditionFailed', 'RequestEntityTooLarge', 'RequestUriTooLong',
        /// 'UnsupportedMediaType', 'RequestedRangeNotSatisfiable',
        /// 'ExpectationFailed', 'UpgradeRequired', 'InternalServerError',
        /// 'NotImplemented', 'BadGateway', 'ServiceUnavailable',
        /// 'GatewayTimeout', 'HttpVersionNotSupported'</param>
        /// <param name="headers">HTTP headers associated with this
        /// operation.</param>
        public OperationWorkerResponse(HttpStatusCode? statusCode = default(HttpStatusCode?), IDictionary<string, IList<string>> headers = default(IDictionary<string, IList<string>>))
        {
            StatusCode = statusCode;
            Headers = headers;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets HTTP Status Code of the operation. Possible values
        /// include: 'Continue', 'SwitchingProtocols', 'OK', 'Created',
        /// 'Accepted', 'NonAuthoritativeInformation', 'NoContent',
        /// 'ResetContent', 'PartialContent', 'MultipleChoices', 'Ambiguous',
        /// 'MovedPermanently', 'Moved', 'Found', 'Redirect', 'SeeOther',
        /// 'RedirectMethod', 'NotModified', 'UseProxy', 'Unused',
        /// 'TemporaryRedirect', 'RedirectKeepVerb', 'BadRequest',
        /// 'Unauthorized', 'PaymentRequired', 'Forbidden', 'NotFound',
        /// 'MethodNotAllowed', 'NotAcceptable', 'ProxyAuthenticationRequired',
        /// 'RequestTimeout', 'Conflict', 'Gone', 'LengthRequired',
        /// 'PreconditionFailed', 'RequestEntityTooLarge', 'RequestUriTooLong',
        /// 'UnsupportedMediaType', 'RequestedRangeNotSatisfiable',
        /// 'ExpectationFailed', 'UpgradeRequired', 'InternalServerError',
        /// 'NotImplemented', 'BadGateway', 'ServiceUnavailable',
        /// 'GatewayTimeout', 'HttpVersionNotSupported'
        /// </summary>
        [JsonProperty(PropertyName = "statusCode")]
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets HTTP headers associated with this operation.
        /// </summary>
        [JsonProperty(PropertyName = "headers")]
        public IDictionary<string, IList<string>> Headers { get; set; }

    }
}