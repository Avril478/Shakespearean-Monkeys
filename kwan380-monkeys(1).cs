namespace Monkeys {
    using Carter;
    using Carter.ModelBinding;
    using Carter.Request;
    using Carter.Response;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using static System.Console;
    
    public class HomeModule : CarterModule {
        public HomeModule () {
            Post ("/try", async (req, res) => {
                await Task.Delay (0);
                var tryRequest = await req.Bind<TryRequest> ();
                GeneticAlgorithm(tryRequest);
                return ;
            });
        }
        
        async Task<AssessResponse> PostFitnessAssess (AssessRequest a) {
            await Task.Delay (0);
            var client = new HttpClient ();
            client.BaseAddress = new Uri ("http://localhost:8091/");
            client.DefaultRequestHeaders.Accept.Clear ();
            client.DefaultRequestHeaders.Accept.Add (
                new MediaTypeWithQualityHeaderValue ("application/json"));
            var hrm = await client.PostAsJsonAsync ("/assess", a);
            hrm.EnsureSuccessStatusCode ();
            var ares = await hrm.Content.ReadAsAsync <AssessResponse>();
            return ares;
        }
        
        async Task PostClientTop (TopRequest t) {
            await Task.Delay (0);
            var client = new HttpClient ();               
            client.BaseAddress = new Uri ("http://localhost:8101/");
            client.DefaultRequestHeaders.Accept.Clear ();
            client.DefaultRequestHeaders.Accept.Add (
                new MediaTypeWithQualityHeaderValue ("application/json"));    
            var hrm = await client.PostAsJsonAsync ("/top", t);
            return;
        }
        
        private Random _random = new Random (1);
        
        private double NextDouble () {
            lock (this) {
                return _random.NextDouble ();
            }
        }
        
        private int NextInt (int a, int b) {
            lock (this) {
                return _random.Next (a, b);
            }
        }

        int ProportionalRandom (int[] weights, int sum) {
            var val = NextDouble () * sum;
            
            for (var i = 0; i < weights.Length; i ++) {
                if (val < weights[i]) return i;
                
                val -= weights[i];
            }
            
            WriteLine ($"***** Unexpected ProportionalRandom Error");
            return 0;
        }

        async void GeneticAlgorithm (TryRequest treq) {
            WriteLine ($"..... GeneticAlgorithm {treq}");
            await Task.Delay (0);            
            var id = treq.id;
            var monkeys = treq.monkeys;
            if (monkeys % 2 != 0) monkeys += 1;
            var length = 0;
            if (treq.length == 0){
                var nullText = new[] {""}.ToList();
                var r = new AssessRequest{id=id, genomes = nullText};
                AssessResponse ares = await PostFitnessAssess(r);
                var scores = ares.scores;
                length = scores[0];
            }
            else{
                length = treq.length;
            }
            var crossover = treq.crossover / 100.0 ;
            var mutation = treq.mutation / 100.0;
            var limit = treq.limit;
            if (limit == 0) limit = 1000;
            var topscore = int.MaxValue;
            var genomes = Enumerable.Range(0,monkeys).Select(i => {
                    var genome = Enumerable.Range(0,length).Select(n => {
                        return (char)NextInt(32,127);
                    }).ToList();
                    return string.Join("",genome);
            }).ToList();

            var obj1 = new AssessRequest {id = id, genomes = genomes};      
            for (int loop = 0; loop < limit; loop ++) {
                AssessResponse x = await PostFitnessAssess(obj1);
                var Id = x.id;
                var scores = x.scores;
                var smallest = scores.Min();
                var largest = scores.Max();
                var index = scores.IndexOf(smallest);
                var genome_list = obj1.genomes;
                var genome = genome_list[index];
                if (smallest<topscore){
                    var obj2 = new TopRequest{id = Id,loop=loop, score=smallest, genome = genome};
                    await PostClientTop(obj2); 
                    topscore = smallest;
                }
                
                if(smallest == 0){
                    break;
                }else{
                    var weights = scores.Select(s =>{
                        return largest - s + 1;
                    });
                    var para = treq.parallel;

                    if (para){
                        var newGenomes = ParallelEnumerable.Range(1, monkeys/2).SelectMany<int, string>(i =>{
                        var c1 = "";
                        var c2 = "";
                        var index1 = ProportionalRandom(weights.ToArray(), weights.Sum());
                        var index2 = ProportionalRandom(weights.ToArray(),weights.Sum());
                        var p1 =genome_list[index1];
                        var p2 = genome_list[index2];
                        
                        if(NextDouble()<crossover){
                            var crossoverIndex = NextInt(0,length);
                            c1 = p1.Substring(0,crossoverIndex) + p2.Substring(crossoverIndex,p2.Length-crossoverIndex);
                            c2 = p2.Substring(0,crossoverIndex) + p1.Substring(crossoverIndex,p1.Length-crossoverIndex);
                        }else{
                            c1 = p1;
                            c2 = p2;
                        }

                        if(NextDouble()<mutation){
                            char[] c = c1.ToCharArray();
                            c[NextInt(0,c1.Length)] = (char)NextInt(32,127);
                            c1 = new string(c);
                        }
                        if(NextDouble()<mutation){

                            char[] c = c2.ToCharArray();
                            c[NextInt(0,c2.Length)] = (char)NextInt(32,127);
                            c2 = new string(c);
                        }
                        var mylist = new List<string>();
                        mylist.Add(c1);
                        mylist.Add(c2);
                        return  mylist;
                    }).ToList();
                        obj1.id = Id;
                        obj1.genomes=newGenomes;
                    }
                    else{
                    var newGenomes = Enumerable.Range(1, monkeys/2).SelectMany<int, string>(i =>{
                        var c1 = "";
                        var c2 = "";
                        var index1 = ProportionalRandom(weights.ToArray(), weights.Sum());
                        var index2 = ProportionalRandom(weights.ToArray(), weights.Sum());
                        var p1 =genome_list[index1];
                        var p2 = genome_list[index2];
                        
                        if(NextDouble()<crossover){
                            var crossoverIndex = NextInt(0,length);
                            c1 = p1.Substring(0,crossoverIndex) + p2.Substring(crossoverIndex,p2.Length-crossoverIndex);
                            c2 = p2.Substring(0,crossoverIndex) + p1.Substring(crossoverIndex,p1.Length-crossoverIndex);
                        }else{
                            c1 = p1;
                            c2 = p2;
                        }

                        if(NextDouble()<mutation){
                            char[] c = c1.ToCharArray();
                            c[NextInt(0,c1.Length)] = (char)NextInt(32,127);
                            c1 = new string(c);
                        }
                        if(NextDouble()<mutation){
                            char[] c = c2.ToCharArray();
                            c[NextInt(0,c2.Length)] = (char)NextInt(32,127);
                            c2 = new string(c);
                        }

                        var mylist = new List<string>();
                        mylist.Add(c1);
                        mylist.Add(c2);
                        return  mylist;
                    }).ToList();

                    obj1.id = Id;
                    obj1.genomes=newGenomes;
                    }
                    
                }

            }
        }
    }
    
    // public class TargetRequest {
        // public int id { get; set; }
        // public bool parallel { get; set; }
        // public string target { get; set; }
        // public override string ToString () {
            // return $"{{{id}, {parallel}, \"{target}\"}}";
        // }  
    // }    

    public class TryRequest {
        public int id { get; set; }
        public bool parallel { get; set; }
        public int monkeys { get; set; }
        public int length { get; set; }
        public int crossover { get; set; }
        public int mutation { get; set; }
        public int limit { get; set; }
        public override string ToString () {
            return $"{{{id}, {parallel}, {monkeys}, {length}, {crossover}, {mutation}, {limit}}}";
        }
    }
    
    public class TopRequest {
        public int id { get; set; }
        public int loop { get; set; }
        public int score { get; set; }
        public string genome { get; set; }
        public override string ToString () {
            return $"{{{id}, {loop}, {score}, {genome}}}";
        }  
    }    
    
    public class AssessRequest {
        public int id { get; set; }
        public List<string> genomes { get; set; }
        public override string ToString () {
            return $"{{{id}, #{genomes.Count}}}";
        }  
    }
    
    public class AssessResponse {
        public int id { get; set; }
        public List<int> scores { get; set; }
        public override string ToString () {
            return $"{{{id}, #{scores.Count}}}";
        }  
    }   
}

namespace Monkeys {
    using Carter;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup {
        public void ConfigureServices (IServiceCollection services) {
            services.AddCarter ();
        }

        public void Configure (IApplicationBuilder app) {
            app.UseRouting ();
            app.UseEndpoints( builder => builder.MapCarter ());
        }
    }
}

namespace Monkeys {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program {
        public static void Main (string[] args) {
//          var host = Host.CreateDefaultBuilder (args)
//              .ConfigureWebHostDefaults (webBuilder => webBuilder.UseStartup<Startup>())

            var urls = new[] {"http://localhost:8081"};
            
            var host = Host.CreateDefaultBuilder (args)
            
                .ConfigureLogging (logging => {
                    logging
                        .ClearProviders ()
                        .AddConsole ()
                        .AddFilter (level => level >= LogLevel.Warning);
                })
                
                .ConfigureWebHostDefaults (webBuilder => {
                    webBuilder.UseStartup<Startup> ();
                    webBuilder.UseUrls (urls);  // !!!
                })
                
                .Build ();
            
            System.Console.WriteLine ($"..... starting on {string.Join (", ", urls)}");            
            host.Run ();
        }
    }
}

