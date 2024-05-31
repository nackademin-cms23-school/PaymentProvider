using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentProvider.Models;
using Stripe.Checkout;

namespace PaymentProvider.Functions;

public class Checkout
{
    private readonly ILogger<Checkout> _logger;

    public Checkout(ILogger<Checkout> logger)
    {
        _logger = logger;
    }

    [Function("Checkout")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "create-checkout-session")] HttpRequestData req)
    {
        var response = req.CreateResponse();
        try
        {
            var domain = "http://localhost:4242";

            var orderRequest = await UnpackOrderRequest(req);

            if (orderRequest != null)
            {
                var lineItems = Convert_OrderRequest_To_SessionLineItemOptions(orderRequest);

                if (lineItems != null)
                {
                    var options = new SessionCreateOptions
                    {
                        BillingAddressCollection = "auto",
                        ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                        {
                            AllowedCountries = new List<string>
                            {
                                "US",
                                "CA",
                                "SE"
                            }
                        },
                        LineItems = lineItems,
                        Mode = "payment",
                        SuccessUrl = domain + "/success.html",
                        CancelUrl = domain + "/cancel.html",
                    };

                    var service = new SessionService();
                    Session session = await service.CreateAsync(options);


                    response.Headers.Add("Location", session.Url);

                    response.StatusCode = System.Net.HttpStatusCode.OK;

                    return response;
                }
            }


        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : Checkout.Run() :: {ex.Message}");
        }
        response.StatusCode = System.Net.HttpStatusCode.BadRequest;
        return response;
    }

    public async Task<OrderRequest> UnpackOrderRequest(HttpRequestData req)
    {
        try
        {
            var json = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(json))
            {
                var orderRequest = JsonConvert.DeserializeObject<OrderRequest>(json);
                if (orderRequest != null)
                {
                    return orderRequest;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : Checkout.UnpackOrderModel() :: {ex.Message}");
        }
        return null!;
    }

    public List<SessionLineItemOptions> Convert_OrderRequest_To_SessionLineItemOptions(OrderRequest orderRequest)
    {
        try
        {
            var items = new List<SessionLineItemOptions>();

            orderRequest.LineItems.ForEach(line =>
            {
                items.Add(new SessionLineItemOptions
                {
                    Price = line.priceId,
                    Quantity = line.quantity,
                });
            });

            if (items.Count > 0)
            {
                return items;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : Checkout.UnpackOrderModel() :: {ex.Message}");
        }
        return null!;
    }
}
