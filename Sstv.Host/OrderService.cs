namespace Sstv.Host;

public class OrderService
{
    public void ProcessOrder(string orderId)
    {
        ValidateOrder(orderId);
        CheckInventory(orderId);
        ProcessPayment(orderId);
    }

    public void ValidateOrder(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            throw new FirstException(ErrorCodes.InvalidData)
                .WithDetailedMessage("Order ID is required");
        }

        if (orderId.Length < 5)
        {
            throw ErrorCodes.SomethingBadHappen.ToException()
                .WithDetailedMessage("Order ID is too short");
        }
    }

    public void CheckInventory(string orderId)
    {
        var hasInventory = false;
        if (!hasInventory)
        {
            throw ErrorCodes.NotEnoughMoney.ToException()
                .WithDetailedMessage("Item is out of stock");
        }
    }

    public void ProcessPayment(string orderId)
    {
        var balance = 100m;
        var amount = 500m;

        if (balance < amount)
        {
            throw ErrorCodes.NotEnoughMoney.ToException()
                .WithDetailedMessage($"Insufficient balance. Required: {amount}, Available: {balance}");
        }

        NonCollectedMethod();
    }

    public void NonCollectedMethod()
    {
        throw ErrorCodes.Default.ToException();
    }
}