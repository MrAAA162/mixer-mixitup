﻿using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace MixItUp.Base.Model.API
{
    [DataContract]
    public class MixItUpUpdateModel
    {
        [JsonProperty]
        public string Version { get; set; }
        [JsonProperty]
        public string ChangelogLink { get; set; }
        [JsonProperty]
        public string ZipArchiveLink { get; set; }

        [JsonProperty]
        public string InstallerLink { get; set; }

        [JsonIgnore]
        public Version SystemVersion { get { return new Version(this.Version); } }

        [JsonIgnore]
        public bool IsPreview { get { return this.SystemVersion.Revision > 1000; } }
    }
}
