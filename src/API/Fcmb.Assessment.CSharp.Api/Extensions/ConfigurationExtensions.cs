using Asp.Versioning;
using Fcmb.Assessment.CSharp.Api.OpenApi;

namespace Fcmb.Assessment.CSharp.Api.Extensions;

internal static class ConfigurationExtensions
{
    extension(IConfigurationBuilder configurationBuilder)
    {
        internal void AddModuleConfiguration(string[] modules)
        {
            foreach (string module in modules)
            {
                configurationBuilder.AddJsonFile($"modules.{module}.json", false, true);
                configurationBuilder.AddJsonFile($"modules.{module}.Development.json", true, true);
            }
        }
    }

    extension(IServiceCollection services)
    {
        internal IServiceCollection AddOpenApiInternal()
        {
            services.AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1);
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
                })
                .AddOpenApi(options =>
                {
                    options.Document.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();

                    options.Document.CreateSchemaReferenceId = jsonTypeInfo =>
                    {
                        string fullName = jsonTypeInfo.Type.FullName?.Replace("+", ".") ?? jsonTypeInfo.Type.Name;
                        string[] parts = fullName.Split('.');

                        if (parts.Length < 2)
                        {
                            return parts[^1];
                        }

                        string className = parts[^1];
                        string folderName = parts[^2];

                        return $"{folderName}.{className}";
                    };
                });

            return services;
        }
    }
}
