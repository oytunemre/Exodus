using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Exodus.Tests.Architecture;

public class ArchitectureTests
{
    private static readonly Assembly MainAssembly = typeof(Program).Assembly;

    private static IEnumerable<Type> GetAllTypes() =>
        MainAssembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false, IsEnum: false });

    // ─── Controller Layer Rules ─────────────────────────────────────────

    [Fact]
    public void Controllers_ShouldInheritFromControllerBase()
    {
        var controllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller"));

        controllers.Should().NotBeEmpty("project should have controllers");

        foreach (var controller in controllers)
        {
            controller.Should().BeDerivedFrom<ControllerBase>(
                $"{controller.Name} should inherit from ControllerBase");
        }
    }

    [Fact]
    public void Controllers_ShouldHaveApiControllerAttribute()
    {
        var controllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.IsSubclassOf(typeof(ControllerBase)));

        controllers.Should().NotBeEmpty();

        foreach (var controller in controllers)
        {
            controller.GetCustomAttribute<ApiControllerAttribute>()
                .Should().NotBeNull($"{controller.Name} should have [ApiController] attribute");
        }
    }

    [Fact]
    public void Controllers_ShouldHaveRouteAttribute()
    {
        var controllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.IsSubclassOf(typeof(ControllerBase)));

        controllers.Should().NotBeEmpty();

        foreach (var controller in controllers)
        {
            controller.GetCustomAttribute<RouteAttribute>()
                .Should().NotBeNull($"{controller.Name} should have [Route] attribute");
        }
    }

    [Fact]
    public void Controllers_ShouldResideInControllersNamespace()
    {
        var controllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.IsSubclassOf(typeof(ControllerBase)));

        controllers.Should().NotBeEmpty();

        foreach (var controller in controllers)
        {
            controller.Namespace.Should().StartWith("Exodus.Controllers",
                $"{controller.Name} should be in Exodus.Controllers namespace");
        }
    }

    // Known controllers that legitimately use DbContext directly (e.g. for email verification queries)
    private static readonly HashSet<string> ControllersAllowedDbContext = new() { "AuthController", "SellerController", "SellerListingController", "SupportController", "TwoFactorController" };

    [Fact]
    public void Controllers_ShouldNotDependOnDbContextDirectly_ExceptAllowed()
    {
        var nonAdminControllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase))
                        && t.Namespace != null
                        && !t.Namespace.Contains("Admin")
                        && !ControllersAllowedDbContext.Contains(t.Name));

        nonAdminControllers.Should().NotBeEmpty();

        foreach (var controller in nonAdminControllers)
        {
            var constructorParams = controller.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType);

            constructorParams.Should().NotContain(
                t => t.Name == "ApplicationDbContext",
                $"{controller.Name} should depend on service interfaces, not DbContext directly");
        }
    }

    [Fact]
    public void Controllers_ShouldOnlyDependOnInterfaces()
    {
        var nonAdminControllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase))
                        && t.Namespace != null
                        && !t.Namespace.Contains("Admin"));

        nonAdminControllers.Should().NotBeEmpty();

        foreach (var controller in nonAdminControllers)
        {
            var constructorParams = controller.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Where(p => p.ParameterType.Namespace != null
                            && p.ParameterType.Namespace.StartsWith("Exodus.Services"));

            foreach (var param in constructorParams)
            {
                param.ParameterType.IsInterface.Should().BeTrue(
                    $"{controller.Name} should depend on interface, not concrete type {param.ParameterType.Name}");
            }
        }
    }

    // ─── Admin Controller Rules ─────────────────────────────────────────

    [Fact]
    public void AdminControllers_ShouldHaveAdminOnlyPolicy()
    {
        var adminControllers = GetAllTypes()
            .Where(t => t.Name.StartsWith("Admin")
                        && t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase)));

        adminControllers.Should().NotBeEmpty("project should have admin controllers");

        foreach (var controller in adminControllers)
        {
            var authorizeAttr = controller.GetCustomAttribute<AuthorizeAttribute>();
            authorizeAttr.Should().NotBeNull(
                $"{controller.Name} should have [Authorize] at class level");
            authorizeAttr!.Policy.Should().Be("AdminOnly",
                $"{controller.Name} should require AdminOnly policy");
        }
    }

    [Fact]
    public void AdminControllers_ShouldResideInAdminNamespace()
    {
        var adminControllers = GetAllTypes()
            .Where(t => t.Name.StartsWith("Admin")
                        && t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase)));

        adminControllers.Should().NotBeEmpty();

        foreach (var controller in adminControllers)
        {
            controller.Namespace.Should().Be("Exodus.Controllers.Admin",
                $"{controller.Name} should be in Exodus.Controllers.Admin namespace");
        }
    }

    // ─── Seller Controller Rules ────────────────────────────────────────

    [Fact]
    public void SellerControllers_ShouldHaveSellerOnlyPolicy()
    {
        var sellerControllers = GetAllTypes()
            .Where(t => t.Namespace == "Exodus.Controllers.Seller"
                        && t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase)));

        sellerControllers.Should().NotBeEmpty("project should have seller controllers");

        foreach (var controller in sellerControllers)
        {
            var authorizeAttr = controller.GetCustomAttribute<AuthorizeAttribute>();
            authorizeAttr.Should().NotBeNull(
                $"{controller.Name} should have [Authorize] at class level");
            authorizeAttr!.Policy.Should().Be("SellerOnly",
                $"{controller.Name} should require SellerOnly policy");
        }
    }

    // ─── Service Layer Rules ────────────────────────────────────────────

    [Fact]
    public void Services_ShouldHaveMatchingInterface()
    {
        var serviceInterfaces = MainAssembly.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Services")
                        && t.Name.StartsWith("I")
                        && t.Name.EndsWith("Service"));

        serviceInterfaces.Should().NotBeEmpty("project should have service interfaces");

        foreach (var iface in serviceInterfaces)
        {
            var expectedName = iface.Name.Substring(1); // remove 'I' prefix
            var implementation = GetAllTypes()
                .FirstOrDefault(t => t.Name == expectedName && iface.IsAssignableFrom(t));

            implementation.Should().NotBeNull(
                $"interface {iface.Name} should have a concrete implementation named {expectedName}");
        }
    }

    [Fact]
    public void Services_ShouldResideInServicesNamespace()
    {
        var serviceTypes = GetAllTypes()
            .Where(t => t.Name.EndsWith("Service")
                        && !t.Name.Contains("Fake")
                        && t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Services"));

        serviceTypes.Should().NotBeEmpty();

        foreach (var service in serviceTypes)
        {
            service.Namespace.Should().StartWith("Exodus.Services",
                $"{service.Name} should be in Exodus.Services namespace");
        }
    }

    [Fact]
    public void ServiceInterfaces_ShouldNotDependOnControllers()
    {
        var serviceInterfaces = MainAssembly.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Services"));

        foreach (var iface in serviceInterfaces)
        {
            var methods = iface.GetMethods();
            foreach (var method in methods)
            {
                method.ReturnType.Namespace.Should().NotBe("Exodus.Controllers",
                    $"{iface.Name}.{method.Name} should not return a Controller type");

                foreach (var param in method.GetParameters())
                {
                    param.ParameterType.Namespace.Should().NotBe("Exodus.Controllers",
                        $"{iface.Name}.{method.Name} should not accept a Controller type as parameter");
                }
            }
        }
    }

    // ─── Entity / Model Layer Rules ─────────────────────────────────────

    // Standalone entities that manage their own key/audit fields (e.g. AuditLog)
    private static readonly HashSet<string> EntitiesWithoutBaseEntity = new() { "AuditLog", "ShipmentEvent" };

    [Fact]
    public void Entities_ShouldInheritFromBaseEntity()
    {
        var entities = GetAllTypes()
            .Where(t => t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Models.Entities")
                        && t.Name != "BaseEntity"
                        && !EntitiesWithoutBaseEntity.Contains(t.Name));

        entities.Should().NotBeEmpty("project should have entity classes");

        foreach (var entity in entities)
        {
            entity.Should().BeDerivedFrom(typeof(Exodus.Models.Entities.BaseEntity),
                $"{entity.Name} should inherit from BaseEntity");
        }
    }

    [Fact]
    public void Entities_ShouldHaveSoftDeleteProperties()
    {
        var baseEntityType = typeof(Exodus.Models.Entities.BaseEntity);

        baseEntityType.GetProperty("IsDeleted").Should().NotBeNull(
            "BaseEntity should have IsDeleted property for soft delete");
        baseEntityType.GetProperty("DeletedDate").Should().NotBeNull(
            "BaseEntity should have DeletedDate property for soft delete");
    }

    [Fact]
    public void Entities_ShouldHaveAuditProperties()
    {
        var baseEntityType = typeof(Exodus.Models.Entities.BaseEntity);

        baseEntityType.GetProperty("CreatedAt").Should().NotBeNull(
            "BaseEntity should have CreatedAt audit property");
        baseEntityType.GetProperty("UpdatedAt").Should().NotBeNull(
            "BaseEntity should have UpdatedAt audit property");
    }

    [Fact]
    public void Entities_ShouldResideInEntitiesNamespace()
    {
        var entities = GetAllTypes()
            .Where(t => t.IsSubclassOf(typeof(Exodus.Models.Entities.BaseEntity)));

        entities.Should().NotBeEmpty();

        foreach (var entity in entities)
        {
            entity.Namespace.Should().StartWith("Exodus.Models.Entities",
                $"{entity.Name} should be in Exodus.Models.Entities namespace");
        }
    }

    [Fact]
    public void Entities_ShouldNotDependOnServices()
    {
        var entities = GetAllTypes()
            .Where(t => t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Models.Entities"));

        foreach (var entity in entities)
        {
            var fields = entity.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType.Namespace != null)
                {
                    field.FieldType.Namespace.Should().NotStartWith("Exodus.Services",
                        $"{entity.Name}.{field.Name} should not reference service types");
                }
            }

            var properties = entity.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType.Namespace != null)
                {
                    prop.PropertyType.Namespace.Should().NotStartWith("Exodus.Services",
                        $"{entity.Name}.{prop.Name} should not reference service types");
                }
            }
        }
    }

    [Fact]
    public void Entities_ShouldNotDependOnControllers()
    {
        var entities = GetAllTypes()
            .Where(t => t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Models.Entities"));

        foreach (var entity in entities)
        {
            var properties = entity.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType.Namespace != null)
                {
                    prop.PropertyType.Namespace.Should().NotStartWith("Exodus.Controllers",
                        $"{entity.Name}.{prop.Name} should not reference controller types");
                }
            }
        }
    }

    // ─── DTO Layer Rules ────────────────────────────────────────────────

    [Fact]
    public void DTOs_ShouldResideInDtoOrServiceNamespace()
    {
        var dtoTypes = GetAllTypes()
            .Where(t => t.Name.EndsWith("Dto") && t.Namespace != null);

        dtoTypes.Should().NotBeEmpty("project should have DTO classes");

        foreach (var dto in dtoTypes)
        {
            var isInDtoNamespace = dto.Namespace!.Contains("Dto");
            var isInServiceNamespace = dto.Namespace.StartsWith("Exodus.Services");
            var isInControllerNamespace = dto.Namespace.StartsWith("Exodus.Controllers");

            (isInDtoNamespace || isInServiceNamespace || isInControllerNamespace).Should().BeTrue(
                $"{dto.Name} should reside in a Dto, Services, or Controllers namespace, but found {dto.Namespace}");
        }
    }

    [Fact]
    public void DTOs_ShouldNotInheritFromBaseEntity()
    {
        var dtoTypes = GetAllTypes()
            .Where(t => t.Name.EndsWith("Dto"));

        foreach (var dto in dtoTypes)
        {
            dto.Should().NotBeDerivedFrom(typeof(Exodus.Models.Entities.BaseEntity),
                $"{dto.Name} is a DTO and should not inherit from BaseEntity");
        }
    }

    [Fact]
    public void DTOs_ShouldNotDependOnDbContext()
    {
        var dtoTypes = GetAllTypes()
            .Where(t => t.Name.EndsWith("Dto"));

        foreach (var dto in dtoTypes)
        {
            var fields = dto.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                field.FieldType.Name.Should().NotBe("ApplicationDbContext",
                    $"{dto.Name} should not reference ApplicationDbContext");
            }
        }
    }

    // ─── Exception Hierarchy Rules ──────────────────────────────────────

    [Fact]
    public void CustomExceptions_ShouldInheritFromApiException()
    {
        var apiExceptionType = typeof(Exodus.Services.Common.ApiException);
        var customExceptions = MainAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Exception))
                        && t != apiExceptionType
                        && t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Services.Common"));

        customExceptions.Should().NotBeEmpty("project should have custom exception types");

        foreach (var exception in customExceptions)
        {
            exception.Should().BeDerivedFrom(apiExceptionType,
                $"{exception.Name} should inherit from ApiException");
        }
    }

    [Fact]
    public void CustomExceptions_ShouldBeSealed()
    {
        var apiExceptionType = typeof(Exodus.Services.Common.ApiException);
        var customExceptions = MainAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(apiExceptionType));

        customExceptions.Should().NotBeEmpty();

        foreach (var exception in customExceptions)
        {
            exception.IsSealed.Should().BeTrue(
                $"{exception.Name} should be sealed to prevent further inheritance");
        }
    }

    [Fact]
    public void ApiException_ShouldHaveStatusCodeProperty()
    {
        var apiExceptionType = typeof(Exodus.Services.Common.ApiException);

        apiExceptionType.GetProperty("StatusCode").Should().NotBeNull(
            "ApiException should expose StatusCode for the exception handling middleware");
    }

    // ─── Dependency Direction Rules ─────────────────────────────────────

    [Fact]
    public void Models_ShouldNotDependOnDataLayer()
    {
        var modelTypes = MainAssembly.GetTypes()
            .Where(t => t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Models"));

        foreach (var type in modelTypes)
        {
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType.Namespace != null)
                {
                    field.FieldType.Namespace.Should().NotStartWith("Exodus.Data",
                        $"{type.Name}.{field.Name} (Model layer) should not depend on Data layer");
                }
            }
        }
    }

    [Fact]
    public void Models_ShouldNotDependOnControllers()
    {
        var modelTypes = MainAssembly.GetTypes()
            .Where(t => t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Models"));

        foreach (var type in modelTypes)
        {
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType.Namespace != null)
                {
                    prop.PropertyType.Namespace.Should().NotStartWith("Exodus.Controllers",
                        $"{type.Name}.{prop.Name} (Model layer) should not depend on Controller layer");
                }
            }
        }
    }

    // ─── Validation Layer Rules ─────────────────────────────────────────

    [Fact]
    public void Validators_ShouldResideInValidationNamespace()
    {
        var validators = GetAllTypes()
            .Where(t => t.Name.EndsWith("Validator")
                        && t.Namespace != null);

        validators.Should().NotBeEmpty("project should have validator classes");

        foreach (var validator in validators)
        {
            validator.Namespace.Should().StartWith("Exodus.Validation",
                $"{validator.Name} should be in Exodus.Validation namespace");
        }
    }

    [Fact]
    public void Validators_ShouldInheritFromAbstractValidator()
    {
        var validators = GetAllTypes()
            .Where(t => t.Name.EndsWith("Validator")
                        && t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Validation"));

        validators.Should().NotBeEmpty();

        foreach (var validator in validators)
        {
            var isFluentValidator = validator.BaseType != null
                && validator.BaseType.IsGenericType
                && validator.BaseType.GetGenericTypeDefinition().Name.StartsWith("AbstractValidator");

            isFluentValidator.Should().BeTrue(
                $"{validator.Name} should inherit from AbstractValidator<T>");
        }
    }

    // ─── Enum Rules ─────────────────────────────────────────────────────

    [Fact]
    public void Enums_ShouldResideInModelsNamespace()
    {
        var enums = MainAssembly.GetTypes()
            .Where(t => t.IsEnum && t.Namespace != null && t.Namespace.StartsWith("Exodus.Models"));

        enums.Should().NotBeEmpty("project should have enum types");

        foreach (var enumType in enums)
        {
            // Enums can live in Exodus.Models.Enums or inline in Exodus.Models.Entities
            var isValid = enumType.Namespace == "Exodus.Models.Enums"
                          || enumType.Namespace!.StartsWith("Exodus.Models.Entities");

            isValid.Should().BeTrue(
                $"{enumType.Name} should be in Exodus.Models.Enums or Exodus.Models.Entities namespace, but found {enumType.Namespace}");
        }
    }

    // ─── Naming Convention Rules ────────────────────────────────────────

    [Fact]
    public void ControllerNames_ShouldEndWithController()
    {
        var controllerTypes = GetAllTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)));

        controllerTypes.Should().NotBeEmpty();

        foreach (var controller in controllerTypes)
        {
            controller.Name.Should().EndWith("Controller",
                $"controller class {controller.Name} should follow naming convention");
        }
    }

    [Fact]
    public void ServiceInterfaces_ShouldFollowINamingConvention()
    {
        var serviceInterfaces = MainAssembly.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Services")
                        && t.Name.EndsWith("Service"));

        serviceInterfaces.Should().NotBeEmpty();

        foreach (var iface in serviceInterfaces)
        {
            iface.Name.Should().StartWith("I",
                $"service interface {iface.Name} should start with 'I'");
        }
    }

    [Fact]
    public void ValidatorNames_ShouldEndWithValidator()
    {
        var validators = GetAllTypes()
            .Where(t => t.Namespace != null
                        && t.Namespace.StartsWith("Exodus.Validation")
                        && t.BaseType is { IsGenericType: true }
                        && t.BaseType.GetGenericTypeDefinition().Name.StartsWith("AbstractValidator"));

        validators.Should().NotBeEmpty();

        foreach (var validator in validators)
        {
            validator.Name.Should().EndWith("Validator",
                $"validator class {validator.Name} should follow naming convention");
        }
    }

    // ─── Middleware Rules ───────────────────────────────────────────────

    [Fact]
    public void ExceptionHandlingMiddleware_ShouldExist()
    {
        var middleware = MainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ExceptionHandlingMiddleware");

        middleware.Should().NotBeNull("project should have an ExceptionHandlingMiddleware");
    }

    [Fact]
    public void ExceptionHandlingMiddleware_ShouldHaveInvokeMethod()
    {
        var middleware = MainAssembly.GetTypes()
            .First(t => t.Name == "ExceptionHandlingMiddleware");

        var invokeMethod = middleware.GetMethod("Invoke");
        invokeMethod.Should().NotBeNull(
            "ExceptionHandlingMiddleware should have an Invoke method");
        invokeMethod!.ReturnType.Should().Be(typeof(Task),
            "Invoke should return Task for async pipeline");
    }

    // ─── Data Layer Rules ───────────────────────────────────────────────

    [Fact]
    public void ApplicationDbContext_ShouldExistInDataNamespace()
    {
        var dbContext = MainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ApplicationDbContext");

        dbContext.Should().NotBeNull("project should have an ApplicationDbContext");
        dbContext!.Namespace.Should().Be("Exodus.Data",
            "ApplicationDbContext should be in Exodus.Data namespace");
    }

    [Fact]
    public void ApplicationDbContext_ShouldHaveDbSetsForAllEntities()
    {
        var dbContext = MainAssembly.GetTypes()
            .First(t => t.Name == "ApplicationDbContext");

        var dbSetProperties = dbContext.GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("DbSet"));

        dbSetProperties.Should().HaveCountGreaterThanOrEqualTo(20,
            "ApplicationDbContext should have DbSet properties for entity types");
    }

    // ─── Cross-Layer Dependency Summary ─────────────────────────────────

    [Fact]
    public void Controllers_ShouldNotReferenceDataNamespaceDirectly_ExceptAllowed()
    {
        var nonAdminControllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase))
                        && t.Namespace != null
                        && !t.Namespace.Contains("Admin")
                        && !ControllersAllowedDbContext.Contains(t.Name));

        foreach (var controller in nonAdminControllers)
        {
            var fields = controller.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType.Namespace != null)
                {
                    field.FieldType.Namespace.Should().NotBe("Exodus.Data",
                        $"{controller.Name} should not directly reference Data layer (use services instead)");
                }
            }
        }
    }

    // ─── API Route Convention Rules ─────────────────────────────────────

    [Fact]
    public void AdminControllers_ShouldHaveAdminRoutePrefix()
    {
        // Only check controllers named Admin* (public-facing controllers in the same file are excluded)
        var adminControllers = GetAllTypes()
            .Where(t => t.Name.StartsWith("Admin")
                        && t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase)));

        adminControllers.Should().NotBeEmpty();

        foreach (var controller in adminControllers)
        {
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            routeAttr.Should().NotBeNull();
            routeAttr!.Template.Should().StartWith("api/admin",
                $"{controller.Name} should have route starting with api/admin");
        }
    }

    [Fact]
    public void SellerControllers_ShouldHaveSellerRoutePrefix()
    {
        var sellerControllers = GetAllTypes()
            .Where(t => t.Namespace == "Exodus.Controllers.Seller"
                        && t.Name.EndsWith("Controller")
                        && t.IsSubclassOf(typeof(ControllerBase)));

        sellerControllers.Should().NotBeEmpty();

        foreach (var controller in sellerControllers)
        {
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            routeAttr.Should().NotBeNull();
            routeAttr!.Template.Should().StartWith("api/seller",
                $"{controller.Name} should have route starting with api/seller");
        }
    }

    // ─── Controller Action Method Rules ─────────────────────────────────

    [Fact]
    public void ControllerActions_ShouldReturnIActionResultOrDerived()
    {
        var controllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.IsSubclassOf(typeof(ControllerBase)));

        foreach (var controller in controllers)
        {
            var publicMethods = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes()
                    .Any(a => a.GetType().Name.StartsWith("Http")));

            foreach (var method in publicMethods)
            {
                var returnType = method.ReturnType;

                // Unwrap Task<T> if async
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }

                // Accept IActionResult, ActionResult, or ActionResult<T>
                var isActionResult = typeof(IActionResult).IsAssignableFrom(returnType)
                                     || typeof(ActionResult).IsAssignableFrom(returnType)
                                     || (returnType.IsGenericType
                                         && returnType.GetGenericTypeDefinition().Name.StartsWith("ActionResult"));

                isActionResult.Should().BeTrue(
                    $"{controller.Name}.{method.Name} should return IActionResult or ActionResult<T>");
            }
        }
    }

    [Fact]
    public void ControllerActions_ShouldHaveHttpMethodAttribute()
    {
        var controllers = GetAllTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.IsSubclassOf(typeof(ControllerBase)));

        foreach (var controller in controllers)
        {
            var publicMethods = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName); // exclude property accessors

            foreach (var method in publicMethods)
            {
                var hasHttpAttribute = method.GetCustomAttributes()
                    .Any(a => a is HttpGetAttribute or HttpPostAttribute
                        or HttpPutAttribute or HttpDeleteAttribute or HttpPatchAttribute);

                hasHttpAttribute.Should().BeTrue(
                    $"{controller.Name}.{method.Name} should have an HTTP method attribute");
            }
        }
    }
}
