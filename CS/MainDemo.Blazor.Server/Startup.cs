using System.Text;
using Demos.Data;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.WebApi.Services;
using DevExpress.Persistent.Base;
using MainDemo.Blazor.Server.Services;
using MainDemo.Module;
using MainDemo.Module.BusinessObjects;
using MainDemo.Module.BusinessObjects.NonPersistent;
using MainDemo.WebApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace MainDemo.Blazor.Server;

public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services) {
        services.AddScoped<IDataService, CustomDataService>();

        services.AddSingleton(typeof(HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<CircuitHandler, Services.Circuits.CircuitHandlerProxy>();
        services.AddXaf(Configuration, builder => {
            builder.UseApplication<MainDemoBlazorApplication>();

            builder.AddXafWebApi(webApiBuilder => {
                webApiBuilder.ConfigureOptions(options => {
                    options.BusinessObject<ApplicationUser>();
                    options.BusinessObject<Department>();
                    options.BusinessObject<Employee>();
                    options.BusinessObject<Location>();
                    options.BusinessObject<Paycheck>();
                    options.BusinessObject<PortfolioFileData>();
                    options.BusinessObject<Position>();
                    options.BusinessObject<Resume>();
                    options.BusinessObject<DemoTask>();

                    options.BusinessObject<CustomNonPersistentObject>();

                    options.ConfigureBusinessObjectActionEndpoints(options => {
                        options.EnableActionEndpoints = true;
                    });
                });
            });

            builder.Modules
                .AddAuditTrailEFCore()
                .AddCloning()
                .AddConditionalAppearance()
                .AddDashboards(options => {
                    options.DashboardDataType = typeof(DevExpress.Persistent.BaseImpl.EF.DashboardData);
                })
                .AddFileAttachments()
                .AddOffice()
                .AddReports(options => {
                    options.EnableInplaceReports = true;
                    options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.EF.ReportDataV2);
                    options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
                })
                .AddValidation(options => {
                    options.AllowValidationDetailsAccess = false;
                })
                .AddScheduler()
                .AddNotifications()
                .AddMainDemoModule()
                .Add<MainDemoBlazorModule>();

            builder.ObjectSpaceProviders
                .AddSecuredEFCore(o => o.PreFetchReferenceProperties())
                    .WithAuditedDbContext(contexts => {
                        contexts.Configure<MainDemoDbContext, AuditingDbContext>(
                            (serviceProvider, businessObjectDbContextOptions) => {
                                // Uncomment this code to use an in-memory database. This database is recreated each time the server starts. With the in-memory database, you don't need to make a migration when the data model is changed.
                                // Do not use this code in production environment to avoid data loss.
                                // We recommend that you refer to the following help topic before you use an in-memory database: https://docs.microsoft.com/en-us/ef/core/testing/in-memory
                                //businessObjectDbContextOptions.UseInMemoryDatabase();
                                string connectionString = GetConnectionString(Configuration);
                                bool isSqlServerAccessible = DemoDbEngineDetectorHelper.IsSqlServerAccessible();
                                ArgumentNullException.ThrowIfNull(connectionString);

                                if(isSqlServerAccessible) {
                                    businessObjectDbContextOptions.UseConnectionString(connectionString);
                                }
                                else {
                                    businessObjectDbContextOptions.UseInMemoryDatabase();
                                }
                                businessObjectDbContextOptions.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
                            },
                            (serviceProvider, auditHistoryDbContextOptions) => {
                                string connectionString = GetConnectionString(Configuration);
                                bool isSqlServerAccessible = DemoDbEngineDetectorHelper.IsSqlServerAccessible();
                                ArgumentNullException.ThrowIfNull(connectionString);

                                if(isSqlServerAccessible) {
                                    auditHistoryDbContextOptions.UseConnectionString(connectionString);
                                }
                                else {
                                    auditHistoryDbContextOptions.UseInMemoryDatabase();
                                }
                            });
                    })
                .AddNonPersistent();

            builder.Security
                .UseIntegratedMode(options => {
                    // See the security configuration in the MainDemoModuleExtensions.ConfigureSecurity method.
                })
                .AddPasswordAuthentication(options => {
                    options.IsSupportChangePassword = true;
                })
                .AddAuthenticationProvider<CustomAuthenticationProvider>();

            builder.AddBuildStep(application => {
                application.ApplicationName = "MainDemo";
                application.CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
                application.DatabaseVersionMismatch += (s, e) => {
                    e.Updater.Update();
                    e.Handled = true;
                };
                application.LastLogonParametersRead += (s, e) => {
                    if(e.LogonObject is AuthenticationStandardLogonParameters logonParameters && string.IsNullOrEmpty(logonParameters.UserName)) {
                        logonParameters.UserName = "Sam";
                    }
                };
            });
        });

        var authenticationBuilder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.LoginPath = "/LoginPage";
            })
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters() {
                    ValidIssuer = Configuration["Authentication:Jwt:ValidIssuer"],
                    ValidAudience = Configuration["Authentication:Jwt:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Authentication:Jwt:IssuerSigningKey"])),
                    AuthenticationType = JwtBearerDefaults.AuthenticationScheme
                };
            });

        /* authenticationBuilder.AddMicrosoftIdentityWebApp(
            msIdentityOptions => {
                Configuration.Bind("Authentication:AzureAd", msIdentityOptions);
            }, cookieScheme: null, openIdConnectScheme: "MicrosoftEntraID"); */

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireXafAuthentication()
                    .Build());

        services
            .AddControllers()
            .AddOData((options, serviceProvider) => {
                options
                    .EnableQueryFeatures(100)
                    .AddRouteComponents("api/odata", new EdmModelBuilder(serviceProvider).GetEdmModel(), Microsoft.OData.ODataVersion.V401, _routeServices => {
                        _routeServices.ConfigureXafWebApiServices();

#if DEBUG
                        // Batch edit ->
                        _routeServices.AddSingleton<ODataBatchHandler, DefaultODataBatchHandler>();
                        // <- Batch edit
#endif
                    });
            });

        services.AddSwaggerGen(c => {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo {
                Title = "MainDemo",
                Version = "v1"
            });

            c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme() {
                Type = SecuritySchemeType.Http,
                Name = "Bearer",
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                        {
                            new OpenApiSecurityScheme() {
                                Reference = new OpenApiReference() {
                                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                    Id = "JWT"
                                }
                            },
                            new string[0]
                        },
                });
        });

        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => {
            //The code below specifies that the naming of properties in an object serialized to JSON must always exactly match
            //the property names within the corresponding CLR type so that the property names are displayed correctly in the Swagger UI.
            //XPO is case-sensitive and requires this setting so that the example request data displayed by Swagger is always valid.
            //Comment this code out to revert to the default behavior.
            //See the following article for more information: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.propertynamingpolicy
            o.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if(env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MainDemo WebApi v1");
            });

#if DEBUG
            // Batch edit ->
            app.UseODataBatching();
            app.UseODataQueryRequest();
            // <- Batch edit
#endif
        }
        else {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseXaf();
        app.UseEndpoints(endpoints => {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
            endpoints.MapControllers();
            endpoints.MapXafEndpoints();
        });
    }

    string GetConnectionString(IConfiguration configuration) {
        string connectionString = null;
        if(configuration.GetConnectionString("ConnectionString") != null) {
            connectionString = configuration.GetConnectionString("ConnectionString");
        }
#if EASYTEST
            if(configuration.GetConnectionString("EasyTestConnectionString") != null) {
                connectionString = configuration.GetConnectionString("EasyTestConnectionString");
            }
#endif
        return connectionString;
    }
}

