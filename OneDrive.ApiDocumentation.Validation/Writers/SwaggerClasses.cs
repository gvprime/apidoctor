﻿using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OneDrive.ApiDocumentation.Validation
{
    internal class SwaggerResource
    {

    }

    internal class SwaggerMethod 
    {
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("parameters", NullValueHandling=NullValueHandling.Ignore)]
        public List<SwaggerParameter> Parameters { get; set; }

        [JsonProperty("tags", NullValueHandling=NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("security", NullValueHandling=NullValueHandling.Ignore)]
        public List<Dictionary<string, string[]>> Security { get ; set; }

        /// <summary>
        /// Key is either the response status "200" or "default"
        /// </summary>
        [JsonProperty("responses", NullValueHandling=NullValueHandling.Ignore)]
        public Dictionary<string, SwaggerResponse> Responses { get; set; }

        public SwaggerMethod()
        {
            Parameters = new List<SwaggerParameter>();
            Tags = new List<string>();
            Responses = new Dictionary<string, SwaggerResponse>();

            Security = new List<Dictionary<string, string[]>>();
            Security.Add(new Dictionary<string, string[]> { { "microsoftAccount", new string[] { "onedrive.readonly", "onedrive.readwrite" } } });
        }

    }

    internal class SwaggerParameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("in")]
        public string In { get; set; }
        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }
        [JsonProperty("required")]
        public bool Required { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("format", NullValueHandling=NullValueHandling.Ignore)]
        public string Format { get; set; }
    }

    internal class SwaggerResponse
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
        public object Schema { get; set; }
    }
}
