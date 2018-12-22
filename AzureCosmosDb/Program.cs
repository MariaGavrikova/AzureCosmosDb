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
    public class Program
    { 
        private static readonly Uri _endpointUri = new Uri("https://cosmos-db-lab.documents.azure.com:443/");
        private const string _primaryKey = "DWH1jWd0fruP8Gl2rEywhV42AFBeJjWYKBRg4wuagc6XDRkZvmZFBg6rnL1y2MO9Qx6esdKC3InkcE0KmSVBgQ==";
        private const string DbName = "EntertainmentDatabase";
        private const string FixedCollectionName = "DefaultCollection";
        private const string UnlimitedCollectionName = "CustomCollection";
        private const int DocumentCount = 10;

        public static async Task Main(string[] args)
        {    
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                Database targetDatabase = new Database { Id = DbName };
                targetDatabase = await client.CreateDatabaseIfNotExistsAsync(targetDatabase);
                await Console.Out.WriteLineAsync($"Database Self-Link:\t{targetDatabase.SelfLink}");

                await client.OpenAsync();
                await client.CreateCollectionAsync(FixedCollectionName, targetDatabase);

                IndexingPolicy indexingPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths = new Collection<IncludedPath>
                    {
                        new IncludedPath
                        {
                            Path = "/*",
                            Indexes = new Collection<Index>
                            {
                                new RangeIndex(DataType.Number, -1),
                                new RangeIndex(DataType.String, -1)                           
                            }
                        }
                    }
                };
                PartitionKeyDefinition partitionKeyDefinition = new PartitionKeyDefinition
                {
                    Paths = new Collection<string> { "/type" }
                };
                var customCollection = await client.CreateCollectionAsync(UnlimitedCollectionName, targetDatabase, partitionKeyDefinition, indexingPolicy);

                await CreateFoodInteractionsAsync(client, customCollection);
                await CreateTvInteractionsAsync(client, customCollection);
                await CreateMapInteractionsAsync(client, customCollection);                
            }     
        }

        private static async Task CreateFoodInteractionsAsync(DocumentClient client, DocumentCollection customCollection)
        {
            var foodInteractions = new Bogus.Faker<PurchaseFoodOrBeverage>()
                .RuleFor(i => i.type, (fake) => nameof(PurchaseFoodOrBeverage))
                .RuleFor(i => i.unitPrice, (fake) => Math.Round(fake.Random.Decimal(1.99m, 15.99m), 2))
                .RuleFor(i => i.quantity, (fake) => fake.Random.Number(1, 5))
                .RuleFor(i => i.totalPrice, (fake, user) => Math.Round(user.unitPrice * user.quantity, 2))
                .Generate(DocumentCount);  
            foreach(var interaction in foodInteractions)
            {
                ResourceResponse<Document> result = await client.CreateDocumentAsync(customCollection.SelfLink, interaction);
                await Console.Out.WriteLineAsync($"Document #{foodInteractions.IndexOf(interaction):000} Created\t{result.Resource.Id}");
            }
        }

        private static async Task CreateTvInteractionsAsync(DocumentClient client, DocumentCollection customCollection)
        {
            var tvInteractions = new Bogus.Faker<WatchLiveTelevisionChannel>()
                .RuleFor(i => i.type, (fake) => nameof(WatchLiveTelevisionChannel))
                .RuleFor(i => i.minutesViewed, (fake) => fake.Random.Number(1, 45))
                .RuleFor(i => i.channelName, (fake) => fake.PickRandom(new List<string> { "NEWS-6", "DRAMA-15", "ACTION-12", "DOCUMENTARY-4", "SPORTS-8" }))
                .Generate(DocumentCount);
            foreach(var interaction in tvInteractions)
            {
                ResourceResponse<Document> result = await client.CreateDocumentAsync(customCollection.SelfLink, interaction);
                await Console.Out.WriteLineAsync($"Document #{tvInteractions.IndexOf(interaction):000} Created\t{result.Resource.Id}");
            }
        }

        private static async Task CreateMapInteractionsAsync(DocumentClient client, DocumentCollection customCollection)
        {
            var mapInteractions = new Bogus.Faker<ViewMap>()
                .RuleFor(i => i.type, (fake) => nameof(ViewMap))
                .RuleFor(i => i.minutesViewed, (fake) => fake.Random.Number(1, 45))
                .Generate(DocumentCount);
            foreach(var interaction in mapInteractions)
            {
                ResourceResponse<Document> result = await client.CreateDocumentAsync(customCollection.SelfLink, interaction);
                await Console.Out.WriteLineAsync($"Document #{mapInteractions.IndexOf(interaction):000} Created\t{result.Resource.Id}");
            }
        }
    }
}
