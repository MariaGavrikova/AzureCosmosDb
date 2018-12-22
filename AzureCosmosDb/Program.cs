 using Bogus;
 using System;
 using System.Threading.Tasks;
 using Microsoft.Azure.Documents;
 using Microsoft.Azure.Documents.Client;
 using System.Collections.Generic;
 using System.Linq;

namespace AzureCosmosDb
{
    public class Program
    { 
        private static readonly Uri _endpointUri = new Uri("https://cosmos-db-lab.documents.azure.com:443/");
        private const string _primaryKey = "DWH1jWd0fruP8Gl2rEywhV42AFBeJjWYKBRg4wuagc6XDRkZvmZFBg6rnL1y2MO9Qx6esdKC3InkcE0KmSVBgQ==";
        private const string DbName = "FinancialDatabase";
        private const string UnlimitedCollectionName = "InvestorCollection";

        public static async Task Main(string[] args)
        {    
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
               await client.OpenAsync();

               Uri sprocLink = UriFactory.CreateStoredProcedureUri(DbName, UnlimitedCollectionName, "bulkUpload");
               List<Person> people = new Faker<Person>()
                .RuleFor(p => p.firstName, f => f.Name.FirstName())
                .RuleFor(p => p.lastName, f => f.Name.LastName())
                .RuleFor(p => p.company, f => "contosofinancial")
                .Generate(5000);
               int pointer = 0;
               while(pointer < people.Count)
                {
                    RequestOptions options = new RequestOptions { PartitionKey = new PartitionKey("contosofinancial") };
                    StoredProcedureResponse<int> result = await client.ExecuteStoredProcedureAsync<int>(sprocLink, options, people.Skip(pointer));
                    pointer += result.Response;
                    await Console.Out.WriteLineAsync($"{pointer} Total Documents\t{result.Response} Documents Uploaded in this Iteration");
                }

                sprocLink = UriFactory.CreateStoredProcedureUri(DbName, UnlimitedCollectionName, "bulkDelete");
                bool resume = true;
                do
                {
                    RequestOptions options = new RequestOptions { PartitionKey = new PartitionKey("contosofinancial") };
                    string query = "SELECT * FROM investors i WHERE i.company = 'contosofinancial'";
                    StoredProcedureResponse<DeleteStatus> result = await client.ExecuteStoredProcedureAsync<DeleteStatus>(sprocLink, options, query);
                    await Console.Out.WriteLineAsync($"Batch Delete Completed.\tDeleted: {result.Response.Deleted}\tContinue: {result.Response.Continuation}");
                    resume = result.Response.Continuation;
                }
                while(resume);
            }     
        }
    }
}
