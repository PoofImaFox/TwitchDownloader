using TwitchDownloader.Services.TwitchGraphQl.TwitchGraphModels;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Abstractions;

namespace TwitchDownloader.Services.TwitchGraphQl {
    public class TwitchUserApi {
        private readonly GraphQLHttpClient _graphQLHttpClient;

        public TwitchUserApi(IGraphQLClient graphQLHttpClient) {
            _graphQLHttpClient = graphQLHttpClient as GraphQLHttpClient;
        }

        public async Task<TwitchUser> GetUserByUsername(string username) {
            var queryUserRequest = new GraphQLRequest {
                Query = @"
                    query GetChannelIdsByUsername($username: String!) {
                    user(login:$username) {
                        displayName,
                        login,
                        id,
                        createdAt,
                        hasPrime,
                        hasTurbo,
                        isEmailReusable,
                        isEmailVerified,
                        isFlaggedToDelete,
                        isSiteAdmin,
                        isStaff,
                        isGlobalMod,
                        isPartner,
                        isPhoneNumberVerified,
                        chatColor,
                        primaryColorHex,
                        profileURL,
                        profileViewCount,
                        bannerImageURL
                    }
                }",
                Variables = new {
                    username = username
                }
            };

            var graphResponse = await _graphQLHttpClient.SendQueryAsync<TwitchUserGraph>(queryUserRequest);

            if (graphResponse.Errors is GraphQLError[] graphErrors) {
                throw new Exception($"{graphErrors.Length}, graph ql errors!\n{string.Join("\n", graphErrors.Select(i => i.Message))}");
            }

            return graphResponse.Data.TwitchUser;
        }
    }
}
