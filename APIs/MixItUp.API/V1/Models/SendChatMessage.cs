﻿using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace MixItUp.API.V1.Models
{
    [DataContract]
    public class SendChatMessage
    {
        [Required]
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public bool SendAsStreamer { get; set; }
    }
}