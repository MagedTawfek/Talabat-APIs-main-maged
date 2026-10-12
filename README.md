### Talabat API Project

### Overview
A scalable e-commerce platform built with modern architectural patterns and best practices. This platform leverages Entity Framework, LINQ, and follows Clean Architecture principles to provide a robust solution for e-commerce needs.

### Architecture
The project is structured into the following layers:

```
src/
├── Talabat.APIs/           # API controllers and presentation layer
├── Talabat.Core/           # Domain entities, interfaces, business logic
├── Talabat.Repository/     # Data access implementation
├── Talabat.Services/       # Business services implementation
```

### Project Structure Explanation
- **Talabat.APIs**: Contains API controllers, filters, and configuration
- **Talabat.Core**: Houses domain entities, interfaces, and core business logic
- **Talabat.Repository**: Implements data access patterns and database operations
- **Talabat.Services**: Contains business service implementations and external integrations

### Features
- **Onion Architecture**: Separation of concerns and maintainability.
- **Repository Pattern**: Abstraction of the data access layer and consistent interface for querying the database.
- **Unit of Work Pattern**: Management of the context and transaction of the Entity Framework.
- **Specification Pattern**: Building complex queries in a composable and maintainable way.
- **Database-backed baskets**: Basket data is stored in SQL Server alongside the other application data.

### Getting Started

#### Prerequisites
- .NET 10.0 SDK
- SQL Server
- Visual Studio 2022 or VS Code

