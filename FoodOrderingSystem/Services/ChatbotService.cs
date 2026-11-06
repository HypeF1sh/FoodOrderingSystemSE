// PB27 (Jordan) - AI chatbot: Gemini plus read-only restaurant tools
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// PB27 AI chatbot. Gemini writes the answers, but it only knows the restaurant through the "tools" below:
// when it needs a fact (a price, today's hours) it asks for a tool, this service runs it through our
// normal services and sends the result back, and Gemini carries on. So the bot cannot invent prices,
// and it can only see what our services already allow - read-only, and only the customer's own data.
public class ChatbotService : IChatbotService
{
    private const int MaxHistory = 12;      // older messages are dropped to keep requests small and fast
    private const int MaxToolRounds = 4;    // look-up rounds before the bot must answer with what it has
    private const int MaxMenuItems = 80;
    private const int MaxRecentRows = 5;
    // The free-tier limit resets every minute, so Google never asks for more than about 60 s.
    // The customer sees a countdown and can press Stop; longer than this, we just say "busy".
    private static readonly TimeSpan MaxRetryWait = TimeSpan.FromSeconds(60);

    private const string NotSetUpMessage =
        "The chatbot is not set up on this computer yet. You can still browse the Menu or book a table.";

    private readonly IGeminiClient _gemini;
    private readonly IMenuService _menuService;
    private readonly IOperatingHoursService _hoursService;
    private readonly IPromotionService _promotionService;
    private readonly IAnnouncementService _announcementService;
    private readonly IRestaurantSettingService _settingService;
    private readonly IReservationService _reservationService;
    private readonly IOrderService _orderService;
    private readonly ILogger<ChatbotService> _logger;

    // Every fact comes from another domain's service, never straight from the database
    public ChatbotService(IGeminiClient gemini, IMenuService menuService, IOperatingHoursService hoursService,
        IPromotionService promotionService, IAnnouncementService announcementService,
        IRestaurantSettingService settingService, IReservationService reservationService,
        IOrderService orderService, ILogger<ChatbotService> logger)
    {
        _gemini = gemini;
        _menuService = menuService;
        _hoursService = hoursService;
        _promotionService = promotionService;
        _announcementService = announcementService;
        _settingService = settingService;
        _reservationService = reservationService;
        _orderService = orderService;
        _logger = logger;
    }

    public bool IsAvailable => _gemini.IsConfigured;

