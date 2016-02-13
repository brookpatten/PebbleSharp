using System;
using System.Collections.Generic;
using PebbleSharp.Core.NonPortable.AppMessage;

namespace PebbleSharp.Core.Responses
{
    [Endpoint( Endpoint.ApplicationMessage )]
    public class ApplicationMessageResponse : ResponseBase
    {
        public AppMessageDictionary Dictionary{get;set;}

        protected override void Load( byte[] payload )
        {
            Dictionary = new AppMessageDictionary(payload);
        }

    }
}