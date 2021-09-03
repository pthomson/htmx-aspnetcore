using System.Linq;
using Htmx;
using JetSwagStore.Models;
using JetSwagStore.Models.Cart;
using JetSwagStore.Models.Extensions;
using JetSwagStore.Models.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JetSwagStore.Controllers;

[Route("[controller]")]
public class CartController : Controller
{
    private readonly StoreDbContext db;
    private readonly CurrentShoppingCart currentShoppingCart;

    public CartController(StoreDbContext db, CurrentShoppingCart currentShoppingCart)
    {
        this.db = db;
        this.currentShoppingCart = currentShoppingCart;
    }
    
    // GET
    public IActionResult Show()
    {
        return View();
    }

    [HttpPost, Route("")]
    public async Task<IActionResult> Update([FromForm] UpdateCartRequest input)
    {
        var product = await db.Products
            .Include(p => p.Options.Where(o => o.Id == input.ProductOptionId))
            .FirstOrDefaultAsync(p => p.Id == input.ProductId);

        var option = product?.Options.FirstOrDefault();

        if (product is null) {
            return BadRequest("Product or option was not found.");
        }

        var cart = await db.FindShoppingCart(currentShoppingCart.Id);

        if (input.Remove)
        {
            input.Quantity = 0;
        }

        if (cart is not null)
        {
            var item = cart.Items
                .Where(i => i.ProductId == product.Id)
                .If(option != null, q => q.Where(p => p.ProductOptionId == option?.Id))
                .FirstOrDefault();

            if (item == null)
            {
                item = new ShoppingCartItem
                {
                    Product = product,
                    Option = option
                };
                cart.Items.Add(item);
            }

            item.Quantity = input.Quantity;

            if (input.Quantity == 0)
            {
                cart.Items.Remove(item);
            }

            await db.SaveChangesAsync();
        }

        return Request.IsHtmx()
            ? PartialView("_Product", new ProductViewModel {
                Info = product,
                ShouldRenderCartButton = true
            })
            : RedirectToAction("Index", "Home");
    }
}
