namespace Sstv.Host;

public static class MinimalEndpoints
{
    public static IEndpointRouteBuilder MapExampleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/minimal-api-example-1", () =>
        {
            // using constants
            var x = new MyException(DomainErrorCodes.NOT_ENOUGH_MONEY)
                .WithDetailedMessage("DetailedError");

            throw x;
        });

        app.MapGet("/minimal-api-example-2", () =>
        {
            // using enums
            throw ErrorCodes.NotEnoughMoney.AsException()
                .WithDetailedMessage("You want 500, but your account balance is 300.");
        });

        app.MapGet("/minimal-api-example-3", () =>
        {
            // using constants without configured error code
            var x = new MyException("NOT-EXISTING-CODE-123")
                .WithDetailedMessage("DetailedError");

            throw x;
        });

        // exception handled in problem details middleware
        app.MapGet("/minimal-api-example-4", () =>
        {
            throw new InvalidOperationException("DetailedError");
        });

        // Explicit results cannot be converted to ProblemDetails by default, you should do it by your hand
        app.MapGet("/minimal-api-example-5", () => Results.BadRequest("DetailedError"));

        // this returns ProblemDetails
        app.MapGet("/minimal-api-example-5-pb", () => Results.Problem("DetailedError", statusCode: StatusCodes.Status400BadRequest));

        return app;
    }
}