using Microsoft.AspNetCore.Mvc;
using StructureWatch.Web.Controllers;
using StructureWatch.Core.Services;
using StructureWatch.Core.Models;
using Moq;

namespace StructureWatch.Web.Tests;

public class StructureControllerTests
{
    [Fact]
    public async Task Get_ReturnsBadRequest_WhenBboxMissing()
    {
        var validator = new CollisionValidator();
        var overpass = new Mock<IOverpassService>();
        var controller = new StructureController(validator, overpass.Object);

        var result = await controller.Get("way/123", "");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenBuildingNotInViewport()
    {
        var validator = new CollisionValidator();
        var overpass = new Mock<IOverpassService>();
        overpass.Setup(o => o.FetchFootprintsAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new List<BuildingFootprint>());

        var controller = new StructureController(validator, overpass.Object);
        var result = await controller.Get("way/999", "40.0,-74.0,41.0,-73.0");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Collisions_ReturnsBadRequest_WhenSelectedNull()
    {
        var validator = new CollisionValidator();
        var overpass = new Mock<IOverpassService>();
        var controller = new StructureController(validator, overpass.Object);

        var result = controller.Collisions(new CollisionRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
