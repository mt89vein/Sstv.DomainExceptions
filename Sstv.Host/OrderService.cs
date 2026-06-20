using Sstv.Domain.Sample;

namespace Sstv.Host;

public interface IOrderService
{
    public void ProcessOrder(string orderId);
}


public sealed class OrderService : IOrderService
{
    public void ProcessOrder(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            throw new MyException("INTERFACE_ONE");
        }

        ValidateOrder(orderId);
        CheckInventory(orderId);
        ProcessPayment(orderId);
        ProcessItem(orderId);
        ProcessItem(orderId, 42);
    }

    public void ProcessItem<T>(T item)
    {
        if (item is null)
        {
            throw new MyException("GENERIC_NULL");
        }
    }

    public void ProcessItem<T, TExtra>(T item, TExtra extra)
    {
        throw ErrorCodes.Default.ToException();
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

        SomeOtherMethod();
        ProcessPayment(1);
    }

    public void ProcessPayment(int orderId)
    {
        var balance = 100m;
        var amount = 500m;

        if (balance < amount)
        {
            throw ErrorCodes.WhateverElse.ToException()
                .WithDetailedMessage($"Insufficient balance. Required: {amount}, Available: {balance}");
        }

        SomeOtherMethod();
    }

    public void SomeOtherMethod()
    {
        throw ErrorCodes.Default.ToException();
    }
}

public sealed class OrderAlternativeService : IOrderService
{
    public void ProcessOrder(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            throw new MyException("INTERFACE_TWO");
        }
    }
}