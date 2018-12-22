 using System;
 using System.Collections.Generic;
 using System.Collections.ObjectModel;
 using System.Diagnostics;
 using System.Linq;
 using System.Net;
 using System.Threading.Tasks;
 using Microsoft.Azure.Documents;
 using Microsoft.Azure.Documents.Client;
 using Microsoft.Azure.Documents.Linq;

namespace AzureCosmosDb
{
    public class Program
    { 
        private static readonly Uri _endpointUri = new Uri("https://cosmos-db-lab.documents.azure.com:443/");
        private const string _primaryKey = "DWH1jWd0fruP8Gl2rEywhV42AFBeJjWYKBRg4wuagc6XDRkZvmZFBg6rnL1y2MO9Qx6esdKC3InkcE0KmSVBgQ==";
        private const string DbName = "FinancialDatabase";
        private const string PeopleCollection = "PeopleCollection";
        private const string TransactionCollection = "TransactionCollection";

        public static async Task Main(string[] args)
        {
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();

                Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, PeopleCollection);
                object doc = new
                {
                    Person = new Bogus.Person(),
                    Relatives = new
                    {
                        Spouse = new Bogus.Person(), 
                        Children = Enumerable.Range(0, 4).Select(r => new Bogus.Person())
                    }
                };
                ResourceResponse<Document> response = await client.CreateDocumentAsync(collectionLink, doc);
                await Console.Out.WriteLineAsync($"{response.RequestCharge} RUs");

                Uri documentLink = UriFactory.CreateDocumentCollectionUri(DbName, PeopleCollection);
                doc = new {
                    id = "example.document",
                    FirstName = "Example",
                    LastName = "Person"
                };
                ResourceResponse<Document> readResponse = await client.UpsertDocumentAsync(collectionLink, doc);
                await Console.Out.WriteLineAsync($"{readResponse.StatusCode}");

                collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, TransactionCollection);
                var transactions = new Bogus.Faker<Transaction>()
                    .RuleFor(t => t.amount, (fake) => Math.Round(fake.Random.Double(5, 500), 2))
                    .RuleFor(t => t.processed, (fake) => fake.Random.Bool(0.6f))
                    .RuleFor(t => t.paidBy, (fake) => $"{fake.Name.FirstName().ToLower()}.{fake.Name.LastName().ToLower()}")
                    .RuleFor(t => t.costCenter, (fake) => fake.Commerce.Department(1).ToLower())
                    .GenerateLazy(100);
                 List<Task<ResourceResponse<Document>>> tasks = new List<Task<ResourceResponse<Document>>>();
                foreach(var transaction in transactions)
                {
                    Task<ResourceResponse<Document>> resultTask = client.CreateDocumentAsync(collectionLink, transaction);
                    tasks.Add(resultTask);
                }    
                Task.WaitAll(tasks.ToArray());
                foreach(var task in tasks)
                {
                    await Console.Out.WriteLineAsync($"Document Created\t{task.Result.Resource.Id}");
                }  

                 FeedOptions options = new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                    PopulateQueryMetrics = true
                };
                var sql = "SELECT c.id FROM c";
                IDocumentQuery<Document> query = client.CreateDocumentQuery<Document>(collectionLink, sql, options).AsDocumentQuery();
                var result = await query.ExecuteNextAsync();
                 foreach(string key in result.QueryMetrics.Keys)
                {
                    await Console.Out.WriteLineAsync($"{key}\t{result.QueryMetrics[key]}");
                }

                Stopwatch timer = new Stopwatch();
                options = new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                    MaxItemCount = 1000,
                    MaxDegreeOfParallelism = -1,
                    MaxBufferedItemCount = 50000
                };  
                await Console.Out.WriteLineAsync($"MaxItemCount:\t{options.MaxItemCount}");
                await Console.Out.WriteLineAsync($"MaxDegreeOfParallelism:\t{options.MaxDegreeOfParallelism}");
                await Console.Out.WriteLineAsync($"MaxBufferedItemCount:\t{options.MaxBufferedItemCount}");
                sql = "SELECT * FROM c WHERE c.processed = true ORDER BY c.amount DESC";
                timer.Start();
                query = client.CreateDocumentQuery<Document>(collectionLink, sql, options).AsDocumentQuery();
                 while (query.HasMoreResults)  
                {
                    var docResult = await query.ExecuteNextAsync<Document>();
                }
                timer.Stop();
                await Console.Out.WriteLineAsync($"Elapsed Time:\t{timer.Elapsed.TotalSeconds}");

                documentLink = UriFactory.CreateDocumentUri(DbName, PeopleCollection, "example.document");            
                response = await client.ReadDocumentAsync(documentLink);
                await Console.Out.WriteLineAsync($"Existing ETag:\t{response.Resource.ETag}"); 

                AccessCondition cond = new AccessCondition { Condition = response.Resource.ETag, Type = AccessConditionType.IfMatch };
                response.Resource.SetPropertyValue("FirstName", "Demo");
                var requestOptions = new RequestOptions { AccessCondition = cond };
                response = await client.ReplaceDocumentAsync(response.Resource, requestOptions);
                await Console.Out.WriteLineAsync($"New ETag:\t{response.Resource.ETag}");    
            }   
        }
    }
}
