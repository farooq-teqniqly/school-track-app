using Microsoft.AspNetCore.Components.Authorization;
using Trakmark.Components;
using Trakmark.Components.Account;
using Trakmark.Extensions;
using Trakmark.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<
    AuthenticationStateProvider,
    IdentityRevalidatingAuthenticationStateProvider
>();

builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddAppDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAppIdentity();
builder.Services.AddAppTelemetry(builder.Configuration);
builder.Services.AddScoped<ISaveCitiesBatchService, SaveCitiesBatchService>();
builder.Services.AddScoped<IRegisteredUserMappingService, RegisteredUserMappingService>();
builder.Services.AddScoped<IRegisteredUserLookupService, RegisteredUserLookupService>();
builder.Services.AddScoped<ICityQueryService, CityQueryService>();

var app = builder.Build();

await app.SeedRolesAsync();

app.UseAppMiddleware();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();

await app.RunAsync();
