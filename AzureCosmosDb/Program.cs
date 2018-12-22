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
        private const string DbName = "UniversityDatabase";
        private const string UnlimitedCollectionName = "StudentCollection";

        public static async Task Main(string[] args)
        {    
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
               Uri collectionLink = UriFactory.CreateDocumentCollectionUri(DbName, UnlimitedCollectionName);
               string sql = "SELECT TOP 5 VALUE s.studentAlias FROM coll s WHERE s.enrollmentYear = 2018 ORDER BY s.studentAlias";
               var query = client.CreateDocumentQuery<string>(collectionLink, new SqlQuerySpec(sql));
               foreach(string alias in query)
               {
                   await Console.Out.WriteLineAsync(alias);
               }

               sql = "SELECT s.firstName, s.lastName, s.clubs FROM students s WHERE s.enrollmentYear = 2018";
               var studentsQuery = client.CreateDocumentQuery<Student>(collectionLink, new SqlQuerySpec(sql));
                foreach(Student student in studentsQuery)
                {
                    await Console.Out.WriteLineAsync($"{student.FirstName} {student.LastName}");
                    foreach(string club in student.Clubs)
                    {
                        await Console.Out.WriteLineAsync($"\t{club}");
                    }
                    await Console.Out.WriteLineAsync();
                }

                sql =  "SELECT VALUE activity FROM students s JOIN activity IN s.clubs WHERE s.enrollmentYear = 2018";
                var activitiesQuery = client.CreateDocumentQuery<string>(collectionLink, new SqlQuerySpec(sql));
                 foreach(string studentActivity in activitiesQuery)
                {
                    await Console.Out.WriteLineAsync(studentActivity);
                }

                sql = "SELECT VALUE { 'id': s.id, 'name': CONCAT(s.firstName, ' ', s.lastName), 'email': { 'home': s.homeEmailAddress, 'school': CONCAT(s.studentAlias, '@contoso.edu') } } FROM students s WHERE s.enrollmentYear = 2018"; 
                var profilesQuery = client.CreateDocumentQuery<StudentProfile>(collectionLink, new SqlQuerySpec(sql), new FeedOptions { MaxItemCount = 100 }).AsDocumentQuery();
                int pageCount = 0;
                while(profilesQuery.HasMoreResults)
                {
                    await Console.Out.WriteLineAsync($"---Page #{++pageCount:0000}---");
                    foreach(StudentProfile profile in await profilesQuery.ExecuteNextAsync())
                    {
                        await Console.Out.WriteLineAsync($"\t[{profile.Id}]\t{profile.Name,-20}\t{profile.Email.School,-50}\t{profile.Email.Home}");
                    }
                }

                 var crossQuery = client
                    .CreateDocumentQuery<StudentWithAlias>(collectionLink, new FeedOptions { EnableCrossPartitionQuery = true })
                    .Where(student => student.projectedGraduationYear == 2020);
                 foreach(StudentWithAlias student in crossQuery)
                {
                    Console.Out.WriteLine($"Enrolled: {student.enrollmentYear}\tGraduation: {student.projectedGraduationYear}\t{student.studentAlias}");
                }      

                string continuationToken = String.Empty;
                do
                {
                    var opt = new FeedOptions 
                    { 
                        EnableCrossPartitionQuery = true, 
                        RequestContinuation = continuationToken 
                    };
                    var continuationQuery = client
                        .CreateDocumentQuery<StudentWithAlias>(collectionLink, opt)
                        .Where(student => student.age < 18)
                        .AsDocumentQuery();

                    var results = await continuationQuery.ExecuteNextAsync<StudentWithAlias>();                
                    continuationToken = results.ResponseContinuation;

                    await Console.Out.WriteLineAsync($"ContinuationToken:\t{continuationToken}");
                    foreach(StudentWithAlias result in results)
                    {
                        await Console.Out.WriteLineAsync($"[Age: {result.age}]\t{result.studentAlias}@consoto.edu");
                    }
                    await Console.Out.WriteLineAsync(); 
                } 
                while (!String.IsNullOrEmpty(continuationToken));

                var options = new FeedOptions 
                { 
                    EnableCrossPartitionQuery = true
                };

                sql = "SELECT * FROM students s WHERE s.financialData.tuitionBalance > 14998";

                var crossPartitionQuery = client
                    .CreateDocumentQuery<StudentWithAlias>(collectionLink, sql, options)
                    .AsDocumentQuery();

                pageCount = 0;
                while(crossPartitionQuery.HasMoreResults)
                {
                    await Console.Out.WriteLineAsync($"---Page #{++pageCount:0000}---");
                    foreach(StudentWithAlias result in await crossPartitionQuery.ExecuteNextAsync())
                    {
                        await Console.Out.WriteLineAsync($"Enrollment: {result.enrollmentYear}\tBalance: {result.financialData.tuitionBalance}\t{result.studentAlias}@consoto.edu");
                    }
                }        
            }     
        }
    }
}