    public async IAsyncEnumerable<ChatStreamEvent> AnswerAsync(List<ChatMessageInput> messages, string? customerId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. Guard clauses: no key on this PC, or the conversation does not end with a question
        if (!IsAvailable)
        {
            yield return ChatStreamEvent.Error(NotSetUpMessage);
            yield break;
        }

        if (messages.Count == 0 || messages[^1].Role != "user")
        {
            yield return ChatStreamEvent.Error("Please type a question.");
            yield break;
        }

        // 2. The recent conversation in Gemini's format ("assistant" is called "model" there)
        var contents = new JsonArray();
        foreach (var message in messages.TakeLast(MaxHistory).SkipWhile(m => m.Role != "user"))
        {
            contents.Add(TextContent(message.Role == "user" ? "user" : "model", message.Text));
        }

        yield return ChatStreamEvent.Status("Thinking...");

        var retried = false;

        // 3. Ask Gemini; if it asks for tools, run them and ask again. The last round offers no tools,
        //    so the bot has to answer with what it has found.
        for (var round = 0; round <= MaxToolRounds; round++)
        {
            var request = BuildRequest(contents, offerTools: round < MaxToolRounds);
            var modelParts = new JsonArray();
            var toolCalls = new List<JsonObject>();
            var wroteText = false;
            GeminiException? failure = null;

            await using (var chunks = _gemini.StreamAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken))
            {
                while (true)
                {
                    // C# does not allow "yield return" inside a try with a catch, so the catch only records the failure
                    JsonObject chunk;
                    try
                    {
                        if (!await chunks.MoveNextAsync()) break;
                        chunk = chunks.Current;
                    }
                    catch (GeminiException ex)
                    {
                        failure = ex;
                        break;
                    }

                    foreach (var part in PartsOf(chunk))
                    {
                        // Keep every part exactly as received: Gemini needs its own parts (with their hidden
                        // "thought signatures") sent back unchanged when we return the tool results
                        modelParts.Add(part.DeepClone());

                        if (part["functionCall"] is JsonObject call)
                        {
                            toolCalls.Add(call);
                        }
                        else if (part["text"] is JsonValue text && part["thought"] is null)
                        {
                            var piece = text.GetValue<string>();
                            if (piece.Length == 0) continue;

                            wroteText = true;
                            yield return ChatStreamEvent.Chunk(piece);
                        }
                    }
                }
            }

            if (failure != null)
            {
                // Free-tier limit hit: if Google asks for a short wait and nothing was shown yet this round,
                // wait (with a visible countdown message) and try the same round once more
                if (!retried && !wroteText && modelParts.Count == 0
                    && failure.RetryAfter is { } wait && wait <= MaxRetryWait)
                {
                    retried = true;
                    yield return ChatStreamEvent.Status($"Busy right now, trying again in {(int)wait.TotalSeconds} s...");
                    await Task.Delay(wait + TimeSpan.FromSeconds(1), cancellationToken);
                    round--;
                    continue;
                }

                yield return ChatStreamEvent.Error(failure.Message);
                yield break;
            }

            // 4. No tool requested: the answer is complete
            if (toolCalls.Count == 0)
            {
                if (!wroteText)
                {
                    yield return ChatStreamEvent.Error("Sorry, I could not answer that. Could you ask it another way?");
                }

                yield return ChatStreamEvent.Done();
                yield break;
            }

            // 5. Run each requested tool, telling the customer what the bot is doing (the "progress" in PB27)
            contents.Add(new JsonObject { ["role"] = "model", ["parts"] = modelParts });

            var results = new JsonArray();
            foreach (var call in toolCalls)
            {
                var name = call["name"]?.GetValue<string>() ?? string.Empty;
                yield return ChatStreamEvent.Status(DescribeTool(name));

                var response = new JsonObject
                {
                    ["name"] = name,
                    ["response"] = new JsonObject { ["result"] = await RunToolAsync(name, call["args"] as JsonObject, customerId) }
                };
                if (call["id"] is JsonNode callId) response["id"] = callId.DeepClone();

                results.Add(new JsonObject { ["functionResponse"] = response });
            }

            contents.Add(new JsonObject { ["role"] = "user", ["parts"] = results });
            yield return ChatStreamEvent.Status("Writing the answer...");
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Request building
    // ---------------------------------------------------------------------------------------------------------

    private static JsonObject BuildRequest(JsonArray contents, bool offerTools)
    {
        var request = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = BuildInstructions() })
            },
            // A copy, because a JsonNode can only belong to one parent and contents is reused next round
            ["contents"] = contents.DeepClone()
        };

        if (offerTools)
        {
            request["tools"] = new JsonArray(new JsonObject { ["functionDeclarations"] = ToolDeclarations() });
        }

        return request;
    }

    // The bot's rules. Today's date is included so "today" and "tomorrow" mean the right days.
    private static string BuildInstructions()
    {
        var now = DateTime.Now;
        return $"""
            You are the friendly online assistant of Rumah Rasa, a Malaysian home-style restaurant in Penang.
            Today is {now:dddd} {now:dd/MM/yyyy}, and the time is {now:hh:mm tt} (Malaysia time).

            Rules:
            - Answer only questions about Rumah Rasa: the menu, prices, opening hours, offers, location,
              bookings, ordering and paying. Politely decline anything else in one sentence.
            - Use the tools for every fact. Never guess a dish, price, time or offer that a tool did not give you.
              If a tool has no answer, say you are not sure and suggest asking the staff.
            - You cannot book, order, cancel or change anything. Tell the customer which page to use instead:
              /Menu to see dishes and order, /Reservations/Create to book a table,
              /Reservations for their bookings, /Orders for their orders.
            - To order at the restaurant, the customer scans the QR code on their table. From outside they need
              a confirmed booking, then they can pre-order.
            - Prices are in Malaysian ringgit, written like RM 12.50. Dates like 15/11/2026, times like 07:30 PM.
            - Keep answers short: two to four sentences, or a short list. Plain text only, no tables.
            - Reply in the language the customer writes in (English, Malay or Chinese).
            """;
    }

    // What Gemini is told about each tool. Gemini decides from the descriptions when to use one.
    private static JsonArray ToolDeclarations() => (JsonArray)JsonNode.Parse("""
        [
          {
            "name": "get_menu",
            "description": "Dishes on the menu with category, price, description and whether they are available today (not sold out).",
            "parameters": {
              "type": "object",
              "properties": {
                "search": { "type": "string", "description": "Optional word to filter dishes by name or description, e.g. 'chicken'. Leave out for the full menu." }
              }
            }
          },
          {
            "name": "get_opening_hours",
            "description": "The restaurant's opening hours for each day of the week."
          },
          {
            "name": "get_offers",
            "description": "Current promotions (discounts) and news announcements."
          },
          {
            "name": "get_restaurant_info",
            "description": "The restaurant's address and phone number."
          },
          {
            "name": "get_my_bookings",
            "description": "The logged-in customer's own table bookings: date, time, party size, table and status."
          },
          {
            "name": "get_my_orders",
            "description": "The logged-in customer's most recent orders: dishes, total, order status and whether it is paid."
          }
        ]
        """)!;   // a fixed, valid JSON literal, so Parse cannot return null

    private static JsonObject TextContent(string role, string text) => new()
    {
        ["role"] = role,
        ["parts"] = new JsonArray(new JsonObject { ["text"] = text })
    };

    // A streamed chunk looks like { candidates: [ { content: { parts: [ ... ] } } ] }
    private static IEnumerable<JsonObject> PartsOf(JsonObject chunk)
    {
        if (chunk["candidates"]?[0]?["content"]?["parts"] is not JsonArray parts) yield break;

        foreach (var part in parts)
        {
            if (part is JsonObject partObject) yield return partObject;
        }
    }

    private static string DescribeTool(string name) => name switch
    {
        "get_menu" => "Checking the menu...",
        "get_opening_hours" => "Checking opening hours...",
        "get_offers" => "Checking current offers...",
        "get_restaurant_info" => "Looking up our address...",
        "get_my_bookings" => "Looking up your bookings...",
        "get_my_orders" => "Looking up your orders...",
        _ => "Looking that up..."
    };

    // ---------------------------------------------------------------------------------------------------------
    // Tools: each one reads through an existing service and returns plain JSON for Gemini
    // ---------------------------------------------------------------------------------------------------------

    private async Task<JsonNode> RunToolAsync(string name, JsonObject? args, string? customerId)
    {
        try
        {
            return name switch
            {
                "get_menu" => await GetMenuAsync(args?["search"]?.GetValue<string>()),
                "get_opening_hours" => await GetOpeningHoursAsync(),
                "get_offers" => await GetOffersAsync(),
                "get_restaurant_info" => await GetRestaurantInfoAsync(),
                "get_my_bookings" => await GetMyBookingsAsync(customerId),
                "get_my_orders" => await GetMyOrdersAsync(customerId),
                _ => ToolError("Unknown tool.")
            };
        }
        catch (Exception ex)
        {
            // A failed look-up should not end the chat: Gemini is told, and says it is not sure
            _logger.LogError(ex, "Chatbot tool {Tool} failed", name);
            return ToolError("This information is not available right now.");
        }
    }

    private async Task<JsonNode> GetMenuAsync(string? search)
    {
        var menu = await _menuService.GetMenuForBrowseAsync(null, string.IsNullOrWhiteSpace(search) ? null : search);

        var dishes = new JsonArray();
        foreach (var section in menu.Sections)
        {
            foreach (var item in section.Items.Take(MaxMenuItems - dishes.Count))
            {
                dishes.Add(new JsonObject
                {
                    ["name"] = item.Name,
                    ["category"] = section.Name,
                    ["price"] = $"RM {item.Price:0.00}",
                    ["description"] = item.Description,
                    ["availableToday"] = item.IsAvailable
                });
            }
        }

        return new JsonObject { ["dishes"] = dishes, ["count"] = dishes.Count };
    }

    private async Task<JsonNode> GetOpeningHoursAsync()
    {
        var days = new JsonArray();
        foreach (var day in await _hoursService.GetWeekAsync())
        {
            days.Add(new JsonObject
            {
                ["day"] = day.DayName,
                ["hours"] = day.IsClosed ? "Closed" : $"{day.OpenTime:hh:mm tt} - {day.CloseTime:hh:mm tt}"
            });
        }

        return new JsonObject { ["week"] = days };
    }

    private async Task<JsonNode> GetOffersAsync()
    {
        var promotions = new JsonArray();
        foreach (var promotion in await _promotionService.GetActiveAsync())
        {
            promotions.Add(new JsonObject
            {
                ["title"] = promotion.Title,
                ["description"] = promotion.Description,
                ["discount"] = $"{promotion.DiscountPercent:0.##}% off",
                ["until"] = promotion.EndDate.ToString("dd/MM/yyyy")
            });
        }

        var news = new JsonArray();
        foreach (var announcement in await _announcementService.GetActiveAsync())
        {
            news.Add(new JsonObject { ["title"] = announcement.Title, ["text"] = announcement.Body });
        }

        return new JsonObject { ["promotions"] = promotions, ["announcements"] = news };
    }

    private async Task<JsonNode> GetRestaurantInfoAsync()
    {
        // Address and Phone are optional settings an admin adds under Configurations > Settings
        var settings = await _settingService.GetAllAsync();
        string? Find(string key) => settings.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

        return new JsonObject
        {
            ["name"] = "Rumah Rasa",
            ["address"] = Find("Address") ?? "Not listed yet - please ask the staff.",
            ["phone"] = Find("Phone") ?? "Not listed yet."
        };
    }

    private async Task<JsonNode> GetMyBookingsAsync(string? customerId)
    {
        if (customerId == null) return ToolError("The customer is not logged in. Ask them to log in to see their bookings.");

        var bookings = new JsonArray();
        var reservations = (await _reservationService.GetCustomerReservationsAsync(customerId))
            .OrderByDescending(r => r.ReservationDate).ThenByDescending(r => r.StartTime)
            .Take(MaxRecentRows * 2);

        foreach (var reservation in reservations)
        {
            bookings.Add(new JsonObject
            {
                ["date"] = reservation.ReservationDate.ToString("dd/MM/yyyy"),
                ["time"] = $"{reservation.StartTime:hh:mm tt} - {reservation.EndTime:hh:mm tt}",
                ["partySize"] = reservation.PartySize,
                ["table"] = $"{reservation.TableNumber} ({reservation.DiningAreaName})",
                ["status"] = reservation.Status
            });
        }

        return new JsonObject { ["bookings"] = bookings };
    }

    private async Task<JsonNode> GetMyOrdersAsync(string? customerId)
    {
        if (customerId == null) return ToolError("The customer is not logged in. Ask them to log in to see their orders.");

        var rows = new JsonArray();
        var orders = (await _orderService.GetCustomerOrdersAsync(customerId))
            .OrderByDescending(o => o.CreatedAt)
            .Take(MaxRecentRows);

        foreach (var order in orders)
        {
            rows.Add(new JsonObject
            {
                ["orderNumber"] = order.OrderId,
                ["placed"] = order.CreatedAt.ToString("dd/MM/yyyy hh:mm tt"),
                ["status"] = order.Status.ToString(),
                ["total"] = $"RM {order.TotalAmount:0.00}",
                ["paid"] = order.Payment.IsPaid,
                ["dishes"] = string.Join(", ", order.Lines.Select(l => $"{l.Quantity} x {l.Name}"))
            });
        }

        return new JsonObject { ["orders"] = rows };
    }

    private static JsonObject ToolError(string message) => new() { ["error"] = message };
}
