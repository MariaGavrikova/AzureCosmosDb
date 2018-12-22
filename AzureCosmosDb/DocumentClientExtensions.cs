 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
 using System.Collections.ObjectModel;
 using System.Net;
 using Microsoft.Azure.Documents;
 using Microsoft.Azure.Documents.Client;
 using Microsoft.Azure.Documents.Linq;

namespace AzureCosmosDb
{ 
    public static class DocumentClientExtensions
    {
        public static async Task<DocumentCollection> CreateCollectionAsync(
            this DocumentClient client,
            string name, 
            Database database,
            PartitionKeyDefinition partitionKey = null, 
            IndexingPolicy indexingPolicy = null)
        {
            var collection = new DocumentCollection 
            {
                Id = name
            };
            if (partitionKey != null)
            {
                collection.PartitionKey = partitionKey;
            }
            if (indexingPolicy != null)
            {
                collection.IndexingPolicy = indexingPolicy;
            }

            var requestOptions = new RequestOptions
            {
                OfferThroughput = 10000
            };
            collection = await client.CreateDocumentCollectionIfNotExistsAsync(database.SelfLink, collection, requestOptions);
            await Console.Out.WriteLineAsync($"{name} Self-Link:\t{collection.SelfLink}");

            return collection;
        }
    }
}