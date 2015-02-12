﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;

namespace OneDrive.ApiDocumentation.Validation.OData
{
    /// <summary>
    /// Converts OData input into json examples which can be validated against our 
    /// ResourceDefinitions in a DocSet.
    /// </summary>
    public class ODataParser
    {

        private static IDictionary<string, object> ODataSimpleTypeExamples = new Dictionary<string, object>() {
            { "Edm.String", "string" },
            { "Edm.Boolean", false },
            { "Edm.Int64", 1234567890 },
            { "Edm.Int32", 1234 },
            { "Edm.Double", 12.345678 },
            { "Edm.DateTimeOffset", "2014-01-01T00:00:00Z" },
        };

        public static List<Schema> ReadSchemaFromMetadata(string metadataContent)
        {
            XDocument doc = XDocument.Parse(metadataContent);

            List<Schema> schemas = new List<Schema>();
            schemas.AddRange(from e in doc.Descendants()
                             where e.Name.LocalName == "Schema"
                             select Schema.FromXml(e));

            return schemas;
        }

        public static async Task<List<Schema>> ReadSchemaFromMetadataUrl(Uri remoteUrl)
        {
            var request = HttpWebRequest.CreateHttp(remoteUrl);
            var response = await request.GetResponseAsync();
            var httpResponse = response as HttpWebResponse;
            if (httpResponse == null)
                return null;

            string remoteMetadataContents;
            using (var stream = httpResponse.GetResponseStream())
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(stream);
                remoteMetadataContents = await reader.ReadToEndAsync();
            }

            return ReadSchemaFromMetadata(remoteMetadataContents);
        }

        public static async Task<List<Schema>> ReadSchemaFromFile(string path)
        {
            System.IO.StreamReader reader = new System.IO.StreamReader(path);
            string localMetadataContents = await reader.ReadToEndAsync();

            return ReadSchemaFromMetadata(localMetadataContents);
        }

        /// <summary>
        /// Convert resources found in the CSDL schema objects into ResourceDefintion instances
        /// that can be tested against the documentation.
        /// </summary>
        /// <param name="schemas"></param>
        /// <returns></returns>
        public static List<ResourceDefinition> GenerateResourcesFromSchemas(IEnumerable<Schema> schemas)
        {
            List<ResourceDefinition> resources = new List<ResourceDefinition>();
            
            foreach (var schema in schemas)
            {
                resources.AddRange(CreateResourcesFromSchema(schema, schemas));
            }

            return resources;
        }


        private static IEnumerable<ResourceDefinition> CreateResourcesFromSchema(Schema schema, IEnumerable<Schema> otherSchema)
        {
            List<ResourceDefinition> resources = new List<ResourceDefinition>();

            resources.AddRange(from ct in schema.ComplexTypes select ResourceDefinitionFromType(schema, otherSchema, ct));
            resources.AddRange(from et in schema.Entities select ResourceDefinitionFromType(schema, otherSchema, et));

            return resources;
        }

        private static ResourceDefinition ResourceDefinitionFromType(Schema schema, IEnumerable<Schema> otherSchema, ComplexType ct)
        {
            var annotation = new CodeBlockAnnotation() { ResourceType = string.Concat(schema.Namespace, ".", ct.Name), BlockType = CodeBlockType.Resource };
            var json = BuildJsonExample(ct, otherSchema);
            ResourceDefinition rd = new ResourceDefinition(annotation, json, null);
            return rd;
        }

        private static string BuildJsonExample(ComplexType ct, IEnumerable<Schema> otherSchema)
        {
            Dictionary<string, object> dict = BuildDictionaryExample(ct, otherSchema);
            return Newtonsoft.Json.JsonConvert.SerializeObject(dict);
        }

        private static Dictionary<string, object> BuildDictionaryExample(ComplexType ct, IEnumerable<Schema> otherSchema)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (var prop in ct.Properties)
            {
                if (prop.Type != "Edm.Stream")
                {
                    dict.Add(prop.Name, ExampleOfType(prop.Type, otherSchema));
                }
            }
            return dict;
        }

        private static string CollectionPrefix = "Collection(";
        private static object ExampleOfType(string typeIdentifier, IEnumerable<Schema> otherSchemas)
        {
            if (typeIdentifier.StartsWith(CollectionPrefix) && typeIdentifier.EndsWith(")"))
            {
                var arrayTypeIdentifier = typeIdentifier.Substring(CollectionPrefix.Length);
                arrayTypeIdentifier = arrayTypeIdentifier.Substring(0, arrayTypeIdentifier.Length - 1);

                var obj = ObjectExampleForType(arrayTypeIdentifier, otherSchemas);
                return new object[] { obj, obj };
            }

            return ObjectExampleForType(typeIdentifier, otherSchemas);
        }

        private static object ObjectExampleForType(string typeIdentifier, IEnumerable<Schema> otherSchemas)
        {
            if (ODataSimpleTypeExamples.ContainsKey(typeIdentifier))
                return ODataSimpleTypeExamples[typeIdentifier];

            // If that fails, the we need to locate the typeIdentifier in the schemas and
            // generate an example from that.
            ComplexType matchingType = otherSchemas.FindTypeWithIdentifier(typeIdentifier);
            if (null == matchingType)
            {
                System.Diagnostics.Debug.WriteLine("Failed to find an example for type: " + typeIdentifier);
                return new { datatype = typeIdentifier };
            }
            else
                return BuildDictionaryExample(matchingType, otherSchemas);
        }
    }
}