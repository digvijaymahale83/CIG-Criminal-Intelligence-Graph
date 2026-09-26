using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<InvestigationBackgroundWorker>();

var host = builder.Build();
host.Run();
