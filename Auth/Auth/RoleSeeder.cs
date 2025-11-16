using Microsoft.AspNetCore.Identity;

namespace Auth;

public class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<RoleSeeder>>();

        string[] roles = [nameof(Roles.Admin), nameof(Roles.Customer)];

        foreach (var roleName in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (roleExists)
            {
                continue;
            }
            var role = new AppRole { Name = roleName };
            var result = await roleManager.CreateAsync(role);
                
            if (result.Succeeded)
                logger.LogInformation($"Role '{roleName}' created successfully.");
            else
                logger.LogError("Error creating role '{RoleName}': {Join}", roleName, string.Join(", ", result.Errors));
        }
    }
}