#### Local Development
1. Start a local SQL Server instance. Configure `ConnectionStrings__DefaultConnection`, `ConnectionStrings__IdentityConnection`, `JWT__SecretKey` (at least 32 bytes), `JWT__ValidIssuer`, `JWT__ValidAudience`, `JWT__TokenVersion`, `JWT__AccessTokenLifeTimeMinutes` (1-15), and `JWT__RefreshTokenLifeTimeDays` (1-90) as user secrets or environment variables. The example below shows the expected settings; replace every placeholder with local values. Increment `JWT__TokenVersion` to invalidate every previously issued access token:
    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Database=TalabatDb;User=sa;Password=YourStrong!Passw0rd;",
        "IdentityConnection": "Server=localhost;Database=TalabatIdentity;User Id=sa;Password=YourStrong!Passw0rd;"
      },
      "Jwt": {
        "SecretKey": "<load-a-random-secret-from-user-secrets-or-environment>",
        "ValidIssuer": "https://localhost:7284/",
        "ValidAudience": "Talabat-Apis-Client",
        "TokenVersion": "2",
        "AccessTokenLifeTimeMinutes": 10,
        "RefreshTokenLifeTimeDays": 30
      }
    }
    ```

2. Configure the initial administrator credentials with User Secrets. A secure admin account must exist before the API starts; the API will create it on first startup and will never use a hard-coded admin password:
    ```bash
    dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com" --project Talabat.APIs
    dotnet user-secrets set "BootstrapAdmin:Password" "<a-strong-unique-password>" --project Talabat.APIs
    ```

3. Run the application:
    ```bash
    dotnet restore
    dotnet run --project Talabat.APIs
    ```

### API Documentation
- API documentation available at `https://localhost:7284/swagger/index.html`
- Protected endpoints require the JWT access token returned by `/api/account/login` or `/api/account/register`. In Swagger, click **Authorize** and enter the access token. Login, registration, token refresh, product browsing, delivery-method browsing, and error handling are public.
- New accounts are assigned the `Customer` role automatically. The initial roles are `Admin`, `Customer`, and `Driver`.
- The `Driver` role currently has no driver-specific API endpoints; drivers can use public catalog and authenticated account endpoints, while basket and order endpoints are Customer-only.
- Authorization matrix: product catalog and delivery methods are public; account profile, address, password, and logout endpoints require authentication; baskets and orders require `Customer` and are scoped to the signed-in user; all role endpoints and email-existence lookup require `Admin`.
- Each account has one role at a time. Changing a user's role revokes that user's refresh-token sessions. Existing access tokens expire within 10 minutes, after which the user must sign in again to receive the current role.
- Access tokens expire after 10 minutes and are checked against the user's Identity security stamp, so logout, password changes, and role changes invalidate them immediately. Refresh tokens are stored as hashes, rotate on refresh, and are revoked on logout, password change, or role change. Reuse of a rotated refresh token revokes that token family.
- Detailed endpoint documentation in the `Talabat.APIs` project
- Postman documentation available [here](https://documenter.getpostman.com/view/28646082/2sAY4yfghP)

### Development Workflow
1. Make changes to the code.
2. Build the solution with `dotnet build`.
3. Run the API with `dotnet run --project Talabat.APIs`.
4. Access the API at `https://localhost:7284/swagger/index.html`.

### Endpoints

**AccountController**
- `POST /api/account/login`: Login a user
- `POST /api/account/register`: Register a new user
- `GET /api/account`: Get the current user
- `GET /api/account/address`: Get the user's address
- `PUT /api/account/address`: Update the user's address
- `GET /api/account/emailexists`: Check if an email exists (Admin only)
- `PUT /api/account/update`: Update the user's information
- `PUT /api/account/updatepassword`: Update the user's password
- `POST /api/account/logout`: Revoke the user's refresh-token sessions

**RolesController**
- `GET /api/roles`: List roles and their permissions
- `GET /api/roles/{roleName}`: Get a role and its permissions
- `POST /api/roles`: Create a role with an optional `permissions` array (Admin only)
- `PUT /api/roles/{roleName}/permissions`: Add a permission using `{ "permission": "orders.read" }` (Admin only)
- `PUT /api/roles/users/{email}/role`: Set a user's role (Admin only)

Role permissions are included as `permission` claims in newly issued JWTs. Protect an endpoint with `[Authorize(Policy = "permission:orders.read")]` to require a permission claim for that action.

**BasketController**
- `GET /api/basket`: Get the signed-in customer's basket (no basket ID is supplied)
- `POST /api/basket`: Update the signed-in customer's basket with product IDs and quantities; basket ID and product details/prices are assigned by the server. Example body: `{ "items": [{ "productId": 1, "quantity": 2 }] }`
- `DELETE /api/basket`: Delete the signed-in customer's basket

**ErrorsController**
- `GET /errors/{code}`: Handle errors

**OrderController**
- `POST /api/order`: Create a new order using the signed-in customer's basket; send only a delivery method ID and shipping address. Basket ID and shipping cost come from server-side data.
- `GET /api/order`: Get orders for the current user
- `GET /api/order/{id}`: Get an order by ID
- `GET /api/order/deliveryMethods`: Get delivery methods

**ProductsController**
- `GET /api/products`: Get a list of products
- `GET /api/products/{id}`: Get a product by ID
- `GET /api/products/brands`: Get a list of product brands
- `GET /api/products/categories`: Get a list of product categories

### Testing
```bash
# Run unit tests
dotnet test
```

### Monitoring and Logging
- Health checks available at `/health`
- Application logs are written to the console.

### Mock Configuration Files

#### appsettings.json
For local development and testing, you can use the following mock configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TalabatDb;User Id=sa;Password=YourStrong!Passw0rd;",
    "IdentityConnection": "Server=localhost;Database=TalabatIdentity;User Id=sa;Password=YourStrong!Passw0rd;"
  },
  "ApiBaseUrl": "https://localhost:7284",
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "<load-a-random-secret-from-user-secrets-or-environment>",
    "ValidIssuer": "https://localhost:7284/",
    "ValidAudience": "Talabat-Apis-Client",
    "TokenVersion": "2",
    "AccessTokenLifeTimeMinutes": 10,
    "RefreshTokenLifeTimeDays": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "ASPNETCORE_ENVIRONMENT": "Development"
}
