Generating and applying EF Core migrations for `UserDataContext`

1. Install dotnet-ef tool (if not present):
   dotnet tool install --global dotnet-ef

2. From repository root, run:
   dotnet ef migrations add InitialCreate --context TWCF_BibleApp.Persistence.Contexts.UserDataContext --output-dir Migrations

   This will scaffold migration files into the `Migrations` folder. If you've added the included example `Migrations/20260302_InitialCreate.cs`, skip this step.

3. Apply migrations at runtime (already done in `MauiProgram`): the code calls `userDb.Database.Migrate()` on startup.

Notes
- `MigrationsDbContextFactory` is provided for design-time migrations.
- Ensure `Microsoft.EntityFrameworkCore.Design` is referenced in the csproj when running `dotnet ef`.
  Add to csproj:
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />

- To generate SQL script instead of applying: 
    dotnet ef migrations script --context TWCF_BibleApp.Persistence.Contexts.UserDataContext

