using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class OrderingModelMappingTests
{
    private OrderingContext _context;
    private IModel _model;

    [TestInitialize]
    public void Initialize()
    {
        var options = new DbContextOptionsBuilder<OrderingContext>()
            .UseNpgsql("Host=localhost;Database=ordering_characterization")
            .Options;
        _context = new OrderingContext(options);
        _model = _context.Model;
    }

    [TestCleanup]
    public void Cleanup() => _context.Dispose();

    [TestMethod]
    public void Order_and_owned_address_mapping_is_characterized()
    {
        var order = Entity<Order>();
        AssertEntity(order, "orders", "Id");
        AssertProperty(order, nameof(Order.BuyerId), true, "BuyerId");
        AssertProperty(order, nameof(Order.PaymentId), true, "PaymentMethodId");
        AssertProperty(order, nameof(Order.OrderStatus), false, "OrderStatus");
        Assert.AreEqual(30, order.FindProperty(nameof(Order.OrderStatus)).GetMaxLength());
        Assert.AreEqual(
            typeof(string),
            order.FindProperty(nameof(Order.OrderStatus)).GetTypeMapping().Converter.ProviderClrType);

        var address = Entity<Address>();
        Assert.IsTrue(address.IsOwned());
        AssertEntity(address, "orders", "OrderId");
        AssertProperty(address, nameof(Address.Street), true, "Address_Street");
        AssertProperty(address, nameof(Address.City), true, "Address_City");
        AssertProperty(address, nameof(Address.State), true, "Address_State");
        AssertProperty(address, nameof(Address.Country), true, "Address_Country");
        AssertProperty(address, nameof(Address.ZipCode), true, "Address_ZipCode");

        var ownership = address.FindOwnership();
        Assert.IsNotNull(ownership);
        Assert.AreSame(order, ownership.PrincipalEntityType);
        Assert.AreEqual("OrderId", ownership.Properties.Single().Name);
        Assert.IsTrue(ownership.IsRequired);
        Assert.IsTrue(ownership.IsRequiredDependent);

        var addressNavigation = order.FindNavigation(nameof(Order.Address));
        Assert.IsNotNull(addressNavigation);
        Assert.AreSame(address, addressNavigation.TargetEntityType);
        Assert.IsTrue(addressNavigation.ForeignKey.IsRequiredDependent);
    }

    [TestMethod]
    public void Order_item_mapping_is_characterized()
    {
        var orderItem = Entity<OrderItem>();
        AssertEntity(orderItem, "orderItems", "Id");
        AssertProperty(orderItem, nameof(OrderItem.ProductName), false, "ProductName");
        AssertProperty(orderItem, nameof(OrderItem.PictureUrl), true, "PictureUrl");
        AssertProperty(orderItem, nameof(OrderItem.UnitPrice), false, "UnitPrice");
        AssertProperty(orderItem, nameof(OrderItem.Discount), false, "Discount");
        AssertProperty(orderItem, nameof(OrderItem.Units), false, "Units");
        AssertProperty(orderItem, nameof(OrderItem.ProductId), false, "ProductId");
        AssertProperty(orderItem, "OrderId", false, "OrderId");

        var orderForeignKey = orderItem.GetForeignKeys().Single(
            key => key.PrincipalEntityType.ClrType == typeof(Order));
        Assert.AreEqual("OrderId", orderForeignKey.Properties.Single().Name);
        Assert.AreEqual(DeleteBehavior.Cascade, orderForeignKey.DeleteBehavior);
        Assert.IsTrue(orderForeignKey.IsRequired);
    }

    [TestMethod]
    public void Buyer_and_payment_method_mapping_is_characterized()
    {
        var buyer = Entity<Buyer>();
        AssertEntity(buyer, "buyers", "Id");
        AssertProperty(buyer, nameof(Buyer.IdentityGuid), false, "IdentityGuid");
        Assert.AreEqual(200, buyer.FindProperty(nameof(Buyer.IdentityGuid)).GetMaxLength());
        var identityIndex = buyer.GetIndexes().Single(
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Buyer.IdentityGuid)]));
        Assert.IsTrue(identityIndex.IsUnique);

        var payment = Entity<PaymentMethod>();
        AssertEntity(payment, "paymentmethods", "Id");
        AssertProperty(payment, "_alias", false, "Alias");
        AssertProperty(payment, "_cardNumber", false, "CardNumber");
        AssertProperty(payment, "_cardHolderName", false, "CardHolderName");
        Assert.IsNull(payment.FindProperty("_securityNumber"));
        AssertProperty(payment, "_expiration", false, "Expiration");
        AssertProperty(payment, "_cardTypeId", false, "CardTypeId");
        AssertProperty(payment, "BuyerId", false, "BuyerId");
        Assert.AreEqual(200, payment.FindProperty("_alias").GetMaxLength());
        Assert.AreEqual(25, payment.FindProperty("_cardNumber").GetMaxLength());
        Assert.AreEqual(200, payment.FindProperty("_cardHolderName").GetMaxLength());

        var buyerForeignKey = payment.GetForeignKeys().Single(
            key => key.PrincipalEntityType.ClrType == typeof(Buyer));
        Assert.AreEqual("BuyerId", buyerForeignKey.Properties.Single().Name);
        Assert.AreEqual(DeleteBehavior.Cascade, buyerForeignKey.DeleteBehavior);
        Assert.IsTrue(buyerForeignKey.IsRequired);

        var cardTypeForeignKey = payment.GetForeignKeys().Single(
            key => key.PrincipalEntityType.ClrType == typeof(CardType));
        Assert.AreEqual("_cardTypeId", cardTypeForeignKey.Properties.Single().Name);
        Assert.AreEqual(DeleteBehavior.Cascade, cardTypeForeignKey.DeleteBehavior);
        Assert.IsTrue(cardTypeForeignKey.IsRequired);
    }

    [TestMethod]
    public void Optional_order_relationships_and_collection_relationships_are_characterized()
    {
        var order = Entity<Order>();
        var buyerForeignKey = order.GetForeignKeys().Single(
            key => key.PrincipalEntityType.ClrType == typeof(Buyer));
        Assert.AreEqual(nameof(Order.BuyerId), buyerForeignKey.Properties.Single().Name);
        Assert.AreEqual(DeleteBehavior.ClientSetNull, buyerForeignKey.DeleteBehavior);
        Assert.IsFalse(buyerForeignKey.IsRequired);

        var paymentForeignKey = order.GetForeignKeys().Single(
            key => key.PrincipalEntityType.ClrType == typeof(PaymentMethod));
        Assert.AreEqual(nameof(Order.PaymentId), paymentForeignKey.Properties.Single().Name);
        Assert.AreEqual(DeleteBehavior.Restrict, paymentForeignKey.DeleteBehavior);
        Assert.IsFalse(paymentForeignKey.IsRequired);

        var orderItems = order.FindNavigation(nameof(Order.OrderItems));
        Assert.IsNotNull(orderItems);
        Assert.IsTrue(orderItems.IsCollection);
        Assert.AreEqual(DeleteBehavior.Cascade, orderItems.ForeignKey.DeleteBehavior);

        var paymentMethods = Entity<Buyer>().FindNavigation(nameof(Buyer.PaymentMethods));
        Assert.IsNotNull(paymentMethods);
        Assert.IsTrue(paymentMethods.IsCollection);
        Assert.AreEqual(DeleteBehavior.Cascade, paymentMethods.ForeignKey.DeleteBehavior);
    }

    private IEntityType Entity<T>()
    {
        var entity = _model.FindEntityType(typeof(T));
        Assert.IsNotNull(entity);
        return entity;
    }

    private static void AssertEntity(IEntityType entity, string tableName, params string[] keyProperties)
    {
        Assert.AreEqual(tableName, entity.GetTableName());
        Assert.AreEqual("ordering", entity.GetSchema());
        var key = entity.FindPrimaryKey();
        Assert.IsNotNull(key);
        CollectionAssert.AreEqual(keyProperties, key.Properties.Select(property => property.Name).ToArray());
    }

    private static void AssertProperty(
        IEntityType entity,
        string propertyName,
        bool isNullable,
        string columnName)
    {
        var property = entity.FindProperty(propertyName);
        Assert.IsNotNull(property);
        Assert.AreEqual(isNullable, property.IsNullable, propertyName);
        var table = StoreObjectIdentifier.Table(entity.GetTableName(), entity.GetSchema());
        Assert.AreEqual(columnName, property.GetColumnName(table), propertyName);
    }
}
