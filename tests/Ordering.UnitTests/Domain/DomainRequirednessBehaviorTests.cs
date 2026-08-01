using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class DomainRequirednessBehaviorTests
{
    [TestMethod]
    public void Order_and_order_item_retain_their_required_domain_values()
    {
        var address = new Address("street", "city", "state", "country", "zip");
        var order = new Order(
            "user-id",
            "user-name",
            address,
            cardTypeId: 1,
            cardNumber: "4111111111111111",
            cardSecurityNumber: "123",
            cardHolderName: "holder",
            cardExpiration: DateTime.UtcNow.AddYears(1));

        order.AddOrderItem(42, "product", 12.50m, 1.50m, "picture", 2);

        Assert.AreSame(address, order.Address);
        Assert.AreEqual(new Address("street", "city", "state", "country", "zip"), order.Address);
        var item = order.OrderItems.Single();
        Assert.AreEqual("product", item.ProductName);
        Assert.AreEqual(42, item.ProductId);
        Assert.AreEqual(12.50m, item.UnitPrice);
        Assert.AreEqual(1.50m, item.Discount);
        Assert.AreEqual("picture", item.PictureUrl);
        Assert.AreEqual(2, item.Units);
    }

    [TestMethod]
    public void Buyer_retains_the_identity_and_name_validated_by_its_constructor()
    {
        var buyer = new Buyer("identity", "name");

        Assert.AreEqual("identity", buyer.IdentityGuid);
        Assert.AreEqual("name", buyer.Name);
    }

    [TestMethod]
    [DataRow("", "123", "holder", "cardNumber")]
    [DataRow("4111111111111111", "", "holder", "securityNumber")]
    [DataRow("4111111111111111", "123", "", "cardHolderName")]
    public void Payment_method_rejects_blank_card_values(
        string cardNumber,
        string securityNumber,
        string cardHolderName,
        string expectedMessage)
    {
        var exception = Assert.ThrowsExactly<OrderingDomainException>(() => new PaymentMethod(
            cardTypeId: 1,
            alias: "primary",
            cardNumber,
            securityNumber,
            cardHolderName,
            expiration: DateTime.UtcNow.AddYears(1)));

        Assert.AreEqual(expectedMessage, exception.Message);
    }
}
