using FastEndpoints;

namespace KeepImproving.API.Endpoints.Model.Example;

public class ExampleEndpoint : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("model/example/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        await Send.OkAsync(new()
        {
            Title = "It's working"
        }, ct);
    }
}
