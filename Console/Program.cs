namespace GraphGetStarted
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Azure.Graphs;
    using Microsoft.Azure.Graphs.Elements;
    using Newtonsoft.Json;

    public class Program
    {
        public static void Main(string[] args)
        {
            string endpoint = ConfigurationManager.AppSettings["Endpoint"];
            string authKey = ConfigurationManager.AppSettings["AuthKey"];

            using (DocumentClient client = new DocumentClient(
                new Uri(endpoint),
                authKey,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                Program p = new Program();
                p.RunAsync(client).Wait();
            }
        }

        public async Task RunAsync(DocumentClient client)
        {
            var db = "graphdb";
            var coll = "graphcoll";

            var database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = db });

            var graph = client.CreateDocumentCollectionQuery(database.Resource.SelfLink).ToArray().FirstOrDefault(x => x.Id == coll);

            if (graph != null)
            {
                await client.DeleteDocumentCollectionAsync(graph.SelfLink);
            }

            graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                        UriFactory.CreateDatabaseUri(db),
                        new DocumentCollection { Id = coll },
                        new RequestOptions { OfferThroughput = 1000 });

            var queries = new List<string>
            {
                "g.addV('person').property('id', 'jlane').property('firstName', 'josh').property('lastName', 'lane').property('hair', 'brown').property('age', 44)",
                "g.addV('person').property('id', 'clane').property('firstName', 'cary').property('lastName', 'lane').property('hair', 'brown').property('age', 43)",
                "g.addV('person').property('id', 'mlane').property('firstName', 'mallory').property('lastName', 'lane').property('hair', 'brown').property('age', 14)",
                "g.addV('person').property('id', 'dlane').property('firstName', 'darby').property('lastName', 'lane').property('hair', 'blonde').property('age', 12)",
                "g.addV('person').property('id', 'lmonetti').property('firstName', 'linda').property('lastName', 'monetti').property('hair', 'blonde')",
                "g.addV('person').property('id', 'nmonetti').property('firstName', 'neil').property('lastName', 'monetti').property('hair', 'gray')",
                "g.addV('person').property('id', 'rgreen').property('firstName', 'ron').property('lastName', 'green').property('hair', 'gray')",
                "g.addV('person').property('id', 'dwoodberry').property('firstName', 'deborah').property('lastName', 'woodberry').property('hair', 'brown')",
                "g.addV('person').property('id', 'alane').property('firstName', 'adam').property('lastName', 'lane').property('hair', 'none')",
                "g.addV('person').property('id', 'rwhittenburg').property('firstName', 'robin').property('lastName', 'whittenburg').property('hair', 'brown')",
                "g.addV('location').property('id', 'georgia').property('name', 'Georgia')",
                "g.addV('location').property('id', 'north-carolina').property('name', 'North Carolina')",
                "g.addV('location').property('id', 'bermuda').property('name', 'Bermuda')",
                "g.V('jlane').addE('resides in').to(g.V('georgia')).property('years', 25)",
                "g.V('clane').addE('resides in').to(g.V('georgia')).property('years', 43)",
                "g.V('mlane').addE('resides in').to(g.V('georgia')).property('years', 14)",
                "g.V('dlane').addE('resides in').to(g.V('georgia')).property('years', 12)",
                "g.V('lmonetti').addE('resides in').to(g.V('north-carolina')).property('years', 3)",
                "g.V('nmonetti').addE('resides in').to(g.V('north-carolina')).property('years', 3)",
                "g.V('rgreen').addE('resides in').to(g.V('georgia')).property('years', 55)",
                "g.V('dwoodberry').addE('resides in').to(g.V('north-carolina')).property('years', 12)",
                "g.V('rwhittenburg').addE('resides in').to(g.V('bermuda')).property('years', 4)",
                "g.V('alane').addE('resides in').to(g.V('north-carolina')).property('years', 22)",
                "g.V('jlane').addE('is parent of').to(g.V('mlane'))",
                "g.V('jlane').addE('is parent of').to(g.V('dlane'))",
                "g.V('clane').addE('is parent of').to(g.V('mlane'))",
                "g.V('clane').addE('is parent of').to(g.V('dlane'))",
                "g.V('lmonetti').addE('is parent of').to(g.V('jlane'))",
                "g.V('lmonetti').addE('is parent of').to(g.V('alane'))",
                "g.V('dwoodberry').addE('is parent of').to(g.V('clane'))",
                "g.V('rgreen').addE('is parent of').to(g.V('clane'))",
                "g.V('dwoodberry').addE('is parent of').to(g.V('rwhittenburg'))",
                "g.V('rgreen').addE('is parent of').to(g.V('rwhittenburg'))"
            };

            foreach (var text in queries)
            {
                Console.WriteLine($"Running {text}");

                var query = client.CreateGremlinQuery<dynamic>(graph, text);

                while (query.HasMoreResults)
                {
                    foreach (dynamic result in await query.ExecuteNextAsync())
                    {
                        Console.WriteLine($"\t {JsonConvert.SerializeObject(result)}");
                    }
                }

                Console.WriteLine();
            }

            //Dictionary<string, string> gremlinQueries = new Dictionary<string, string>
            //{
            //    { "Cleanup",        "g.V().drop()" },
            //    { "AddVertex 1",    "g.addV('person').property('id', 'thomas').property('firstName', 'Thomas').property('age', 44)" },
            //    { "AddVertex 2",    "g.addV('person').property('id', 'mary').property('firstName', 'Mary').property('lastName', 'Andersen').property('age', 39)" },
            //    { "AddVertex 3",    "g.addV('person').property('id', 'ben').property('firstName', 'Ben').property('lastName', 'Miller')" },
            //    { "AddVertex 4",    "g.addV('person').property('id', 'robin').property('firstName', 'Robin').property('lastName', 'Wakefield')" },
            //    { "AddEdge 1",      "g.V('thomas').addE('knows').to(g.V('mary'))" },
            //    { "AddEdge 2",      "g.V('thomas').addE('knows').to(g.V('ben'))" },
            //    { "AddEdge 3",      "g.V('ben').addE('knows').to(g.V('robin'))" },
            //    //{ "UpdateVertex",   "g.V('thomas').property('age', 44)" },
            //    //{ "CountVertices",  "g.V().count()" },
            //    //{ "Filter Range",   "g.V().hasLabel('person').has('age', gt(40))" },
            //    //{ "Project",        "g.V().hasLabel('person').values('firstName')" },
            //    //{ "Sort",           "g.V().hasLabel('person').order().by('firstName', decr)" },
            //    //{ "Traverse",       "g.V('thomas').outE('knows').inV().hasLabel('person')" },
            //    //{ "Traverse 2x",    "g.V('thomas').outE('knows').inV().hasLabel('person').outE('knows').inV().hasLabel('person')" },
            //    //{ "Loop",           "g.V('thomas').repeat(out()).until(has('id', 'robin')).path()" },
            //    //{ "DropEdge",       "g.V('thomas').outE('knows').where(inV().has('id', 'mary')).drop()" },
            //    //{ "CountEdges",     "g.E().count()" },
            //    //{ "DropVertex",     "g.V('thomas').drop()" },
            //};

            //foreach (KeyValuePair<string, string> gremlinQuery in gremlinQueries)
            //{
            //    Console.WriteLine($"Running {gremlinQuery.Key}: {gremlinQuery.Value}");

            //    // The CreateGremlinQuery method extensions allow you to execute Gremlin queries and iterate
            //    // results asychronously
            //    IDocumentQuery<dynamic> query = client.CreateGremlinQuery<dynamic>(graph, gremlinQuery.Value);
            //    while (query.HasMoreResults)
            //    {
            //        foreach (dynamic result in await query.ExecuteNextAsync())
            //        {
            //            Console.WriteLine($"\t {JsonConvert.SerializeObject(result)}");
            //        }
            //    }

            //    Console.WriteLine();
            //}

            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadLine();
        }
    }
}
