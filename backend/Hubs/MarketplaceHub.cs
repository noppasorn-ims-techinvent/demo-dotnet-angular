using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs;

[Authorize]
public class MarketplaceHub : Hub
{
    public const string Path = "/hubs/marketplace";

    public const string AdminsGroup = "admins";

    public static string BuyerGroupName(int buyerId) => $"buyer-{buyerId}";

    public static string SellerGroupName(int sellerUserId) => $"seller-{sellerUserId}";

    /// <summary>รองรับทั้ง claim แบบ NameIdentifier และ JWT มาตรฐาน (sub)</summary>
    private static int? TryGetUserId(ClaimsPrincipal? user)
    {
        var v = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user?.FindFirstValue("sub");
        return int.TryParse(v, out var id) ? id : null;
    }

    public async Task JoinBuyerGroup(int buyerId)
    {
        var userId = TryGetUserId(Context.User);
        if (userId is null || userId != buyerId)
        {
            throw new HubException("You can only join your own buyer group.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, BuyerGroupName(buyerId));
    }

    public async Task JoinSellerGroup(int sellerId)
    {
        var userId = TryGetUserId(Context.User);
        if (userId is null || userId != sellerId)
        {
            throw new HubException("You can only join your own seller group.");
        }

        if (!Context.User!.IsInRole("Seller"))
        {
            throw new HubException("Seller role required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SellerGroupName(sellerId));
    }

    public async Task JoinAdminGroup()
    {
        if (!Context.User!.IsInRole("Admin"))
        {
            throw new HubException("Admin only.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
    }
}
