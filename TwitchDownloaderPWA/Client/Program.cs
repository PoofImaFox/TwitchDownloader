using BlazorStrap;

using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Newtonsoft.Json.Serialization;

using TwitchDownloader.Services;
using TwitchDownloader.Services.TwitchGraphQl;

namespace TwitchDownloaderPWA {
    public class Program {
        public static async Task Main(string[] args) {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddBlazorStrap();
            builder.Services
                .AddSingleton<IGraphQLClient>(serviceCollection => {
                    var graphClient = new GraphQLHttpClient(
                        new GraphQLHttpClientOptions() {
                            EndPoint = new Uri("https://gql.twitch.tv/gql")
                        },
                        new NewtonsoftJsonSerializer(settings => {
                            settings.ContractResolver = new DefaultContractResolver {
                                NamingStrategy = new DefaultNamingStrategy()
                            };
                        }));

                    graphClient.HttpClient.DefaultRequestHeaders.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                    return graphClient;
                })
                .AddSingleton<TwitchUserApi>()
                .AddSingleton<TwitchGraphQlClient>();

            await builder.Build().RunAsync();
        }
    }
}